using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using unipos_basic_backend.src.Services;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Repositories
{
    public sealed class AuthRepository (PostgresDb db, IConfiguration config) : IAuthRepository
    {
        private readonly PostgresDb _db = db;
        private readonly IConfiguration _config = config;

        public async Task<AuthUsersDTO?> GetUserByUsername(string username)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"SELECT id, username, roles, password_hash AS Password, is_active AS IS_Active FROM tbUsers WHERE username = @Username";
            return await conn.QueryFirstOrDefaultAsync<AuthUsersDTO>(sql, new { Username = username });
        }

        public async Task<AuthUsersDTO?> GetUserById(Guid userId)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"SELECT id, username, roles, password_hash AS Password, is_active AS IS_Active FROM tbUsers WHERE id = @Id";
            return await conn.QueryFirstOrDefaultAsync<AuthUsersDTO>(sql, new { Id = userId });
        }

        public async Task<UsernameRoleDTO?> GetUsernameRoleById(Guid userId)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"SELECT username, roles FROM tbUsers WHERE id = @Id LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<UsernameRoleDTO?>(sql, new { Id = userId });
        }

        public async Task<RefreshTokenDTO?> GetValidRefreshToken(string token)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"SELECT id AS Id, user_id AS UserId, token AS Token, expires_at AS ExpiresAt, created_at AS CreatedAt, revoked_at AS RevokedAt FROM tbRefreshToken WHERE token = @Token AND revoked_at IS NULL AND expires_at > NOW()";
            return await conn.QueryFirstOrDefaultAsync<RefreshTokenDTO>(sql, new { Token = token });
        }

        public async Task SaveRefreshToken(RefreshTokenDTO refreshToken)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"INSERT INTO tbRefreshToken (id, user_id, token, expires_at) VALUES (@Id, @UserId, @Token, @ExpiresAt)";
            await conn.ExecuteAsync(sql, refreshToken);
        }

        public async Task RevokeRefreshToken(string token)
        {
            await using var conn = _db.CreateConnection();

            const string sql = @"UPDATE tbRefreshToken SET revoked_at = NOW() WHERE token = @Token";
            await conn.ExecuteAsync(sql, new { Token = token });
        }

        public string GenerateAccessToken(AuthUsersDTO authUsers)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, authUsers.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, authUsers.Username),
                new Claim(ClaimTypes.Role, authUsers.Roles),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSettings:securityKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWTSettings:validIssuer"],
                audience: _config["JWTSettings:validAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshToken(Guid userId)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(randomBytes);

            var refreshToken = new RefreshTokenDTO
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await SaveRefreshToken(refreshToken);
            return token;
        }
    }
}