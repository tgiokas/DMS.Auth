namespace Authentication.Application.Dtos;

public class UserQueryParams
{
    public string? SortBy { get; set; } = "username"; 
    public bool SortDescending { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
