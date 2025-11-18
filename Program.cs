/*
* Copyright (c) 2025, Ramadan Ismael
*/

using unipos_basic_backend.src.Configs;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;
using unipos_basic_backend.src.Repositories;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

ServiceManagerConfig.Configure(builder.Services, builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfiguration();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseWebSockets();

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();
string[] folders = ["uploads", "images"];
foreach (var folder in folders)
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);
    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(path),
        RequestPath = "/" + folder
    });
}

app.MapHub<NotificationHub>("/notificationHub", op =>
{
    op.Transports = HttpTransportType.WebSockets;
})
.RequireCors("CorsPolicy")
.RequireAuthorization();

app.MapControllers();

app.Run();