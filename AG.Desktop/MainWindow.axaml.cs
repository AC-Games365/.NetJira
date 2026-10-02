using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AG.Models;
using AG.Services;

namespace AG.Desktop;

public partial class MainWindow : Window
{
    private readonly TicketService _ticketService = new();
    private List<Sprint> _sprints = new();
    private int? _selectedSprintFilterId = 1; // Sprint 1 par défaut

    public MainWindow()
    {
        InitializeComponent();

        CreateTicketBtn.Click += OnCreateTicketClicked;
        CreateSprintBtn.Click += OnCreateSprintClicked;
        RefreshButton.Click += (_, _) => ReloadAll();
        CompleteSprintButton.Click += OnCompleteSprintClicked;

        SearchBox.KeyUp += (_, _) => RefreshBoard();
        TypeFilterComboBox.SelectionChanged += (_, _) => RefreshBoard();
        AssigneeFilterComboBox.SelectionChanged += (_, _) => RefreshBoard();
        ResetFiltersButton.Click += OnResetFiltersClicked;

        SprintFilterComboBox.SelectionChanged += OnSprintFilterChanged;

        ReloadAll();
    }

    private void ReloadAll()
    {
        _sprints = _ticketService.GetAllSprints();

        // 1. Remplir le filtre de Sprint du header
        SprintFilterComboBox.SelectionChanged -= OnSprintFilterChanged;
        SprintFilterComboBox.Items.Clear();

        SprintFilterComboBox.Items.Add(new ComboBoxItem { Content = "🌐 Tous les tickets", Tag = int.MinValue });
        SprintFilterComboBox.Items.Add(new ComboBoxItem { Content = "📦 Backlog général (Sans sprint)", Tag = -1 });

        int selectIdx = 0;
        for (int i = 0; i < _sprints.Count; i++)
        {
            var s = _sprints[i];
            var item = new ComboBoxItem
            {
                Content = $"🏃 {s.Name} ({s.State})",
                Tag = s.Id
            };
            SprintFilterComboBox.Items.Add(item);

            if (_selectedSprintFilterId.HasValue && s.Id == _selectedSprintFilterId.Value)
            {
                selectIdx = i + 2;
            }
        }

        SprintFilterComboBox.SelectedIndex = selectIdx;
        SprintFilterComboBox.SelectionChanged += OnSprintFilterChanged;

        // 2. Remplir le sélecteur de Sprint du formulaire de création
        NewTicketSprintComboBox.Items.Clear();
        NewTicketSprintComboBox.Items.Add(new ComboBoxItem { Content = "📦 Sans sprint (Backlog)", Tag = null });
        foreach (var s in _sprints)
        {
            NewTicketSprintComboBox.Items.Add(new ComboBoxItem { Content = $"🏃 {s.Name}", Tag = s.Id });
        }
        NewTicketSprintComboBox.SelectedIndex = _sprints.Any() ? 1 : 0;

        // 3. Remplir le sélecteur d'Assignés
        var assignees = _ticketService.GetAllAssignees();
        AssigneeFilterComboBox.Items.Clear();
        AssigneeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Tous" });
        foreach (var a in assignees)
        {
            AssigneeFilterComboBox.Items.Add(new ComboBoxItem { Content = a });
        }
        AssigneeFilterComboBox.SelectedIndex = 0;

        RefreshBoard();
    }

