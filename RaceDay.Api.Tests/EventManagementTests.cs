using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>Events and Categories: Organisers can manage them, everyone can view them.</summary>
public class EventManagementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventManagementTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CreateEvent_Returns201AndOwnsTheEvent()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var profile = await organiser.GetFromJsonAsync<ProfileDto>("/api/users/me");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest("Soweto Marathon"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Soweto Marathon", created!.Name);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(21.1m, created.DistanceKm);
        Assert.Equal(profile!.UserId, created.OrganiserId);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var request = TestHelpers.NewEventRequest();
        request.EventType = "Swim";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithMissingName_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var request = TestHelpers.NewEventRequest();
        request.Name = "";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithZeroDistance_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var request = TestHelpers.NewEventRequest();
        request.DistanceKm = 0;

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateOwnEvent_Returns200AndPersistsChanges()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var update = TestHelpers.NewEventRequest("Renamed Event");
        update.EventType = "Cycle";
        update.DistanceKm = 80m;

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);
        var reloaded = await organiser.GetFromJsonAsync<EventDto>($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Event", reloaded!.Name);
        Assert.Equal("Cycle", reloaded.EventType);
        Assert.Equal(80m, reloaded.DistanceKm);
    }

    [Fact]
    public async Task Organiser_UpdateUnknownEvent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await organiser.PutAsJsonAsync("/api/events/999999", TestHelpers.NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeleteOwnEvent_Returns200ThenEventIsGone()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await organiser.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task EventsMine_ReturnsOnlyTheLoggedInOrganisersEvents()
    {
        var organiserA = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var organiserB = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var eventA = await TestHelpers.CreateEventAsync(organiserA, "A's event");
        var eventB = await TestHelpers.CreateEventAsync(organiserB, "B's event");

        var mineA = await organiserA.GetFromJsonAsync<List<EventDto>>("/api/events/mine");

        Assert.Contains(mineA!, e => e.EventId == eventA.EventId);
        Assert.DoesNotContain(mineA!, e => e.EventId == eventB.EventId);
    }

    [Fact]
    public async Task Participant_CanViewEvents_AndSearchAndFilter()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var unique = "Zebra" + Guid.NewGuid().ToString("N")[..6];
        var ev = await TestHelpers.CreateEventAsync(organiser, unique);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var all = await participant.GetFromJsonAsync<List<EventDto>>("/api/events");
        var searched = await participant.GetFromJsonAsync<List<EventDto>>($"/api/events?search={unique}");
        var wrongType = await participant.GetFromJsonAsync<List<EventDto>>($"/api/events?search={unique}&eventType=Walk");
        var badType = await participant.GetAsync("/api/events?eventType=Swim");

        Assert.Contains(all!, e => e.EventId == ev.EventId);
        Assert.Single(searched!);
        Assert.Empty(wrongType!);
        Assert.Equal(HttpStatusCode.BadRequest, badType.StatusCode);
    }

    [Fact]
    public async Task GetUnknownEvent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------- Categories ----------------

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteCategories_AndEveryoneCanViewThem()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var created = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId, "10km");
        var update = await organiser.PutAsJsonAsync($"/api/categories/{created.CategoryId}",
            new UpdateCategoryRequest { Name = "21km", DistanceKm = 21.1m, Price = 150m });
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var visible = await participant.GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventId}/categories");
        var delete = await organiser.DeleteAsync($"/api/categories/{created.CategoryId}");
        var afterDelete = await participant.GetFromJsonAsync<List<CategoryDto>>($"/api/events/{ev.EventId}/categories");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Contains(visible!, c => c.Name == "21km" && c.DistanceKm == 21.1m);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Empty(afterDelete!);
    }

    [Fact]
    public async Task Category_WithNegativePrice_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CreateCategoryRequest { Name = "5km", DistanceKm = 5, Price = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Categories_ForUnknownEvent_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateUnknownCategory_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await organiser.PutAsJsonAsync("/api/categories/999999",
            new UpdateCategoryRequest { Name = "x", DistanceKm = 5, Price = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
