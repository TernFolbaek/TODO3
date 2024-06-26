namespace TODO.Models;
using System;
using System.Collections.Generic;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public List<UserTodo> UserTodos { get; set; } = new List<UserTodo>();
    public string RefreshToken { get; set; } 
    public DateTime RefreshTokenExpiryTime { get; set; } 
}