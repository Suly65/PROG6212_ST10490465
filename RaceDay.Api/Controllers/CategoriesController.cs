using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

public class CategoriesController : ApiControllerBase
{
    public CategoriesController(RaceDayDbContext db) : base(db)
    {
    }

    [HttpGet("/api/events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "List an event's categories", Description = "Returns all categories (e.g. 5km, 10km, Under 20) for an event. Viewable by everyone.")]
    [SwaggerResponse(200, "Array of categories.", typeof(List<CategoryDto>))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new MessageResponse("Event not found."));

        var categories = await Db.Categories.AsNoTracking()
            .Include(c => c.Route)
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKM)
            .ToListAsync();

        return Ok(categories.Select(c => c.ToDto()).ToList());
    }

    [HttpPost("/api/events/{eventId:int}/categories")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "Add a category to an event", Description = "Adds a category to an event. Only the Organiser who owns the event may add categories.")]
    [SwaggerResponse(201, "The new category.", typeof(CategoryDto))]
    [SwaggerResponse(400, "Invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of this event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Event does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateCategoryRequest request)
    {
        var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null) return NotFound(new MessageResponse("Event not found."));
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only add categories to events that you own."));

        var category = new Category
        {
            EventID = eventId,
            Name = request.Name.Trim(),
            DistanceKM = request.DistanceKm,
            Price = request.Price
        };

        Db.Categories.Add(category);
        await Db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", category.ToDto());
    }

    [HttpPut("/api/categories/{id:int}")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "Update a category", Description = "Updates a category. Only the Organiser who owns the parent event may update it.")]
    [SwaggerResponse(200, "The updated category.", typeof(CategoryDto))]
    [SwaggerResponse(400, "Invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of the parent event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Category does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await Db.Categories.Include(c => c.Event).Include(c => c.Route).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null) return NotFound(new MessageResponse("Category not found."));
        if (category.Event == null || category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only modify categories of events that you own."));

        category.Name = request.Name.Trim();
        category.DistanceKM = request.DistanceKm;
        category.Price = request.Price;
        await Db.SaveChangesAsync();

        return Ok(category.ToDto());
    }

    [HttpDelete("/api/categories/{id:int}")]
    [RequireRole(Roles.Organiser)]
    [SwaggerOperation(Summary = "Delete a category", Description = "Removes a category (and its enrolments/results). Only the Organiser who owns the parent event may delete it.")]
    [SwaggerResponse(200, "Category deleted.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(403, "Not an Organiser, or not the owner of the parent event.", typeof(MessageResponse))]
    [SwaggerResponse(404, "Category does not exist.", typeof(MessageResponse))]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await Db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null) return NotFound(new MessageResponse("Category not found."));
        if (category.Event == null || category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new MessageResponse("You can only delete categories of events that you own."));

        Db.Categories.Remove(category);
        await Db.SaveChangesAsync();
        return Ok(new MessageResponse("Category deleted."));
    }
}
