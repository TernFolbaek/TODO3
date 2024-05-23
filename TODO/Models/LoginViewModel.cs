namespace TODO.Models;
using System.ComponentModel.DataAnnotations;
public class LoginViewModel
{
    [Required(ErrorMessage = "The Username field is required.")]
    [StringLength(100, ErrorMessage = "Username must be less than 100 characters")]
    public string Username { get; set; }
    
    [Required(ErrorMessage = "The Password field is required.")]
    public string Password { get; set; }
}