using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class ParticipantController:Controller
{
 private readonly ApiClient _api; public ParticipantController(ApiClient api)=>_api=api;
 private bool Allowed()=>HttpContext.Session.GetString("UserRole")=="Participant";
 public IActionResult Index(){if(!Allowed())return Forbid();return RedirectToAction("Index","Events");}
 public async Task<IActionResult> MyEvents(){if(!Allowed())return Forbid();return View(await _api.GetAsync<List<ApiEnrolment>>("api/enrolments/me")??new());}
 public async Task<IActionResult> MyResults(){if(!Allowed())return Forbid();return View(await _api.GetAsync<List<ApiResult>>("api/results/me")??new());}
}
