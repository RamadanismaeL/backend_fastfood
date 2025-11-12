using FluentValidation;
using FluentValidation.AspNetCore;
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

                // FluentValidation
                service.AddValidatorsFromAssembly(typeof(Program).Assembly);
                service.AddControllers().Services.AddFluentValidationAutoValidation();
                service.AddHttpContextAccessor();

                service.AddSingleton<PostgresDb>();
                service.AddScoped<IUsersRepository, UsersRepository>();

                string? audience = configuration["JWTSettings:validAudience"];
                if (string.IsNullOrWhiteSpace(audience))
                    throw new InvalidOperationException("JWTSettings:validAudience is missing in configuration.");
                service.AddCors(op =>
                {
                    op.AddPolicy("unipos_fastfood",
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