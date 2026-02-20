namespace Authentication.Application.Dtos;

public class FilterCriterion
{
    public required string Field { get; set; }
    public required string Value { get; set; }
}

public class UserQueryParams
{
    public string? SortFields { get; set; }
    public string? SortDirections { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }

    public List<FilterCriterion>? Filters { get; set; }
}
