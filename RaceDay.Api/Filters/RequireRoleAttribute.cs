using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Filters;

/// <summary>
/// Role-based access control using the server-side session.
///   - No session (not logged in)              -> 401 Unauthorized
///   - Logged in but role is not allowed       -> 403 Forbidden
/// Usage: [RequireRole("Organiser")], [RequireRole("Organiser", "Participant")]
/// or [RequireLogin] when any logged-in user may call the endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId == null || string.IsNullOrEmpty(role))
        {
            context.Result = new UnauthorizedObjectResult(
                new MessageResponse("You must be logged in to access this resource."));
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(
                new MessageResponse($"Access denied. This action requires the role: {string.Join(" or ", _roles)}."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

/// <summary>Any authenticated user (Organiser or Participant).</summary>
public class RequireLoginAttribute : RequireRoleAttribute
{
    public RequireLoginAttribute() : base()
    {
    }
}
