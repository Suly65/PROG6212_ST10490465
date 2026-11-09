using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;
public class RegisterForm
{
    [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
}
public class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public class EventForm
{
    [Required, StringLength(150, MinimumLength=2)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14);
    [Required] public string Location { get; set; } = "";
    [Range(0.01,999.99)] public decimal DistanceKm { get; set; }
    [Required] public string EventType { get; set; } = "Run";
}
public class EnrolForm { [Range(1,int.MaxValue)] public int CategoryId { get; set; } }
