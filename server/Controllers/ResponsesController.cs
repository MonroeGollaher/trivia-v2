using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/responses")]
[Authorize]
public class ResponsesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResponsesController(AppDbContext db) => _db = db;

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet("{questionId}")]
    public async Task<IActionResult> GetByQuestion(int questionId)
    {
        var responses = await _db.Responses
            .Where(r => r.QuestionId == questionId)
            .Include(r => r.Team)
            .ToListAsync();
        return Ok(responses);
    }

    [HttpPost("{questionId}")]
    public async Task<IActionResult> AddResponse(int questionId, [FromBody] CreateResponseDto dto)
    {
        var teamId = GetUserId();
        var response = new Response
        {
            QuestionId = questionId,
            TeamId = teamId,
            Answer = dto.Answer,
            Wager = dto.Wager
        };
        _db.Responses.Add(response);
        await _db.SaveChangesAsync();
        return Ok(response);
    }

    [HttpPut("{responseId}/approval")]
    public async Task<IActionResult> ToggleApproval(int responseId)
    {
        var response = await _db.Responses.FindAsync(responseId);
        if (response == null) return NotFound();
        response.Approved = !response.Approved;
        await _db.SaveChangesAsync();
        return Ok(response);
    }
}

public record CreateResponseDto(string Answer, int Wager);
