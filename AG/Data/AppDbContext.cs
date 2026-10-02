using System.IO;
using AG.Models;
using Microsoft.EntityFrameworkCore;

namespace AG.Data;

public class AppDbContext : DbContext
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Sprint> Sprints => Set<Sprint>();

    public string DbPath { get; }

    public AppDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniJiraAG");

        Directory.CreateDirectory(folder);
        DbPath = Path.Combine(folder, "tickets.db");
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniJiraAG");

        Directory.CreateDirectory(folder);
        DbPath = Path.Combine(folder, "tickets.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.Priority).HasConversion<string>();
            entity.Property(t => t.Status).HasConversion<string>();
            entity.Property(t => t.Type).HasConversion<string>();
            entity.Property(t => t.Assignee).HasMaxLength(100);

            entity.HasOne(t => t.Sprint)
                  .WithMany(s => s.Tickets)
                  .HasForeignKey(t => t.SprintId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Goal).HasMaxLength(500);
            entity.Property(s => s.State).HasConversion<string>();
        });
    }

    public void EnsureSeeded()
    {
        Database.EnsureCreated();

        // Migration automatique légère si la table existait déjà sans les nouvelles colonnes
        try
        {
            Database.ExecuteSqlRaw("ALTER TABLE Tickets ADD COLUMN Assignee TEXT DEFAULT 'Non assigné';");
        }
        catch { /* La colonne existe déjà */ }

        try
        {
            Database.ExecuteSqlRaw("ALTER TABLE Tickets ADD COLUMN Type TEXT DEFAULT 'TASK';");
        }
        catch { /* La colonne existe déjà */ }

        try
        {
            Database.ExecuteSqlRaw("ALTER TABLE Tickets ADD COLUMN StoryPoints INTEGER DEFAULT 3;");
        }
        catch { /* La colonne existe déjà */ }

        if (!Sprints.Any())
        {
            var sprint1 = new Sprint(1, "Sprint 1 - Authentification & BCE", "Mettre en place la synchro et les contrôles légaux", SprintState.ACTIVE);
            var sprint2 = new Sprint(2, "Sprint 2 - Refonte UI & Libellés", "Polir l'interface utilisateur et résoudre les 403", SprintState.PLANNED);

            Sprints.AddRange(sprint1, sprint2);
            SaveChanges();

            Tickets.AddRange(
                new Ticket
                {
                    Id = 1,
                    Title = "Vérifier le format BCE",
                    Description = "Valider l'algorithme Modulo 97 pour les numéros d'entreprise belges.",
                    Priority = Priority.MEDIUM,
                    Type = TicketType.STORY,
                    Status = TicketStatus.IN_PROGRESS,
                    StoryPoints = 5,
                    Assignee = "Clément A.",
                    IsClosed = false,
                    SprintId = 1
                },
                new Ticket
                {
                    Id = 2,
                    Title = "Erreur de synchro SSO",
                    Description = "Le token OAuth expire prématurément lors des appels vers le service d'identité.",
                    Priority = Priority.HIGH,
                    Type = TicketType.BUG,
                    Status = TicketStatus.DONE,
                    StoryPoints = 8,
                    Assignee = "Sophie D.",
                    IsClosed = true,
                    SprintId = 1
                },
                new Ticket
                {
                    Id = 3,
                    Title = "Erreur 403 après refresh",
                    Description = "Le middleware rejette les tokens rafraîchis si l'en-tête de session n'est pas réémis.",
                    Priority = Priority.HIGH,
                    Type = TicketType.BUG,
                    Status = TicketStatus.TODO,
                    StoryPoints = 3,
                    Assignee = "Thomas M.",
                    IsClosed = false,
                    SprintId = 2
                },
                new Ticket
                {
                    Id = 4,
                    Title = "Améliorer un libellé",
                    Description = "Modifier la mention de consentement RGPD sur la modale d'inscription.",
                    Priority = Priority.LOW,
                    Type = TicketType.TASK,
                    Status = TicketStatus.BACKLOG,
                    StoryPoints = 1,
                    Assignee = "Clément A.",
                    IsClosed = false,
                    SprintId = null
                }
            );

            SaveChanges();
        }
    }
}
