using AG.Data;
using AG.Models;
using Microsoft.EntityFrameworkCore;

namespace AG.Services;

public class TicketService
{
    private readonly Func<AppDbContext> _contextFactory;

    public TicketService(Func<AppDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory ?? (() => new AppDbContext());
        using var db = _contextFactory();
        db.EnsureSeeded();
    }

    // ==========================================
    // TICKETS
    // ==========================================

    public Ticket AddTicket(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        using var db = _contextFactory();
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }

    public Ticket CreateTicket(
        string title,
        Priority priority,
        string? description = null,
        int? sprintId = null,
        TicketStatus status = TicketStatus.TODO,
        TicketType type = TicketType.TASK,
        int storyPoints = 3,
        string assignee = "Non assigné")
    {
        using var db = _contextFactory();
        var ticket = new Ticket
        {
            Title = title.Trim(),
            Priority = priority,
            Description = description?.Trim(),
            SprintId = sprintId,
            Status = status,
            Type = type,
            StoryPoints = storyPoints,
            Assignee = string.IsNullOrWhiteSpace(assignee) ? "Non assigné" : assignee.Trim(),
            IsClosed = (status == TicketStatus.DONE)
        };

        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }

    public bool UpdateTicket(Ticket updated)
    {
        using var db = _contextFactory();
        var ticket = db.Tickets.FirstOrDefault(t => t.Id == updated.Id);
        if (ticket == null) return false;

        ticket.Title = updated.Title;
        ticket.Description = updated.Description;
        ticket.Priority = updated.Priority;
        ticket.Type = updated.Type;
        ticket.StoryPoints = updated.StoryPoints;
        ticket.Assignee = updated.Assignee;
        ticket.Status = updated.Status;
        ticket.IsClosed = (updated.Status == TicketStatus.DONE);
        ticket.SprintId = updated.SprintId;

        db.SaveChanges();
        return true;
    }

    public bool MoveTicketToSprint(int ticketId, int? sprintId)
    {
        using var db = _contextFactory();
        var ticket = db.Tickets.FirstOrDefault(t => t.Id == ticketId);
        if (ticket == null) return false;

        ticket.SprintId = sprintId;
        db.SaveChanges();
        return true;
    }

    public bool UpdateTicketStatus(int ticketId, TicketStatus newStatus)
    {
        using var db = _contextFactory();
        var ticket = db.Tickets.FirstOrDefault(t => t.Id == ticketId);
        if (ticket == null) return false;

        ticket.Status = newStatus;
        ticket.IsClosed = (newStatus == TicketStatus.DONE);
        db.SaveChanges();
        return true;
    }

    public bool CloseTicket(int id)
    {
        using var db = _contextFactory();
        var ticket = db.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null) return false;

        ticket.Close();
        db.SaveChanges();
        return true;
    }

    public bool DeleteTicket(int id)
    {
        using var db = _contextFactory();
        var ticket = db.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null) return false;

        db.Tickets.Remove(ticket);
        db.SaveChanges();
        return true;
    }

    public Ticket? GetTicketById(int id)
    {
        using var db = _contextFactory();
        return db.Tickets
            .Include(t => t.Sprint)
            .FirstOrDefault(t => t.Id == id);
    }

    public List<Ticket> GetAllTickets()
    {
        using var db = _contextFactory();
        return db.Tickets
            .Include(t => t.Sprint)
            .OrderBy(t => t.Id)
            .ToList();
    }

    public List<Ticket> GetTicketsBySprint(int? sprintId)
    {
        using var db = _contextFactory();
        return db.Tickets
            .Include(t => t.Sprint)
            .Where(t => t.SprintId == sprintId)
            .OrderByDescending(t => t.Priority)
            .ToList();
    }

    public List<Ticket> SearchTickets(string searchTerm, int? sprintId = null, TicketType? type = null, string? assignee = null)
    {
        using var db = _contextFactory();
        var query = db.Tickets.Include(t => t.Sprint).AsQueryable();

        if (sprintId.HasValue)
        {
            query = query.Where(t => t.SprintId == sprintId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignee) && assignee != "Tous")
        {
            query = query.Where(t => t.Assignee == assignee);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        return query.OrderByDescending(t => t.Priority).ToList();
    }

    public List<Ticket> GetOpenTicketsSortedByPriority()
    {
        using var db = _contextFactory();
        return db.Tickets
            .Include(t => t.Sprint)
            .Where(t => !t.IsClosed)
            .OrderByDescending(t => t.Priority)
            .ToList();
    }

    public int GetOpenTicketsCount()
    {
        using var db = _contextFactory();
        return db.Tickets.Count(t => !t.IsClosed);
    }

    public List<string> GetAllAssignees()
    {
        using var db = _contextFactory();
        return db.Tickets
            .Select(t => t.Assignee)
            .Distinct()
            .OrderBy(a => a)
            .ToList();
    }

    // ==========================================
    // SPRINTS
    // ==========================================

    public List<Sprint> GetAllSprints()
    {
        using var db = _contextFactory();
        return db.Sprints
            .Include(s => s.Tickets)
            .OrderBy(s => s.Id)
            .ToList();
    }

    public Sprint CreateSprint(string name, string? goal = null, SprintState state = SprintState.PLANNED)
    {
        using var db = _contextFactory();
        var sprint = new Sprint
        {
            Name = name.Trim(),
            Goal = goal?.Trim(),
            State = state,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        db.Sprints.Add(sprint);
        db.SaveChanges();
        return sprint;
    }

    public bool UpdateSprintState(int sprintId, SprintState newState)
    {
        using var db = _contextFactory();
        var sprint = db.Sprints.FirstOrDefault(s => s.Id == sprintId);
        if (sprint == null) return false;

        sprint.State = newState;
        db.SaveChanges();
        return true;
    }

    public bool DeleteSprint(int sprintId)
    {
        using var db = _contextFactory();
        var sprint = db.Sprints.Include(s => s.Tickets).FirstOrDefault(s => s.Id == sprintId);
        if (sprint == null) return false;

        foreach (var ticket in sprint.Tickets)
        {
            ticket.SprintId = null;
        }

        db.Sprints.Remove(sprint);
        db.SaveChanges();
        return true;
    }
}
