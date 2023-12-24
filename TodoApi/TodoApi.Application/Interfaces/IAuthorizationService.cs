using TodoApi.Domain.Entities;

namespace TodoApi.Application.Interfaces
{
    public interface IAuthorizationService
    {
        bool IsBiometricTokenValid(string token, int userId);
        string GenerateJwtToken(User user);
    }
    
    public interface ITokenGenerator
    {
        string GenerateToken(User user);
    }

    public interface ITokenValidator
    {
        bool ValidateToken(string token, int userId);
    }
}
