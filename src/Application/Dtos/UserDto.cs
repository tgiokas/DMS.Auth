namespace DMS.Auth.Application.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsMfaEnabled { get; set; }
        public string AgencyId { get; set; }
    }
}