using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    public EventsController(RaceDayDbContext db) : base(db)
    {
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "List events",
        Description = "Returns all events ordered by date. Optional filters: search (name, location or description), eventType (Run, Walk or Cycle) and upcomingOnly=true to hide past events. Viewable by everyone.")]
    [SwaggerResponse(200, "Array of events.", typeof(List<EventDto>))]
    [SwaggerResponse(400, "Invalid eventType filter.", typeof(MessageResponse))]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? eventType, [FromQuery] bool upcomingOnly = false)
    {
        IQueryable<Event> query = Db.Events.AsNoTracking().Include(e => e.Organiser);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term)
                                     || e.Location.Contains(term)
                                     || (e.Description != null && e.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = EventTypes.All.FirstOrDefault(t => t.Equals(eventType.Trim(), StringComparison.OrdinalIgnoreCase));
            if (type == null)
                return BadRequest(new MessageResponse("eventType must be Run, Walk or Cycle."));
            query = query.Where(e => e.EventType == type);
        }

        if (upcomingOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(e => e.EventDate >= now);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToDto()).ToList());
    }

    [HttpGet("mine")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "List my events", Description = "Returns the events created by the logged-in Organiser. Organiser only.")]
    [SwaggerResponse(200, "Array of the Organiser's events.", typeof(List<EventDto>))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Logged in but not an Organiser.", typeof(MessageResponse))]
    public async Task<IActionResult> GetMine()
    {
        var events = await Db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Where(e => e.OrganiserID == CurrentUserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(e => e.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get one event", Description = "Returns the full details of a single event including its categories. Viewable by everyone.")]
    [SwaggerResponse(200, "The event with its categories.", typeof(EventDto))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await Db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Route)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null) return NotFound(new MessageResponse("Event not found."));
        return Ok(ev.ToDto(includeCategories: true));
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(
        Summary = "Create an event",
        Description = "Creates an event owned by the logged-in Organiser. EventType must be Run, Walk or Cycle. Organiser only.")]
    [SwaggerResponse(201, "The newly created event.", typeof(EventDto))]
    [SwaggerResponse(400, "Invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Logged in but not an Organiser.", typeof(MessageResponse))]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!TryValidate(request, out var eventType, out var error))
            return BadRequest(new MessageResponse(error));

        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKM = request.DistanceKm,
            EventType = eventType,
            CreatedAt = DateTime.UtcNow
        };

        Db.Events.Add(ev);
        await Db.SaveChangesAsync();
        await Db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ev.ToDto());
    }

    [HttpPut("{id:int}")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "Update an event", Description = "Updates an event. Only the Organiser who owns the event may update it.")]
    [SwaggerResponse(200, "The updated event.", typeof(EventDto))]
    [SwaggerResponse(400, "Invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of this event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var ev = await Db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new MessageResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only modify events that you own."));

        if (!TryValidate(request, out var eventType, out var error))
            return BadRequest(new MessageResponse(error));

        ev.Name = request.Name.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKM = request.DistanceKm;
        ev.EventType = eventType;

        await Db.SaveChangesAsync();
        return Ok(ev.ToDto());
    }

    [HttpDelete("{id:int}")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(
        Summary = "Delete an event",
        Description = "Deletes an event together with its categories, enrolments and results. Only the owning Organiser may delete it.")]
    [SwaggerResponse(200, "Event deleted.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of this event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound(new MessageResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only delete events that you own."));

        Db.Events.Remove(ev);
        await Db.SaveChangesAsync();
        return Ok(new MessageResponse("Event deleted."));
    }

    private static bool TryValidate(CreateEventRequest r, out string eventType, out string error)
    {
        error = string.Empty;
        eventType = EventTypes.All.FirstOrDefault(t => t.Equals(r.EventType?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        if (eventType.Length == 0)
        {
            error = "EventType must be Run, Walk or Cycle.";
            return false;
        }

        if (r.EventDate == default)
        {
            error = "A valid EventDate is required.";
            return false;
        }

        return true;
    }
}
