using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Mapping;

/// <summary>Maps database models to the DTOs returned by the API.</summary>
public static class MappingExtensions
{
    public static ProfileDto ToProfileDto(this AppUser u) => new()
    {
        UserId = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };

    public static AuthResponse ToAuthResponse(this AppUser u, string message) => new()
    {
        UserId = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        Message = message
    };

    public static RouteDto ToDto(this RaceRoute r) => new()
    {
        RouteId = r.RouteID,
        RouteName = r.RouteName,
        ElevationGainM = r.ElevationGainM,
        MapUrl = r.MapURL
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        CategoryId = c.CategoryID,
        EventId = c.EventID,
        Name = c.Name,
        DistanceKm = c.DistanceKM,
        Price = c.Price,
        Route = c.Route?.ToDto()
    };

    public static EventDto ToDto(this Event e, bool includeCategories = false) => new()
    {
        EventId = e.EventID,
        OrganiserId = e.OrganiserID,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKM,
        EventType = e.EventType,
        Categories = includeCategories
            ? e.Categories.OrderBy(c => c.DistanceKM).Select(c => c.ToDto()).ToList()
            : null
    };

    public static EnrolmentDto ToDto(this Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentID,
        ParticipantId = en.ParticipantID,
        ParticipantName = en.Participant?.FullName ?? string.Empty,
        CategoryId = en.CategoryID,
        CategoryName = en.Category?.Name ?? string.Empty,
        EventId = en.Category?.EventID ?? 0,
        EventName = en.Category?.Event?.Name ?? string.Empty,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status
    };

    public static ResultDto ToDto(this Result r) => new()
    {
        ResultId = r.ResultID,
        EnrolmentId = r.EnrolmentID,
        ParticipantId = r.Enrolment?.ParticipantID ?? 0,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? string.Empty,
        EventId = r.Enrolment?.Category?.EventID ?? 0,
        EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        RecordedAt = r.RecordedAt
    };
}
