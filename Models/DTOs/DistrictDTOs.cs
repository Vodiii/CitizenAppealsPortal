namespace CitizenAppealsPortal.Models.DTOs;

public class CreateDistrictDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BoundaryGeoJson { get; set; } = string.Empty;
    public string? DeputyId { get; set; }
}

public class UpdateDistrictDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public string? DeputyId { get; set; }
}

public class ApproveDeputyDto
{
    public bool Approve { get; set; }
    public int? DistrictId { get; set; }
    public int? TermMonths { get; set; }   // срок полномочий в месяцах (например, 48). Если null – бессрочно?
}

public class ExtendTermDto
{
    public int? TermMonths { get; set; }  // добавить месяцев от текущей даты, или null – сделать бессрочным
    public bool Deactivate { get; set; }  // если true – завершить текущий срок досрочно
}