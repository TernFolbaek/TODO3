namespace TODO.Models;
using System;
using System.Collections.Generic;

public class TodoItem
{
    public int Id { get; set; }
    public string Description { get; set; }
    public bool IsComplete { get; set; }
    private DateTimeOffset? _dueDate;
    public DateTimeOffset? DueDate
    {
        get => _dueDate;
        set => _dueDate = value; 
    }
    public List<UserTodo> UserTodos { get; set; } = new List<UserTodo>();
}

