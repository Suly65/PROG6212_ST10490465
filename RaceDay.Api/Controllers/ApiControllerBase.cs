using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for reading the logged-in user from the session.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly RaceDayDbContext Db;

    protected ApiControllerBase(RaceDayDbContext db)
    {
        Db = db;
    }

    /// <summary>UserId of the logged-in user. Only call on endpoints protected by a [RequireRole]/[RequireLogin] filter.</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
}
