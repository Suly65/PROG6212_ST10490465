using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

public class ResultsController : ApiControllerBase
{
    public ResultsController(RaceDayDbContext db) : base(db)
    {
    }

    private IQueryable<Result> ResultsWithDetails() =>
        Db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    [HttpPost("/api/results")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(
        Summary = "Capture a result",
        Description = "Records the finish time (hh:mm:ss) and finishing position for an enrolment. Only the Organiser who owns the event may capture results, and only one result is allowed per enrolment.")]
    [SwaggerResponse(201, "The new result.", typeof(ResultDto))]
    [SwaggerResponse(400, "Invalid data, cancelled enrolment, or a result already exists.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of the event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Enrolment does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Create([FromBody] CreateResultRequest request)
    {
        var enrolment = await Db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentId);

        if (enrolment == null) return NotFound(new MessageResponse("Enrolment not found."));

        if (enrolment.Category?.Event == null || enrolment.Category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only capture results for events that you own."));

        if (request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromHours(24))
            return BadRequest(new MessageResponse("FinishTime must be greater than 00:00:00 and less than 24 hours."));

        if (request.Position is < 1)
            return BadRequest(new MessageResponse("Position must be 1 or higher."));

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return BadRequest(new MessageResponse("A result cannot be captured for a cancelled enrolment."));

        if (await Db.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentId))
            return BadRequest(new MessageResponse("A result has already been captured for this enrolment."));

        var result = new Result
        {
            EnrolmentID = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            RecordedAt = DateTime.UtcNow
        };

        Db.Results.Add(result);
        await Db.SaveChangesAsync();

        var saved = await ResultsWithDetails().AsNoTracking().FirstAsync(r => r.ResultID == result.ResultID);
        return Created($"/api/events/{saved.Enrolment?.Category?.EventID}/results", saved.ToDto());
    }

    [HttpGet("/api/results/me")]
    [RequireRole(Roles.Participant)]
    [SwaggerOperation(Summary = "My results", Description = "Returns the logged-in Participant's personal result history. Participant only.")]
    [SwaggerResponse(200, "Array of results.", typeof(List<ResultDto>))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Logged in but not a Participant.", typeof(MessageResponse))]
    public async Task<IActionResult> GetMine()
    {
        var results = await ResultsWithDetails().AsNoTracking()
            .Where(r => r.Enrolment!.ParticipantID == CurrentUserId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();

        return Ok(results.Select(r => r.ToDto()).ToList());
    }

    [HttpGet("/api/events/{eventId:int}/results")]
    [SwaggerOperation(Summary = "Event results (leaderboard)", Description = "Returns all results for an event ordered by finishing position, then finish time. Viewable by everyone.")]
    [SwaggerResponse(200, "Array of results.", typeof(List<ResultDto>))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new MessageResponse("Event not found."));

        var results = await ResultsWithDetails().AsNoTracking()
            .Where(r => r.Enrolment!.Category!.EventID == eventId)
            .ToListAsync();

        var ordered = results
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.FinishTime)
            .Select(r => r.ToDto())
            .ToList();

        return Ok(ordered);
    }
}
