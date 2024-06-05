namespace TODO.Models;
using System;
using System.ComponentModel.DataAnnotations;
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
}

