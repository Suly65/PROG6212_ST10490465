using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>Registration, login, logout and password handling.</summary>
public class AuthenticationTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthenticationTests(RaceDayFactory factory) => _factory = factory;

    // ---------------- Registration ----------------

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_WithValidDetails_Returns201AndRole(string role)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.NewRegisterRequest(role));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(role, body!.Role);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = TestHelpers.NewRegisterRequest(TestHelpers.Participant);

        var first = await client.PostAsJsonAsync("/api/auth/register", request);
        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var request = TestHelpers.NewRegisterRequest("Administrator");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var request = TestHelpers.NewRegisterRequest(TestHelpers.Participant);
        request.Password = "short";

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var request = TestHelpers.NewRegisterRequest(TestHelpers.Participant, "not-an-email");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainTextPassword()
    {
        var client = _factory.CreateClient();
        var request = TestHelpers.NewRegisterRequest(TestHelpers.Participant);
        await client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Users.Single(u => u.Email == request.Email.ToLowerInvariant());

        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.DoesNotContain(request.Password, stored.PasswordHash);
        Assert.StartsWith("PBKDF2$", stored.PasswordHash);
    }

    // ---------------- Login ----------------

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndCreatesSession()
    {
        var client = _factory.CreateClient();
        var register = TestHelpers.NewRegisterRequest(TestHelpers.Organiser);
        await client.PostAsJsonAsync("/api/auth/register", register);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email, Password = register.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(TestHelpers.Organiser, body!.Role);

        // The session now identifies the user on later requests.
        var profile = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var register = TestHelpers.NewRegisterRequest(TestHelpers.Participant);
        await client.PostAsJsonAsync("/api/auth/register", register);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email, Password = "WrongPassword!1" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var profile = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = TestHelpers.UniqueEmail("ghost"), Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    // ---------------- Logout ----------------

    [Fact]
    public async Task Logout_ClearsSession_AndProtectedEndpointsReturn401()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    // ---------------- Profile ----------------

    [Fact]
    public async Task Profile_CanBeViewedAndUpdated_ByTheLoggedInUser()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var newEmail = TestHelpers.UniqueEmail("updated");

        var update = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest { FullName = "Sarah van Wyk", Email = newEmail });
        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Sarah van Wyk", profile!.FullName);
        Assert.Equal(newEmail, profile.Email);
    }

    [Fact]
    public async Task Profile_UpdateWithEmailOfAnotherUser_Returns409()
    {
        var other = TestHelpers.NewRegisterRequest(TestHelpers.Organiser);
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", other);
        var client = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var update = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest { FullName = "Someone", Email = other.Email });

        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    // ---------------- Password hashing (pure unit tests) ----------------

    [Fact]
    public void PasswordService_HashIsNotThePassword_AndVerifiesCorrectly()
    {
        var service = new PasswordService();

        var hash = service.Hash("MySecret123");

        Assert.NotEqual("MySecret123", hash);
        Assert.True(service.Verify("MySecret123", hash));
        Assert.False(service.Verify("mysecret123", hash));
    }

    [Fact]
    public void PasswordService_SamePasswordProducesDifferentHashes_BecauseOfSalt()
    {
        var service = new PasswordService();

        Assert.NotEqual(service.Hash("MySecret123"), service.Hash("MySecret123"));
    }

    [Fact]
    public void PasswordService_MalformedHash_DoesNotVerify()
    {
        var service = new PasswordService();

        Assert.False(service.Verify("anything", "not-a-valid-hash"));
        Assert.False(service.Verify("anything", string.Empty));
    }
}