    private void OnSprintFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SprintFilterComboBox.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag is int sprintId)
            {
                _selectedSprintFilterId = sprintId == -1 ? null : (sprintId == int.MinValue ? int.MinValue : sprintId);
            }

            RefreshBoard();
        }
    }

    private void OnResetFiltersClicked(object? sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        TypeFilterComboBox.SelectedIndex = 0;
        AssigneeFilterComboBox.SelectedIndex = 0;
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        var searchTerm = SearchBox.Text?.Trim();

        TicketType? typeFilter = null;
        if (TypeFilterComboBox.SelectedItem is ComboBoxItem typeItem && typeItem.Content is string typeStr && typeStr != "Tous")
        {
            if (Enum.TryParse<TicketType>(typeStr, out var parsedType))
            {
                typeFilter = parsedType;
            }
        }

        string? assigneeFilter = null;
        if (AssigneeFilterComboBox.SelectedItem is ComboBoxItem assItem && assItem.Content is string assStr && assStr != "Tous")
        {
            assigneeFilter = assStr;
        }

        int? sprintScope = (_selectedSprintFilterId == int.MinValue) ? null : _selectedSprintFilterId;

        var tickets = _ticketService.SearchTickets(searchTerm ?? string.Empty, sprintScope, typeFilter, assigneeFilter);

        // Cartes séparées par colonnes Kanban
        var todoItems = tickets
            .Where(t => t.Status == TicketStatus.TODO || t.Status == TicketStatus.BACKLOG)
            .Select(t => new TicketCardViewModel(t))
            .ToList();

        var inProgressItems = tickets
            .Where(t => t.Status == TicketStatus.IN_PROGRESS)
            .Select(t => new TicketCardViewModel(t))
            .ToList();

        var doneItems = tickets
            .Where(t => t.Status == TicketStatus.DONE)
            .Select(t => new TicketCardViewModel(t))
            .ToList();

        TodoList.ItemsSource = todoItems;
        InProgressList.ItemsSource = inProgressItems;
        DoneList.ItemsSource = doneItems;

        HeaderTodoCount.Text = todoItems.Count.ToString();
        HeaderInProgressCount.Text = inProgressItems.Count.ToString();
        HeaderDoneCount.Text = doneItems.Count.ToString();

        // Calcul des Story Points pour le sprint courant
        int totalPoints = tickets.Sum(t => t.StoryPoints);
        SprintPointsTextBlock.Text = $"{totalPoints} pts";

        FilterStatusTextBlock.Text = $"{tickets.Count} ticket(s) filtré(s)";

        // Statistiques globales
        var all = _ticketService.GetAllTickets();
        CountTodoTextBlock.Text = all.Count(t => t.Status == TicketStatus.TODO).ToString();
        CountInProgressTextBlock.Text = all.Count(t => t.Status == TicketStatus.IN_PROGRESS).ToString();
        CountDoneTextBlock.Text = all.Count(t => t.Status == TicketStatus.DONE).ToString();
        CountBacklogTextBlock.Text = all.Count(t => t.SprintId == null).ToString();
    }

    private void OnCreateTicketClicked(object? sender, RoutedEventArgs e)
    {
        var title = NewTicketTitleTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ShowNotification("Le titre est obligatoire !", isError: true);
            return;
        }

        var description = NewTicketDescriptionTextBox.Text?.Trim();

        var priority = NewTicketPriorityComboBox.SelectedIndex switch
        {
            0 => Priority.LOW,
            2 => Priority.HIGH,
            _ => Priority.MEDIUM
        };

        var type = NewTicketTypeComboBox.SelectedIndex switch
        {
            1 => TicketType.STORY,
            2 => TicketType.BUG,
            _ => TicketType.TASK
        };

        var points = NewTicketPointsComboBox.SelectedIndex switch
        {
            0 => 1,
            1 => 2,
            3 => 5,
            4 => 8,
            5 => 13,
            _ => 3
        };

        var assignee = string.IsNullOrWhiteSpace(NewTicketAssigneeTextBox.Text) ? "Non assigné" : NewTicketAssigneeTextBox.Text.Trim();

        int? sprintId = null;
        if (NewTicketSprintComboBox.SelectedItem is ComboBoxItem { Tag: int sid })
        {
            sprintId = sid;
        }

        var ticket = _ticketService.CreateTicket(
            title,
            priority,
            description,
            sprintId,
            TicketStatus.TODO,
            type,
            points,
            assignee);

        NewTicketTitleTextBox.Text = string.Empty;
        NewTicketDescriptionTextBox.Text = string.Empty;

        ShowNotification($"Ticket #{ticket.Id} ({type}) créé avec succès !");
        RefreshBoard();
    }

    private void OnCreateSprintClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewSprintNameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowNotification("Nom de sprint manquant !", isError: true);
            return;
        }

        var goal = NewSprintGoalTextBox.Text?.Trim();
        var sprint = _ticketService.CreateSprint(name, goal, SprintState.ACTIVE);

        NewSprintNameTextBox.Text = string.Empty;
        NewSprintGoalTextBox.Text = string.Empty;

        ShowNotification($"Sprint '{sprint.Name}' créé !");
        _selectedSprintFilterId = sprint.Id;
        ReloadAll();
    }

    private void OnCompleteSprintClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedSprintFilterId.HasValue && _selectedSprintFilterId.Value > 0)
        {
            _ticketService.UpdateSprintState(_selectedSprintFilterId.Value, SprintState.COMPLETED);
            ShowNotification($"Sprint #{_selectedSprintFilterId.Value} marqué comme terminé !");
            ReloadAll();
        }
        else
        {
            ShowNotification("Sélectionnez un sprint actif pour le clôturer.", isError: true);
        }
    }

    private void OnMoveToTodoClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            _ticketService.UpdateTicketStatus(id, TicketStatus.TODO);
            RefreshBoard();
        }
    }

    private void OnMoveToInProgressClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            _ticketService.UpdateTicketStatus(id, TicketStatus.IN_PROGRESS);
            RefreshBoard();
        }
    }

    private void OnMoveToDoneClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            _ticketService.CloseTicket(id);
            RefreshBoard();
        }
    }

    private void OnSprintSelectorClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            var ticket = _ticketService.GetTicketById(id);
            if (ticket == null || !_sprints.Any()) return;

            int? nextSprintId = null;
            if (ticket.SprintId == null)
            {
                nextSprintId = _sprints.First().Id;
            }
            else
            {
                var curIdx = _sprints.FindIndex(s => s.Id == ticket.SprintId);
                if (curIdx >= 0 && curIdx < _sprints.Count - 1)
                {
                    nextSprintId = _sprints[curIdx + 1].Id;
                }
                else
                {
                    nextSprintId = null;
                }
            }

            _ticketService.MoveTicketToSprint(id, nextSprintId);
            var targetName = nextSprintId.HasValue
                ? _sprints.FirstOrDefault(s => s.Id == nextSprintId)?.Name
                : "Backlog";
            ShowNotification($"Ticket #{id} déplacé vers : {targetName}");
            RefreshBoard();
        }
    }

    private void OnDeleteTicketClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int id })
        {
            _ticketService.DeleteTicket(id);
            ShowNotification($"Ticket #{id} supprimé.");
            RefreshBoard();
        }
    }

    private void ShowNotification(string message, bool isError = false)
    {
        NotificationLabel.Text = message;
        NotificationLabel.Foreground = isError ? Brushes.Tomato : Brushes.MediumSeaGreen;
    }
}

