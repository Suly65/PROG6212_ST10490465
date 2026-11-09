using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Mapping;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Both roles can view and update ONLY their own profile. The user is always taken
/// from the session - never from a UserId supplied by the caller.
/// </summary>
[Route("api/users")]
[RequireLogin]
public class ProfileController : ApiControllerBase
{
    public ProfileController(RaceDayDbContext db) : base(db)
    {
    }

    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get my profile", Description = "Returns the profile of the logged-in user (Organiser or Participant).")]
    [SwaggerResponse(200, "The logged-in user's profile.", typeof(ProfileDto))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(404, "The account no longer exists.", typeof(MessageResponse))]
    public async Task<IActionResult> GetMe()
    {
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user == null) return NotFound(new MessageResponse("User not found."));
        return Ok(user.ToProfileDto());
    }

    [HttpPut("me")]
    [SwaggerOperation(Summary = "Update my profile", Description = "Updates the full name and email of the logged-in user.")]
    [SwaggerResponse(200, "The updated profile.", typeof(ProfileDto))]
    [SwaggerResponse(400, "Invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Not logged in.", typeof(MessageResponse))]
    [SwaggerResponse(404, "The account no longer exists.", typeof(MessageResponse))]
    [SwaggerResponse(409, "That email address belongs to another account.", typeof(MessageResponse))]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await Db.Users.FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user == null) return NotFound(new MessageResponse("User not found."));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email && u.UserID != user.UserID))
            return Conflict(new MessageResponse("That email address is already used by another account."));

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await Db.SaveChangesAsync();

        return Ok(user.ToProfileDto());
    }
}
