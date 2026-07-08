using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Profile>().HasKey(p => p.Id);

        model.Entity<Game>()
            .HasOne(g => g.Creator)
            .WithMany()
            .HasForeignKey(g => g.CreatorId);

        model.Entity<Profile>()
            .HasOne(p => p.CurrentGame)
            .WithMany(g => g.Players)
            .HasForeignKey(p => p.CurrentGameId);


        model.Entity<Response>()
            .HasOne(r => r.Team)
            .WithMany(p => p.Responses)
            .HasForeignKey(r => r.TeamId);

        model.Entity<Response>()
            .HasIndex(r => new { r.TeamId, r.QuestionId })
            .IsUnique();

        model.Entity<Question>()
            .Property(q => q.WrongAnswers)
            .HasColumnType("text[]");
    }
}
