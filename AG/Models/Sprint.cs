using System;
using System.Collections.Generic;

namespace AG.Models;

public class Sprint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SprintState State { get; set; } = SprintState.ACTIVE;

    public List<Ticket> Tickets { get; set; } = new();

    public Sprint() { }

    public Sprint(int id, string name, string? goal = null, SprintState state = SprintState.ACTIVE)
    {
        Id = id;
        Name = name;
        Goal = goal;
        State = state;
    }
}
