using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class EventsController : Controller
{
    private readonly ApiClient _api; public EventsController(ApiClient api) => _api = api;
    public async Task<IActionResult> Index(string? search, string? eventType)
    {
        var query = new List<string>(); if (!string.IsNullOrWhiteSpace(search)) query.Add("search="+Uri.EscapeDataString(search)); if (!string.IsNullOrWhiteSpace(eventType)) query.Add("eventType="+Uri.EscapeDataString(eventType));
        ViewBag.Search=search; ViewBag.EventType=eventType;
        return View(await _api.GetAsync<List<ApiEvent>>("api/events"+(query.Count>0?"?"+string.Join("&",query):"")) ?? new());
    }
    public async Task<IActionResult> Details(int id)
    {
        var item=await _api.GetAsync<ApiEvent>($"api/events/{id}"); if(item is null) return NotFound();
        item.Categories=await _api.GetAsync<List<ApiCategory>>($"api/events/{id}/categories") ?? new(); return View(item);
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Enrol(int id, EnrolForm form)
    {
        if(HttpContext.Session.GetString("UserRole")!="Participant") return Forbid();
        var result=await _api.SendAsync(HttpMethod.Post,"api/enrolments",form);
        TempData[result.Ok?"Notice":"Error"]=result.Message; return RedirectToAction(nameof(Details),new{id});
    }
}
