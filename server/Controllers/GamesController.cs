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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var games = await _db.Games
            .Where(g => g.CreatorId == userId)
            .Include(g => g.Creator)
            .ToListAsync();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var game = await _db.Games
            .Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (game == null) return NotFound();
        return Ok(game);
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
        return Ok(game);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.CreatorId == userId);
        if (game == null) return NotFound();
        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return Ok(game);
    }
}

public record CreateGameDto(string Title, int NumberOfQuestions);
