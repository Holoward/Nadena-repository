namespace Application.Interfaces;

public interface ICurrentUserService
{
    string GetCurrentUserId();
    string GetCurrentUserName();
}
