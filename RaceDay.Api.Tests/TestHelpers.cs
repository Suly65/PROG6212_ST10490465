using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>Helpers that keep the individual tests short and readable.</summary>
public static class TestHelpers
{
    public const string Password = "Passw0rd!23";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@raceday.test";

    /// <summary>Registers a new user and returns the request that was used.</summary>
    public static RegisterRequest NewRegisterRequest(string role, string? email = null) => new()
    {
        FullName = $"Test {role}",
        Email = email ?? UniqueEmail(role.ToLowerInvariant()),
        Password = Password,
        Role = role
    };

    /// <summary>
    /// Registers a brand new user, logs in and returns a client that carries the session cookie.
    /// Every call returns a separate client, so an Organiser and a Participant can act side by side.
    /// </summary>
    public static async Task<HttpClient> LoggedInClientAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var register = NewRegisterRequest(role);

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = register.Email, Password = register.Password });
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    public static CreateEventRequest NewEventRequest(string name = "Gqeberha Bay Run") => new()
    {
        Name = name,
        Description = "Scenic coastal road running event.",
        EventDate = DateTime.UtcNow.AddMonths(2),
        Location = "Gqeberha, Eastern Cape",
        DistanceKm = 21.1m,
        EventType = "Run"
    };

    public static async Task<EventDto> CreateEventAsync(HttpClient organiser, string name = "Gqeberha Bay Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "10km Race")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest { Name = name, DistanceKm = 10m, Price = 100m });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}
