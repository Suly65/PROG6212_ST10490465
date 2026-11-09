using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class OrganiserController : Controller
{
    private readonly ApiClient _api; public OrganiserController(ApiClient api)=>_api=api;
    private bool Allowed()=>HttpContext.Session.GetString("UserRole")=="Organiser";
    public async Task<IActionResult> Index(){if(!Allowed())return Forbid(); return View(await _api.GetAsync<List<ApiEvent>>("api/events/mine")??new());}
    [HttpGet] public IActionResult Create(){if(!Allowed())return Forbid();return View(new EventForm());}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(EventForm form){if(!Allowed())return Forbid();if(!ModelState.IsValid)return View(form);var r=await _api.SendAsync(HttpMethod.Post,"api/events",form);if(!r.Ok){ModelState.AddModelError("",r.Message);return View(form);}TempData["Notice"]="Event created.";return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id){if(!Allowed())return Forbid();var r=await _api.SendAsync(HttpMethod.Delete,$"api/events/{id}");TempData[r.Ok?"Notice":"Error"]=r.Message;return RedirectToAction(nameof(Index));}
    public async Task<IActionResult> Enrolments(int id){if(!Allowed())return Forbid();ViewBag.EventId=id;return View(await _api.GetAsync<List<ApiEnrolment>>($"api/events/{id}/enrolments")??new());}
    public async Task<IActionResult> Results(int id){if(!Allowed())return Forbid();ViewBag.EventId=id;return View(await _api.GetAsync<List<ApiResult>>($"api/events/{id}/results")??new());}
}
