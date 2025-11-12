using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        private const string TermsUrl = "https://unipos.app/terms";
        private const string SchemeDescription = "Enter JWT 'Bearer {token}' to access this API";

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
                Contact = new OpenApiContact
                {
                    Name = ContactName,
                    Email = ContactEmail,
                    Url = new Uri(ContactUrl)
                }
            });
        }

        private static void ConfigureJWTAuthentication(SwaggerGenOptions op)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = SchemeDescription
            };

            op.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
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