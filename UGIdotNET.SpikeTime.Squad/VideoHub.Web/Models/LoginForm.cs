using System.ComponentModel.DataAnnotations;

namespace VideoHub.Web.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Inserisci l'email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Inserisci la password.")]
    public string Password { get; set; } = "";
}
