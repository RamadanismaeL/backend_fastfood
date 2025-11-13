using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using unipos_basic_backend.src.Data;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Repositories;

namespace unipos_basic_backend.src.Configs
{
    public static class ServiceManagerConfig
    {
        public static void Configure(IServiceCollection service, IConfiguration configuration)
        {
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();

            try
            {
                service.AddOpenApi(); 
                service.AddEndpointsApiExplorer();
                service.AddSwaggerConfiguration();
                service.AddJWTAuthentication(configuration, logger);
                service.AddHttpContextAccessor();

                // FluentValidation
                service.AddValidatorsFromAssembly(typeof(Program).Assembly);
                service.AddControllers().Services.AddFluentValidationAutoValidation();                

                service.AddSingleton<PostgresDb>();
                service.AddScoped<IUsersRepository, UsersRepository>();
                service.AddScoped<IAuthRepository, AuthRepository>();

                // Rate limitting: Sliding Windows
                service.AddRateLimiter(op =>
                {
                    // 1. Gloabl: 100 requests in any 60-second window (per user or IP)
                    op.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                        RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromSeconds(60),
                                SegmentsPerWindow = 6,
                                AutoReplenishment = true
                            }
                        )
                    );

                    // 2. Login Endpoint: 5 attempts in any 60 seconds
                    op.AddSlidingWindowLimiter("signin", opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromSeconds(60);
                        opt.SegmentsPerWindow = 6;
                        opt.AutoReplenishment = true;
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0; // No queue -> instant reject
                    });

                    // 3. Response: 429 + retry-after
                    op.OnRejected = async (context, token) =>
                    {
                        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.HttpContext.Response.Headers.RetryAfter = "60";

                        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        {
                            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                            await context.HttpContext.Response.WriteAsync($"Too many requests. Try again in {retryAfter.TotalSeconds:F0} seconds.", token);
                        }
                        else
                        {
                            await context.HttpContext.Response.WriteAsync("Too many requests. Please slow down.", token);
                        }

                        // Log abuse
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Rate limited: {IP} on {Path} : ", context.HttpContext.Connection.RemoteIpAddress, context.HttpContext.Request.Path);
                    };                    
                });

                string? audience = configuration["JWTSettings:validAudience"];
                if (string.IsNullOrWhiteSpace(audience))
                    throw new InvalidOperationException("JWTSettings:validAudience is missing in configuration.");
                service.AddCors(op =>
                {
                    op.AddPolicy("CorsPolicy",
                    c =>
                    {
                        c.WithOrigins(audience, "file://")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                    });
                });
            }
            catch (Exception ex)
            {
                logger.LogError("An error occurred while configuring services: {Message}", ex.Message);
                throw;
            }
        }
    }
}