using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ITokenValidator _tokenValidator;

        public AuthorizationService(ITokenGenerator tokenGenerator, ITokenValidator tokenValidator)
        {
            _tokenGenerator = tokenGenerator;
            _tokenValidator = tokenValidator;
        }

        public bool IsBiometricTokenValid(string token, int userId)
        {
            return _tokenValidator.ValidateToken(token, userId);
        }

        public string GenerateJwtToken(User user)
        {
            return _tokenGenerator.GenerateToken(user);
        }
    }
}
