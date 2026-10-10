using System.ComponentModel.DataAnnotations;

public sealed class Department
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public sealed class InternshipProgram
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Description { get; set; } = "";
    public int Capacity { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Sắp diễn ra";
}

public sealed record DepartmentInput([property:Required, MaxLength(120)] string Name);

public sealed record ProgramInput(
    [property:Required, MaxLength(160)] string Name,
    [property:Range(1, int.MaxValue)] int DepartmentId,
    [property:MaxLength(2000)] string? Description,
    [property:Range(1, 10000)] int Capacity,
    DateOnly StartDate,
    DateOnly EndDate,
    [property:Required] string Status);