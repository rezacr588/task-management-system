using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities; // Adjust the namespace as necessary
using TodoApi.Infrastructure.Data; // Adjust the namespace as necessary
using Microsoft.Extensions.Configuration;

namespace TodoApi.Infrastructure.Services
{
    public class JwtTokenGenerator : ITokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var secret = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("JWT secret is not configured. Please set 'Jwt:Key' in configuration.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim("id", user.Id.ToString()),
                // Add additional claims as needed
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class BiometricTokenValidator : ITokenValidator
    {
        private readonly ApplicationDbContext _context;

        public BiometricTokenValidator(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool ValidateToken(string token, int userId)
        {
            var user = _context.Users.Find(userId);
            return user != null && user.BiometricToken == token;
        }
    }

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
