using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>Event enrolments and results, including that enrolments are recorded correctly.</summary>
public class EnrolmentAndResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentAndResultTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, EventDto ev, CategoryDto category)> SetUpEventAsync()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        return (organiser, ev, category);
    }

    // ---------------- Enrolments ----------------

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsCorrectlyRecorded()
    {
        var (_, ev, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var profile = await participant.GetFromJsonAsync<ProfileDto>("/api/users/me");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(profile!.UserId, created!.ParticipantId);
        Assert.Equal(category.CategoryId, created.CategoryId);
        Assert.Equal(ev.EventId, created.EventId);
        Assert.Equal("Confirmed", created.Status);

        // ...and it shows up in the Participant's own list.
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == created.EnrolmentId && e.EventId == ev.EventId);
    }

    [Fact]
    public async Task Participant_EnrollingTwiceInTheSameCategory_Returns400()
    {
        var (_, _, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        await TestHelpers.EnrolAsync(participant, category.CategoryId);

        var second = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrollingInUnknownCategory_Returns404()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrollingWithInvalidCategoryId_Returns400()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { CategoryId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForTheirOwnEvent()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/events/{ev.EventId}/enrolments");
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(list!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Organiser_ViewEnrolmentsForUnknownEvent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await organiser.GetAsync("/api/events/999999/enrolments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ButNotSomeoneElses()
    {
        var (_, _, category) = await SetUpEventAsync();
        var owner = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var other = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(owner, category.CategoryId);

        var byOther = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        var byOwner = await owner.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        var mine = await owner.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        Assert.Equal("Cancelled", mine!.Single(e => e.EnrolmentId == enrolment.EnrolmentId).Status);
    }

    // ---------------- Results ----------------

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        var capture = await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = new TimeSpan(0, 52, 30), Position = 14 });
        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/me");
        var leaderboard = await participant.GetFromJsonAsync<List<ResultDto>>($"/api/events/{ev.EventId}/results");

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        var result = mine!.Single(r => r.EnrolmentId == enrolment.EnrolmentId);
        Assert.Equal(new TimeSpan(0, 52, 30), result.FinishTime);
        Assert.Equal(14, result.Position);
        Assert.Contains(leaderboard!, r => r.ResultId == result.ResultId);
    }

    [Fact]
    public async Task Organiser_CannotCaptureSecondResultForSameEnrolment_Returns400()
    {
        var (organiser, _, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);
        var request = new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = new TimeSpan(1, 0, 0), Position = 1 };
        await organiser.PostAsJsonAsync("/api/results", request);

        var second = await organiser.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);
        var intruder = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await intruder.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = new TimeSpan(1, 0, 0), Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultForUnknownEnrolment_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Organiser);

        var response = await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = 999999, FinishTime = new TimeSpan(1, 0, 0), Position = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultWithInvalidTimeOrPosition_Returns400()
    {
        var (organiser, _, category) = await SetUpEventAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolment = await TestHelpers.EnrolAsync(participant, category.CategoryId);

        var zeroTime = await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.Zero, Position = 1 });
        var badPosition = await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = new TimeSpan(1, 0, 0), Position = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, zeroTime.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPosition.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organiser, _, category) = await SetUpEventAsync();
        var runnerA = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var runnerB = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolA = await TestHelpers.EnrolAsync(runnerA, category.CategoryId);
        var enrolB = await TestHelpers.EnrolAsync(runnerB, category.CategoryId);
        await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolA.EnrolmentId, FinishTime = new TimeSpan(0, 50, 0), Position = 1 });
        await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolB.EnrolmentId, FinishTime = new TimeSpan(0, 55, 0), Position = 2 });

        var resultsForA = await runnerA.GetFromJsonAsync<List<ResultDto>>("/api/results/me");

        Assert.Single(resultsForA!);
        Assert.Equal(enrolA.EnrolmentId, resultsForA![0].EnrolmentId);
    }

    [Fact]
    public async Task EventLeaderboard_IsOrderedByPosition()
    {
        var (organiser, ev, category) = await SetUpEventAsync();
        var first = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var second = await TestHelpers.LoggedInClientAsync(_factory, TestHelpers.Participant);
        var enrolFirst = await TestHelpers.EnrolAsync(first, category.CategoryId);
        var enrolSecond = await TestHelpers.EnrolAsync(second, category.CategoryId);
        // capture the 2nd place result first to prove the ordering is done by the API
        await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolSecond.EnrolmentId, FinishTime = new TimeSpan(0, 58, 0), Position = 2 });
        await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest { EnrolmentId = enrolFirst.EnrolmentId, FinishTime = new TimeSpan(0, 49, 0), Position = 1 });

        var leaderboard = await _factory.CreateClient().GetFromJsonAsync<List<ResultDto>>($"/api/events/{ev.EventId}/results");

        Assert.Equal(new[] { 1, 2 }, leaderboard!.Select(r => r.Position!.Value).ToArray());
    }
}
