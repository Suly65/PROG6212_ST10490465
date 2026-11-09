using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace RaceDay.Mvc.Services;

/// <summary>Small HTTP client that forwards the API's session cookie on behalf of the signed-in MVC user.</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly IConfiguration _configuration;
    private const string ApiCookieKey = "RaceDay.ApiCookie";

    public ApiClient(HttpClient http, IHttpContextAccessor accessor, IConfiguration configuration)
    {
        _http = http;
        _accessor = accessor;
        _configuration = configuration;
        _http.BaseAddress = new Uri(_configuration["Api:BaseUrl"] ?? "http://localhost:5000/");
    }

    private HttpRequestMessage Create(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        var cookie = _accessor.HttpContext?.Session.GetString(ApiCookieKey);
        if (!string.IsNullOrWhiteSpace(cookie)) request.Headers.Add("Cookie", cookie);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        using var request = Create(HttpMethod.Get, path);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<(bool Ok, string Message)> SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = Create(method, path, body);
        using var response = await _http.SendAsync(request);
        var message = await ReadMessage(response);
        return (response.IsSuccessStatusCode, message);
    }

    public async Task<(bool Ok, string Message)> LoginAsync(object body)
    {
        using var request = Create(HttpMethod.Post, "api/auth/login", body);
        using var response = await _http.SendAsync(request);
        var message = await ReadMessage(response);
        if (response.IsSuccessStatusCode)
        {
            var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.FirstOrDefault() : null;
            var cookiePair = setCookie?.Split(';', 2)[0];
            if (!string.IsNullOrWhiteSpace(cookiePair)) _accessor.HttpContext!.Session.SetString(ApiCookieKey, cookiePair);
            return (true, message);
        }
        return (false, message);
    }

    public async Task LogoutAsync()
    {
        try { await SendAsync(HttpMethod.Post, "api/auth/logout"); }
        finally { _accessor.HttpContext?.Session.Remove(ApiCookieKey); }
    }

    private static async Task<string> ReadMessage(HttpResponseMessage response)
    {
        try
        {
            var data = await response.Content.ReadFromJsonAsync<ApiMessage>();
            return data?.Message ?? response.ReasonPhrase ?? "Request completed.";
        }
        catch { return response.IsSuccessStatusCode ? "Request completed." : "The API request failed."; }
    }

    private sealed class ApiMessage { public string? Message { get; set; } }
}
