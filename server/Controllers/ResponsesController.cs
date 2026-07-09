using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/responses")]
[Authorize]
public class ResponsesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<GameHub> _hub;

    public ResponsesController(AppDbContext db, IHubContext<GameHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet("{questionId}")]
    public async Task<IActionResult> GetByQuestion(int questionId)
    {
        var responses = await _db.Responses
            .Where(r => r.QuestionId == questionId)
            .Include(r => r.Team)
            .Select(r => new {
                r.Id, r.TeamId, r.QuestionId, r.Answer, r.Wager, r.Approved,
                team = new { r.Team!.Name, r.Team.TeamName }
            })
            .ToListAsync();
        return Ok(responses);
    }

    [HttpPost("{questionId}")]
    public async Task<IActionResult> AddResponse(int questionId, [FromBody] CreateResponseDto dto)
    {
        var teamId = GetUserId();

        var existing = await _db.Responses.FirstOrDefaultAsync(r => r.TeamId == teamId && r.QuestionId == questionId);
        if (existing != null) return Conflict("Already submitted a response for this question.");

        var response = new Response
        {
            QuestionId = questionId,
            TeamId = teamId,
            Answer = dto.Answer,
            Wager = dto.Wager
        };
        _db.Responses.Add(response);
        await _db.SaveChangesAsync();

        var result = new { response.Id, response.TeamId, response.QuestionId, response.Answer, response.Wager, response.Approved };

        var question = await _db.Questions.FindAsync(questionId);
        if (question != null)
            await _hub.Clients.Group(question.GameId.ToString()).SendAsync("orderRanking", result);

        return Ok(result);
    }

    [HttpPut("{responseId}/approval")]
    public async Task<IActionResult> ToggleApproval(int responseId)
    {
        var response = await _db.Responses.FindAsync(responseId);
        if (response == null) return NotFound();
        response.Approved = !response.Approved;
        await _db.SaveChangesAsync();
        return Ok(new { response.Id, response.TeamId, response.QuestionId, response.Answer, response.Wager, response.Approved });
    }
}

public record CreateResponseDto(string Answer, int Wager);
