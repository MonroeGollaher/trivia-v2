using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/questions")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestionsController(AppDbContext db) => _db = db;

    [HttpGet("{gameId}")]
    public async Task<IActionResult> GetByGame(int gameId)
    {
        var questions = await _db.Questions
            .Where(q => q.GameId == gameId)
            .ToListAsync();
        return Ok(questions);
    }

    [HttpPost("{gameId}")]
    public async Task<IActionResult> AddQuestions(int gameId, [FromBody] List<OpenTriviaQuestionDto> results)
    {
        var questions = results.Select(q => new Question
        {
            GameId = gameId,
            Category = q.Category,
            Text = q.Question,
            Answer = q.CorrectAnswer,
            WrongAnswers = q.IncorrectAnswers
        }).ToList();

        _db.Questions.AddRange(questions);
        await _db.SaveChangesAsync();
        return Ok(questions);
    }

    [HttpPut("{gameId}/next")]
    public async Task<IActionResult> NextQuestion(int gameId)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game == null) return NotFound();
        game.ActiveQuestionIndex++;
        await _db.SaveChangesAsync();
        return Ok(game);
    }
}

public record OpenTriviaQuestionDto(
    string Category,
    string Question,
    string CorrectAnswer,
    List<string> IncorrectAnswers
);
