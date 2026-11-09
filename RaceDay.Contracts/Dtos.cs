using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// ============================================================
//  Shared request / response shapes for the RaceDay API.
//  DTOs are NOT database tables - they only describe the data
//  moving into and out of the API.
// ============================================================

/// <summary>Generic message wrapper used for errors and simple confirmations.</summary>
public class MessageResponse
{
    public MessageResponse() { }
    public MessageResponse(string message) => Message = message;

    public string Message { get; set; } = string.Empty;
}

// ---------------------------- Authentication ----------------------------

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ------------------------------- Profile --------------------------------

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

// -------------------------------- Events --------------------------------

public class CreateEventRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Overall event distance in kilometres.</summary>
    [Range(0.01, 999.99, ErrorMessage = "DistanceKm must be between 0.01 and 999.99.")]
    public decimal DistanceKm { get; set; }

    /// <summary>One of: Run, Walk, Cycle.</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventRequest : CreateEventRequest
{
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;

    /// <summary>Populated by GET /api/events/{id} only.</summary>
    public List<CategoryDto>? Categories { get; set; }
}

// ------------------------------ Categories ------------------------------

public class CreateCategoryRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "DistanceKm must be between 0.01 and 999.99.")]
    public decimal DistanceKm { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Price cannot be negative.")]
    public decimal Price { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest
{
}

public class RouteDto
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public int ElevationGainM { get; set; }
    public string? MapUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal Price { get; set; }

    /// <summary>Route information (if one has been captured for this category).</summary>
    public RouteDto? Route { get; set; }
}

// ------------------------------ Enrolments ------------------------------

public class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid CategoryId is required.")]
    public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

// -------------------------------- Results -------------------------------

public class CreateResultRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid EnrolmentId is required.")]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time as hh:mm:ss, e.g. 00:52:30.</summary>
    [Required]
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position (optional, 1 or higher).</summary>
    public int? Position { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>Reserved for the Part 3 image-upload feature.</summary>
public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
