using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Configs
{
    public static class JWTConfig
    {
        public static void AddJWTAuthentication(this IServiceCollection service, IConfiguration configuration, ILogger logger)
        {
            try
            {
                var jwtSettings = configuration.GetSection("JWTSettings").Get<JwtSettingsDTO>();

                if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.ValidAudience) || string.IsNullOrEmpty(jwtSettings.ValidIssuer) || string.IsNullOrEmpty(jwtSettings.SecurityKey))
                {
                    throw new InvalidOperationException("JWT Settings are not properly configured in appsettings.");
                }

                service.AddAuthentication(op =>
                {
                    op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    op.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(op =>
                {
                    op.RequireHttpsMetadata = false;
                    op.SaveToken = true;
                    op.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.ValidIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.ValidAudience,
                        RequireAudience = true,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,

                        ClockSkew = TimeSpan.FromMilliseconds(500),

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey)),
                        RequireSignedTokens = true
                    };

                    op.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Priority 1: Cookie (frontend)
                            if (context.Request.Cookies.TryGetValue("accessToken", out var token) && !string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                                return Task.CompletedTask;
                            }

                            // Priority 2: Header (Postman)
                            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                context.Token = authHeader["Bearer ".Length..].Trim();
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                logger.LogError("An error occurred while configuring JWT Authentication: {Message}", ex.Message);
                throw;
            }
        }
    }
}