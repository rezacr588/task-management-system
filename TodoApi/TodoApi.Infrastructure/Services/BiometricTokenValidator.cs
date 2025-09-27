using TodoApi.Application.Interfaces;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Infrastructure.Services
{
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
}