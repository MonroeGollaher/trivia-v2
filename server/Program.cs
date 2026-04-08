using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Remove app.UseHttpsRedirection() if it's there
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();