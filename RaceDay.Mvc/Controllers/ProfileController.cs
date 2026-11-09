using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class ProfileController:Controller
{
 private readonly ApiClient _api; public ProfileController(ApiClient api)=>_api=api;
 public async Task<IActionResult> Index(){if(HttpContext.Session.GetString("UserRole") is null)return Forbid();var profile=await _api.GetAsync<RaceDay.Mvc.Models.ApiUser>("api/users/me");if(profile is null)return View("Unavailable");return View(profile);}
}
