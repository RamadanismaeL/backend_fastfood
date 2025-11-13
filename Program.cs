/*
* Copyright (c) 2025, UniPOS - Ramadan Ismael
*/

using unipos_basic_backend.src.Configs;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

ServiceManagerConfig.Configure(builder.Services, builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfiguration();
}

app.UseHttpsRedirection();
app.UseWebSockets();
app.UseDefaultFiles();
string[] folders = ["uploads", "images"];
foreach (var folder in folders)
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);
    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(path),
        RequestPath = "/" + folder
    });
}

app.UseCors("unipos_fastfood");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();