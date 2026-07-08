public class Game
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int NumberOfQuestions { get; set; }
    public string CreatorId { get; set; } = "";
    public string RoomPin { get; set; } = "";
    public int ActiveQuestionIndex { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Profile Creator { get; set; } = null!;
    public List<Question> Questions { get; set; } = [];
    public List<Profile> Players { get; set; } = [];
}
