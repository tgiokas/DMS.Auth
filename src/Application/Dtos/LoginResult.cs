namespace Authentication.Application.Dtos;

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? Response { get; set; }

    public static LoginResult Fail(string error) => new() { Success = false, Error = error };
    public static LoginResult Ok(object response) => new() { Success = true, Response = response };
}