public class TicketCardViewModel
{
    public Ticket Ticket { get; }

    public TicketCardViewModel(Ticket ticket)
    {
        Ticket = ticket;
    }

    public string SprintTag => Ticket.Sprint != null ? $"⚡ {Ticket.Sprint.Name}" : "📦 Backlog";

    public string StoryPointsTag => $"{Ticket.StoryPoints} pts";

    public string AssigneeInitial => string.IsNullOrWhiteSpace(Ticket.Assignee) ? "?" : Ticket.Assignee[..1].ToUpper();

    public string DescriptionPreview => string.IsNullOrWhiteSpace(Ticket.Description)
        ? "Aucune description"
        : (Ticket.Description.Length > 90 ? Ticket.Description[..90] + "..." : Ticket.Description);

    public IBrush PriorityBadgeColor => Ticket.Priority switch
    {
        Priority.HIGH => Brush.Parse("#DC2626"),    // Rouge vif
        Priority.MEDIUM => Brush.Parse("#D97706"),  // Orange ambre
        Priority.LOW => Brush.Parse("#2563EB"),     // Bleu
        _ => Brushes.Gray
    };

    public IBrush TypeBadgeColor => Ticket.Type switch
    {
        TicketType.BUG => Brush.Parse("#BE123C"),    // Rose rouge foncé
        TicketType.STORY => Brush.Parse("#059669"),  // Vert émeraude
        TicketType.TASK => Brush.Parse("#4F46E5"),   // Indigo
        _ => Brushes.DarkSlateGray
    };
}
