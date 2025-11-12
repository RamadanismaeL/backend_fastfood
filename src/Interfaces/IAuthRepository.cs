using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthUsersDTO?> GetUserByUsername(string username);
        Task<RefreshTokenDTO?> GetValidRefreshToken(string token);
        Task SaveRefreshToken(RefreshTokenDTO refreshToken);
        Task RevokeRefreshToken(string token);
        string GenerateAccessToken(AuthUsersDTO authUsers);
        Task<string> GenerateRefreshToken(Guid userId);
    }
}