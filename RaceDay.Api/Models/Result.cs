using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>A finish time and position for one Enrolment (zero or one per Enrolment).</summary>
[Table("Results")]
public class Result
{
    [Key]
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public TimeSpan FinishTime { get; set; }

    public int? Position { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Enrolment? Enrolment { get; set; }
}
