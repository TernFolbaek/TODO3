namespace TODO.Models;
using System;
using System.Collections.Generic;

public class TodoItem
{
    public int Id { get; set; }
    public string Description { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public List<UserTodo> UserTodos { get; set; } = new List<UserTodo>();
    public string Status { get; set; } = "Pending"; 
    public DateTimeOffset? DateCompleted { get; set; } = null;
}
