using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public class ProfilesControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public ProfilesControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> SeedProfileAndGame()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var pin = Guid.NewGuid().ToString()[..6];
        db.Games.Add(new Game { Title = "Test", NumberOfQuestions = 1, CreatorId = TestAuthHandler.UserId, RoomPin = pin });
        await db.SaveChangesAsync();

        return pin;
    }

    [Fact]
    public async Task JoinGame_Returns200_WithFlatDto()
    {
        var pin = await SeedProfileAndGame();

        var response = await _client.PutAsJsonAsync($"/api/profiles/joingame/{pin}", new { teamName = "Team Awesome" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("teamName", out var teamName));
        Assert.Equal("Team Awesome", teamName.GetString());
        Assert.False(body.TryGetProperty("currentGame", out _), "Profile DTO must not include 'currentGame' navigation property");
        Assert.False(body.TryGetProperty("responses", out _), "Profile DTO must not include 'responses' navigation property");
    }

    [Fact]
    public async Task JoinGame_Returns404_WhenGameNotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var response = await _client.PutAsJsonAsync("/api/profiles/joingame/BADPIN", new { teamName = "Team X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
