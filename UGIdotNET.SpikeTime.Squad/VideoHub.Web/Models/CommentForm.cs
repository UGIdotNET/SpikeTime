using System.ComponentModel.DataAnnotations;

namespace VideoHub.Web.Models;

public class CommentForm
{
    [Required(ErrorMessage = "Inserisci il tuo nome.")]
    [StringLength(80, ErrorMessage = "Il nome può contenere al massimo 80 caratteri.")]
    public string Author { get; set; } = "";

    [Required(ErrorMessage = "Scrivi un commento.")]
    [StringLength(1000, ErrorMessage = "Il commento può contenere al massimo 1000 caratteri.")]
    public string Text { get; set; } = "";

    // Honeypot: hidden from users, must stay empty.
    public string Website { get; set; } = "";
}
