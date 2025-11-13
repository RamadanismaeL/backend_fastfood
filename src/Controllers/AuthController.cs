using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController (IAuthRepository authRepository, ILogger<AuthController> logger) : ControllerBase, IAuthController
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly ILogger<AuthController> _logger = logger;

        [HttpPost("v1/sign-in")]
        [EnableRateLimiting("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn([FromBody] AuthRequestDTO authRequest)
        {
            if (!ModelState.IsValid) return BadRequest(new AuthResponseDTO { IsSuccess = false, Message = "Invalid input." });

            try
            {
                var user = await _authRepository.GetUserByUsername(authRequest.Username);
                if (user is null || !BCrypt.Net.BCrypt.Verify(authRequest.Password, user.Password) || user.Is_Active == false)
                {
                    return Unauthorized(new AuthResponseDTO { IsSuccess = false, Message = "Invalid credentials." });
                }

                var accessToken = _authRepository.GenerateAccessToken(user);
                var refreshToken = await _authRepository.GenerateRefreshToken(user.Id);

                SetRefreshTokenCookie(refreshToken);

                return Ok(new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = "Signed in successfully.",
                    AccessToken = accessToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sign In failed for user {authRequest.Username}");
                return BadRequest(new AuthResponseDTO { IsSuccess = false, Message = "Unable to sign in. Please check your credentials and try again." });
            }
        }

        [HttpPost("v1/refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken)) return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Refresh token missing." });

                var tokenRecord = await _authRepository.GetValidRefreshToken(refreshToken);
                if (tokenRecord is null)
                {
                    Response.Cookies.Delete("refreshToken", GetSecureCookieOptions());
                    return Unauthorized(new ResponseDTO { IsSuccess = false, Message = "Invalid or expired refresh token." });
                }

                var user = await _authRepository.GetUserByUsername(tokenRecord.UserId.ToString());
                if (user is null) return Unauthorized();

                var newAccessToken = _authRepository.GenerateAccessToken(user);
                var newRefreshToken = await _authRepository.GenerateRefreshToken(user.Id);

                await _authRepository.RevokeRefreshToken(refreshToken);
                SetRefreshTokenCookie(newRefreshToken);

                return Ok(new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = "Session extended successfully.",
                    AccessToken = newAccessToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session expired. Please sign in again.");
                return BadRequest(new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Session expired. Please sign in again."
                });
            }
        }

        [HttpPost("v1/sign-out")]
        [Authorize]
        public new async Task<IActionResult> SignOut()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (!string.IsNullOrEmpty(refreshToken)) await _authRepository.RevokeRefreshToken(refreshToken);

                Response.Cookies.Delete("refreshToken", GetSecureCookieOptions());

                return Ok(new ResponseDTO
                {
                    IsSuccess = true,
                    Message = "Signed out successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign Out failed. Please retry.");
                return BadRequest(new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Unabel to sign out. Please try again."
                });
            }
        }

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, GetSecureCookieOptions());
        }

        private CookieOptions GetSecureCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "api/auth/refresh"
            };
        }
    }
}