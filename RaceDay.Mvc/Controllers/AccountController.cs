using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
namespace RaceDay.Mvc.Controllers;
public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) => _api = api;
    [HttpGet] public IActionResult Login() => View(new LoginForm());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await _api.LoginAsync(new { form.Email, form.Password });
        if (!result.Ok) { ModelState.AddModelError("", result.Message); return View(form); }
        // The API response contains the authenticated user's role; retrieve it through the profile endpoint.
        var user = await _api.GetAsync<ApiUser>("api/users/me");
        if (user is null) { ModelState.AddModelError("", "Login succeeded, but the API profile could not be loaded."); return View(form); }
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetInt32("UserId", user.UserId);
        return RedirectToAction("Index", user.Role == "Organiser" ? "Organiser" : "Participant");
    }
    [HttpGet] public IActionResult Register() => View(new RegisterForm());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await _api.SendAsync(HttpMethod.Post, "api/auth/register", form);
        if (!result.Ok) { ModelState.AddModelError("", result.Message); return View(form); }
        TempData["Notice"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync(); HttpContext.Session.Clear(); return RedirectToAction("Index", "Home");
    }
}
