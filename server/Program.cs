var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Remove app.UseHttpsRedirection() if it's there
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();