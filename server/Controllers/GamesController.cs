using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/games")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly AppDbContext _db;

    public GamesController(AppDbContext db) => _db = db;

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    private static object ToDto(Game g) => new {
        g.Id, g.Title, g.NumberOfQuestions, g.CreatorId, g.RoomPin, g.ActiveQuestionIndex, g.CreatedAt, g.IsEnded
    };

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var games = await _db.Games
            .Where(g => g.CreatorId == userId)
            .Select(g => new { g.Id, g.Title, g.NumberOfQuestions, g.CreatorId, g.RoomPin, g.ActiveQuestionIndex, g.CreatedAt, g.IsEnded })
            .ToListAsync();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound();
        return Ok(ToDto(game));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameDto dto)
    {
        var userId = GetUserId();

        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null)
        {
            profile = new Profile
            {
                Id = userId,
                Email = User.FindFirstValue("https://monroeg.us.auth0.com/email") ?? "",
                Name = User.FindFirstValue("name") ?? userId,
            };
            _db.Profiles.Add(profile);
        }

        var game = new Game
        {
            Title = dto.Title,
            NumberOfQuestions = dto.NumberOfQuestions,
            CreatorId = userId,
            RoomPin = new Random().Next(100000, 999999).ToString()
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return Ok(ToDto(game));
    }

    [HttpPut("{id}/end")]
    public async Task<IActionResult> EndGame(int id)
    {
        var userId = GetUserId();
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatorId == userId);
        if (game == null) return NotFound();
        game.IsEnded = true;
        await _db.SaveChangesAsync();
        return Ok(ToDto(game));
    }

    [HttpGet("{id}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(int id)
    {
        var questionIds = await _db.Questions
            .Where(q => q.GameId == id)
            .Select(q => q.Id)
            .ToListAsync();

        var players = await _db.Profiles
            .Where(p => p.CurrentGameId == id)
            .ToListAsync();

        var approvedResponses = await _db.Responses
            .Where(r => r.Approved && questionIds.Contains(r.QuestionId))
            .ToListAsync();

        var leaderboard = players
            .Select(p => new {
                TeamName = p.TeamName ?? p.Name,
                TotalScore = approvedResponses.Where(r => r.TeamId == p.Id).Sum(r => r.Wager)
            })
            .OrderByDescending(e => e.TotalScore)
            .ToList();

        return Ok(leaderboard);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatorId == userId);
        if (game == null) return NotFound();
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return Ok(new { game.Id });
    }
}

public record CreateGameDto(string Title, int NumberOfQuestions);
