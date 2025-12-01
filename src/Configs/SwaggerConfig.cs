using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace unipos_basic_backend.src.Configs
{
    public static class SwaggerConfig
    {
        private const string ApiVersion = "v1";
        private const string ApiTitle = "UniPOS FastFood Basic Backend API";
        private const string ApiDescription = "UniPOS FastFood Basic Backend API Documentation, version 1.0";
        private const string ContactName = "Ramadan Ismael";
        private const string ContactEmail = "ramadan.ismael02@gmail.com";
        private const string ContactUrl = "https://github.com/RamadanisameL";

        public static void AddSwaggerConfiguration(this IServiceCollection service)
        {
            service.AddSwaggerGen(op =>
            {
                ConfigureSwaggerDoc(op);
                ConfigureJWTAuthentication(op);
            });
        }

        public static void ConfigureSwaggerDoc(SwaggerGenOptions op)
        {
            op.SwaggerDoc(ApiVersion, new OpenApiInfo
            {
                Title = ApiTitle,
                Version = ApiVersion,
                Description = ApiDescription,
                TermsOfService = new Uri("http://tempuri.org/terms"),
                Contact = new OpenApiContact
                {
                    Name = ContactName,
                    Email = ContactEmail,
                    Url = new Uri(ContactUrl)
                },
                License = new OpenApiLicense
                {
                    Name = "Apache 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                }
            });
        }

        private static void ConfigureJWTAuthentication(SwaggerGenOptions op)
        {

            op.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Name = "JWT Authorization",
                Description = "Enter JWT 'Bearer {token}' to access this API",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"                
            });

            op.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });

            /*
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("/auth-server/connect/authorize", UriKind.Relative),
                        Scopes = new Dictionary<string, string>
                        {
                            ["readAccess"] = "Access read operations",
                            ["writeAccess"] = "Access write operations"
                        }
                    }
                }
            });

            options.AddSecurityRequirement((document) => new OpenApiSecurityRequirement()
            {
                [new OpenApiSecuritySchemeReference("oauth2", document)] = ["readAccess", "writeAccess"]
            });
            */
        }

        public static void UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(op =>
            {
                op.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", $"{ApiTitle} - {ApiVersion}");
                op.RoutePrefix = string.Empty;
                op.DocumentTitle = "uniPOs api";
                op.DisplayRequestDuration();
                op.EnableFilter();
                op.EnableValidator();
                op.DisplayOperationId();
                op.DocExpansion(DocExpansion.None);
            });
        }
    }
}