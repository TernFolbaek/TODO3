namespace TODO.Models;

public class UserTodo
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int TodoId { get; set; }
    public TodoItem TodoItem { get; set; }
}