using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public class ResponsesControllerTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebAppFactory _factory;

    public ResponsesControllerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> SeedQuestion()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Profiles.FindAsync(TestAuthHandler.UserId) == null)
        {
            db.Profiles.Add(new Profile { Id = TestAuthHandler.UserId, Email = "test@test.com", Name = "Test" });
            await db.SaveChangesAsync();
        }

        var game = new Game { Title = "Test", NumberOfQuestions = 1, CreatorId = TestAuthHandler.UserId, RoomPin = Guid.NewGuid().ToString()[..6] };
        var question = new Question { Text = "Q?", Answer = "A", Category = "Test", WrongAnswers = [] };
        game.Questions.Add(question);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        return question.Id;
    }

    [Fact]
    public async Task AddResponse_Returns200_WithFlatDto()
    {
        var questionId = await SeedQuestion();

        var response = await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "my answer", wager = 5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _));
        Assert.True(body.TryGetProperty("answer", out _));
        Assert.False(body.TryGetProperty("question", out _), "Response DTO must not include 'question' navigation property");
        Assert.False(body.TryGetProperty("team", out _), "Response DTO must not include 'team' navigation property");
    }

    [Fact]
    public async Task AddResponse_Returns409_OnDuplicate()
    {
        var questionId = await SeedQuestion();

        await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "first", wager = 1 });
        var second = await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "second", wager = 1 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetByQuestion_Returns200_WithFlatDto()
    {
        var questionId = await SeedQuestion();
        await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "test", wager = 2 });

        var response = await _client.GetAsync($"/api/responses/{questionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var first = body.EnumerateArray().First();
        Assert.True(first.TryGetProperty("team", out _));
        Assert.False(first.TryGetProperty("responses", out _), "Team DTO inside response must not include 'responses' navigation property");
    }

    [Fact]
    public async Task ToggleApproval_Returns200_WithApprovedToggled()
    {
        var questionId = await SeedQuestion();
        var postRes = await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "toggle me", wager = 7 });
        var postBody = await postRes.Content.ReadFromJsonAsync<JsonElement>();
        var responseId = postBody.GetProperty("id").GetInt32();
        var initialApproved = postBody.GetProperty("approved").GetBoolean();

        var toggleRes = await _client.PutAsJsonAsync($"/api/responses/{responseId}/approval", new { });

        Assert.Equal(HttpStatusCode.OK, toggleRes.StatusCode);
        var toggleBody = await toggleRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(!initialApproved, toggleBody.GetProperty("approved").GetBoolean());
        Assert.False(toggleBody.TryGetProperty("team", out _), "ToggleApproval must not include 'team' navigation property");
    }

    [Fact]
    public async Task GetResult_Returns200_WithCorrectAnswer_WhenDenied()
    {
        var questionId = await SeedQuestion(); // question Answer = "A"

        var postRes = await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "Wrong answer", wager = 5 });
        var responseId = (await postRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var result = await _client.GetAsync($"/api/responses/{responseId}/result");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("approved").GetBoolean());
        Assert.Equal("Wrong answer", body.GetProperty("answer").GetString());
        Assert.Equal("A", body.GetProperty("correctAnswer").GetString());
    }

    [Fact]
    public async Task GetResult_Returns200_WithCorrectAnswer_WhenApproved()
    {
        var questionId = await SeedQuestion(); // question Answer = "A"

        var postRes = await _client.PostAsJsonAsync($"/api/responses/{questionId}", new { answer = "A", wager = 10 });
        var responseId = (await postRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await _client.PutAsJsonAsync($"/api/responses/{responseId}/approval", new { });

        var result = await _client.GetAsync($"/api/responses/{responseId}/result");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("approved").GetBoolean());
        Assert.Equal(10, body.GetProperty("wager").GetInt32());
        Assert.Equal("A", body.GetProperty("correctAnswer").GetString());
    }

    [Fact]
    public async Task GetResult_Returns404_WhenResponseNotFound()
    {
        var result = await _client.GetAsync("/api/responses/999999/result");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
