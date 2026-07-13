using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public class GamesControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public GamesControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Game> SeedGame(string title = "Test Game")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var game = new Game { Title = title, NumberOfQuestions = 2, CreatorId = TestAuthHandler.UserId, RoomPin = Guid.NewGuid().ToString()[..6] };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }

    [Fact]
    public async Task EndGame_Returns200_AndSetsIsEnded()
    {
        var game = await SeedGame("End Test");

        var response = await _client.PutAsJsonAsync($"/api/games/{game.Id}/end", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isEnded").GetBoolean());
    }

    [Fact]
    public async Task EndGame_Returns404_WhenNotCreator()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var otherId = "other|user999";
        if (await db.Profiles.FindAsync(otherId) == null)
        {
            db.Profiles.Add(new Profile { Id = otherId, Email = "other@test.com", Name = "Other" });
            await db.SaveChangesAsync();
        }
        var game = new Game { Title = "Other's Game", NumberOfQuestions = 1, CreatorId = otherId, RoomPin = Guid.NewGuid().ToString()[..6] };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var response = await _client.PutAsJsonAsync($"/api/games/{game.Id}/end", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_IncludesIsEndedField()
    {
        var game = await SeedGame("IsEnded Test");
        // End the game so we have one ended and can verify the field comes through
        await _client.PutAsJsonAsync($"/api/games/{game.Id}/end", new { });

        var response = await _client.GetAsync("/api/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var endedGame = body.EnumerateArray().FirstOrDefault(g => g.GetProperty("id").GetInt32() == game.Id);
        Assert.True(endedGame.GetProperty("isEnded").GetBoolean());
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsTeamsOrderedByApprovedWagers()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var game = new Game { Title = "Leaderboard Test", NumberOfQuestions = 2, CreatorId = TestAuthHandler.UserId, RoomPin = Guid.NewGuid().ToString()[..6] };
        var q1 = new Question { Text = "Q1", Answer = "A1", Category = "Test", WrongAnswers = [] };
        var q2 = new Question { Text = "Q2", Answer = "A2", Category = "Test", WrongAnswers = [] };
        game.Questions.AddRange([q1, q2]);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        // Alpha: 10 approved + 5 approved = 15 pts
        // Beta: 20 approved + 100 NOT approved = 20 pts  → Beta wins
        var alpha = new Profile { Id = "lb-alpha", Email = "a@test.com", Name = "A Name", TeamName = "Alpha", CurrentGameId = game.Id };
        var beta = new Profile { Id = "lb-beta", Email = "b@test.com", Name = "B Name", TeamName = "Beta", CurrentGameId = game.Id };
        db.Profiles.AddRange(alpha, beta);
        db.Responses.AddRange(
            new Response { TeamId = "lb-alpha", QuestionId = q1.Id, Answer = "A", Wager = 10, Approved = true },
            new Response { TeamId = "lb-alpha", QuestionId = q2.Id, Answer = "B", Wager = 5, Approved = true },
            new Response { TeamId = "lb-beta", QuestionId = q1.Id, Answer = "C", Wager = 20, Approved = true },
            new Response { TeamId = "lb-beta", QuestionId = q2.Id, Answer = "D", Wager = 100, Approved = false }
        );
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/games/{game.Id}/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entries = body.EnumerateArray().ToList();

        Assert.Equal(2, entries.Count);
        Assert.Equal("Beta", entries[0].GetProperty("teamName").GetString());
        Assert.Equal(20, entries[0].GetProperty("totalScore").GetInt32());
        Assert.Equal("Alpha", entries[1].GetProperty("teamName").GetString());
        Assert.Equal(15, entries[1].GetProperty("totalScore").GetInt32());
    }

    [Fact]
    public async Task GetLeaderboard_UsesTeamNameOverAuthName()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var game = new Game { Title = "TeamName Test", NumberOfQuestions = 1, CreatorId = TestAuthHandler.UserId, RoomPin = Guid.NewGuid().ToString()[..6] };
        var q = new Question { Text = "Q", Answer = "A", Category = "Cat", WrongAnswers = [] };
        game.Questions.Add(q);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        // Profile has both Name (Auth0 display name) and TeamName — TeamName should win
        db.Profiles.Add(new Profile { Id = "tn-player", Email = "p@test.com", Name = "auth0|6a5146fac5eb", TeamName = "The Cool Kids", CurrentGameId = game.Id });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/games/{game.Id}/leaderboard");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var first = body.EnumerateArray().First();

        Assert.Equal("The Cool Kids", first.GetProperty("teamName").GetString());
    }
}
