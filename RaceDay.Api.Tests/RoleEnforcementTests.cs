using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>
/// Proves invalid access is blocked:
///   401 Unauthorized  = not logged in
///   403 Forbidden     = logged in with the wrong role (or not the owner)
/// </summary>
public class RoleEnforcementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleEnforcementTests(RaceDayFactory factory) => _factory = factory;

    // ---------------- Unauthenticated (401) ----------------

    [Fact]
    public async Task Unauthenticated_Profile_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CreateEvent_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Enrol_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest { CategoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/enrolments/me")]
    [InlineData("/api/results/me")]
    [InlineData("/api/events/mine")]
    [InlineData("/api/events/1/enrolments")]
    public async Task Unauthenticated_ProtectedGetEndpoints_Return401(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CanStillViewPublicEventsAndCategories()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var anonymous = _factory.CreateClient();

        var events = await anonymous.GetAsync("/api/events");
        var oneEvent = await anonymous.GetAsync($"/api/events/{ev.EventId}");
        var categories = await anonymous.GetAsync($"/api/events/{ev.EventId}/categories");

        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Equal(HttpStatusCode.OK, oneEvent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    // ---------------- Participant hitting Organiser-only endpoints (403) ----------------

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdateEvent_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PutAsJsonAsync($"/api/events/{ev.EventId}", TestHelpers.NewEventRequest("Hacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_DeleteEvent_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CreateCategoryRequest { Name = "5km", DistanceKm = 5, Price = 50 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CaptureResult_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(50), Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------- Organiser hitting Participant-only endpoints (403) ----------------

    [Fact]
    public async Task Organiser_Enrol_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/enrolments/me")]
    [InlineData("/api/results/me")]
    public async Task Organiser_ParticipantOnlyGetEndpoints_Return403(string url)
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await organiser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OrganiserOnlyGetEndpoint_EventsMine_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------------- Ownership: an Organiser cannot touch another Organiser's data (403) ----------------

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner);
        var intruder = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await intruder.PutAsJsonAsync($"/api/events/{ev.EventId}", TestHelpers.NewEventRequest("Stolen"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner);
        var intruder = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await intruder.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner);
        var intruder = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await intruder.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CreateCategoryRequest { Name = "5km", DistanceKm = 5, Price = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(owner);
        var intruder = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await intruder.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
