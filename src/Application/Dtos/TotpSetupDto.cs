namespace DMS.Auth.Application.Dtos;

public class TotpSetupDto
{
    public string Username { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public string QrCodeUri { get; set; } = default!;
    public string Issuer { get; set; } = default!;   
    public string SetupToken { get; set; } = default!;
}
