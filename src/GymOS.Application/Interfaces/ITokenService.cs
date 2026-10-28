using GymOS.Domain.Entities;

namespace GymOS.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    int GetUserIdFromToken(string token);
}
