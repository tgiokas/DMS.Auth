namespace Authentication.Application.Dtos;

public class TotpSetupDto
{
    public required string Username { get; set; } 
    public required string Secret { get; set; } 
    public required string QrCodeUri { get; set; } 
    public required string Issuer { get; set; }
    public required string SetupToken { get; set; } 
}
