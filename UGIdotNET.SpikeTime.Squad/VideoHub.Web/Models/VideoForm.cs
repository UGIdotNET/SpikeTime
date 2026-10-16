using System.ComponentModel.DataAnnotations;

namespace VideoHub.Web.Models;

public class VideoForm
{
    [Required(ErrorMessage = "Il titolo è obbligatorio.")]
    [StringLength(200, ErrorMessage = "Il titolo può contenere al massimo 200 caratteri.")]
    public string Title { get; set; } = "";

    [StringLength(2000, ErrorMessage = "La descrizione può contenere al massimo 2000 caratteri.")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Il link al video è obbligatorio.")]
    [StringLength(2000, ErrorMessage = "Il link può contenere al massimo 2000 caratteri.")]
    public string Url { get; set; } = "";
}
