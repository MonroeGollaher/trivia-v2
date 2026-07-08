using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/profiles")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfilesController(AppDbContext db) => _db = db;

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPost("me")]
    public async Task<IActionResult> UpsertMe([FromBody] UpsertProfileDto dto)
    {
        var userId = GetUserId();
        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null)
        {
            profile = new Profile { Id = userId, Email = dto.Email, Name = dto.Name, Picture = dto.Picture };
            _db.Profiles.Add(profile);
        }
        else
        {
            profile.Name = dto.Name;
            profile.Picture = dto.Picture;
        }
        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpGet("game/{gameId}")]
    public async Task<IActionResult> GetTeamsByGame(int gameId)
    {
        var players = await _db.Profiles
            .Where(p => p.CurrentGameId == gameId)
            .ToListAsync();
        return Ok(players);
    }

    [HttpPut("joingame/{roomPin}")]
    public async Task<IActionResult> JoinGame(string roomPin, [FromBody] JoinGameDto dto)
    {
        var userId = GetUserId();
        var game = await _db.Games.FirstOrDefaultAsync(g => g.RoomPin == roomPin);
        if (game == null) return NotFound("Game not found");

        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null) return NotFound("Profile not found");

        profile.CurrentGameId = game.Id;
        profile.TeamName = dto.TeamName;
        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    [HttpPut("leavegame")]
    public async Task<IActionResult> LeaveGame()
    {
        var userId = GetUserId();
        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null) return NotFound();
        profile.CurrentGameId = null;
        await _db.SaveChangesAsync();
        return Ok(profile);
    }
}

public record UpsertProfileDto(string Email, string Name, string? Picture);
public record JoinGameDto(string TeamName);
