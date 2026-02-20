namespace Authentication.Application.Dtos;
public class PagedResult<T>
{
    public List<T> Results { get; set; } = [];
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; }
}