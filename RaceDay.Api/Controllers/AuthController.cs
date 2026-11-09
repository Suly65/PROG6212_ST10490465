using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IPasswordService _passwords;

    public AuthController(RaceDayDbContext db, IPasswordService passwords) : base(db)
    {
        _passwords = passwords;
    }

    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new account",
        Description = "Creates an Organiser or Participant account. The password is hashed (PBKDF2) before it is saved; the original password is never stored. Role must be 'Organiser' or 'Participant'.")]
    [SwaggerResponse(201, "Account created. Returns the new user (never the password).", typeof(AuthResponse))]
    [SwaggerResponse(400, "Missing/invalid fields or an invalid role.", typeof(MessageResponse))]
    [SwaggerResponse(409, "That email address is already registered.", typeof(MessageResponse))]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = Roles.All.FirstOrDefault(r => r.Equals(request.Role?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (role == null)
            return BadRequest(new MessageResponse("Role must be either 'Organiser' or 'Participant'."));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new MessageResponse("An account with this email address already exists."));

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        return Created("/api/users/me", user.ToAuthResponse("Registration successful. Please log in."));
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Log in",
        Description = "Verifies the email and password. On success a server-side session is created that stores the UserId and Role, and a session cookie is returned. All protected endpoints use that session.")]
    [SwaggerResponse(200, "Login successful. Session created.", typeof(AuthResponse))]
    [SwaggerResponse(400, "Missing/invalid fields.", typeof(MessageResponse))]
    [SwaggerResponse(401, "Invalid email or password.", typeof(MessageResponse))]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "unknown email" and "wrong password" so accounts cannot be probed.
        if (user == null || !_passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new MessageResponse("Invalid email or password."));

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(user.ToAuthResponse("Login successful."));
    }

    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out",
        Description = "Clears the server-side session so protected endpoints can no longer be accessed.")]
    [SwaggerResponse(200, "Session cleared.", typeof(MessageResponse))]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Logged out."));
    }
}
