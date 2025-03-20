namespace DMS.Auth.Application.Dtos;

public class MfaEnrollmentResponse
{
    public string TotpSecret { get; set; }
    public string QrCode { get; set; }
}
