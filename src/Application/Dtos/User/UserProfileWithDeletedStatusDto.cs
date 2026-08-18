namespace Authentication.Application.Dtos;

public class UserProfileWithDeletedStatusDto : UserProfileDto
{
    public new bool Deleted
    {
        get => base.Deleted;
        set => base.Deleted = value;
    }
}
