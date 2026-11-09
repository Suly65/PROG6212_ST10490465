using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

public class EnrolmentsController : ApiControllerBase
{
    public EnrolmentsController(RaceDayDbContext db) : base(db)
    {
    }

    private IQueryable<Enrolment> EnrolmentsWithDetails() =>
        Db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event);

    [HttpPost("/api/enrolments")]
    [RequireRole(Roles.Participant)]
    [SwaggerOperation(
        Summary = "Enter an event",
        Description = "Enrols the logged-in Participant in a category. The enrolment links the Participant, the category and (through the category) the event. Participant only.")]
    [SwaggerResponse(201, "The enrolment record.", typeof(EnrolmentDto))]
    [SwaggerResponse(400, "Invalid data or already enrolled in this category.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Logged in but not a Participant.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Category does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentRequest request)
    {
        var category = await Db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId);
        if (category == null) return NotFound(new MessageResponse("Category not found."));

        var alreadyEnrolled = await Db.Enrolments.AnyAsync(e =>
            e.ParticipantID == CurrentUserId
            && e.CategoryID == request.CategoryId
            && e.Status != EnrolmentStatuses.Cancelled);

        if (alreadyEnrolled)
            return BadRequest(new MessageResponse("You are already enrolled in this category."));

        var enrolment = new Enrolment
        {
            ParticipantID = CurrentUserId,
            CategoryID = request.CategoryId,
            Status = EnrolmentStatuses.Confirmed,
            EnrolmentDate = DateTime.UtcNow
        };

        Db.Enrolments.Add(enrolment);
        await Db.SaveChangesAsync();

        var saved = await EnrolmentsWithDetails().AsNoTracking().FirstAsync(e => e.EnrolmentID == enrolment.EnrolmentID);
        return Created("/api/enrolments/me", saved.ToDto());
    }

    [HttpGet("/api/enrolments/me")]
    [RequireRole(Roles.Participant)]
    [SwaggerOperation(Summary = "My enrolments", Description = "Lists all enrolments of the logged-in Participant. Participant only.")]
    [SwaggerResponse(200, "Array of enrolments.", typeof(List<EnrolmentDto>))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Logged in but not a Participant.", typeof(MessageResponse))]
    public async Task<IActionResult> GetMine()
    {
        var enrolments = await EnrolmentsWithDetails().AsNoTracking()
            .Where(e => e.ParticipantID == CurrentUserId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToDto()).ToList());
    }

    [HttpGet("/api/events/{eventId:int}/enrolments")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "View an event's enrolments", Description = "Lists every enrolment for an event. Only the Organiser who owns the event may view them.")]
    [SwaggerResponse(200, "Array of enrolments.", typeof(List<EnrolmentDto>))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of this event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null) return NotFound(new MessageResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only view enrolments for events that you own."));

        var enrolments = await EnrolmentsWithDetails().AsNoTracking()
            .Where(e => e.Category!.EventID == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToDto()).ToList());
    }

    [HttpDelete("/api/enrolments/{id:int}")]
    [RequireRole(Roles.Participant)]
    [SwaggerOperation(Summary = "Cancel my enrolment", Description = "Cancels the logged-in Participant's own enrolment (Status becomes 'Cancelled'; the record is kept for history). Participant only.")]
    [SwaggerResponse(200, "Enrolment cancelled.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not a Participant, or the enrolment belongs to someone else.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Enrolment does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await Db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null) return NotFound(new MessageResponse("Enrolment not found."));
        if (enrolment.ParticipantID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only cancel your own enrolments."));

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await Db.SaveChangesAsync();
        return Ok(new MessageResponse("Enrolment cancelled."));
    }
}
