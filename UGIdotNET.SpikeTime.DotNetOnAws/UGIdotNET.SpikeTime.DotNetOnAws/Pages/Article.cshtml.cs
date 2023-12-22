using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UGIdotNET.SpikeTime.DotNetOnAws.Pages;

public class ArticleModel : PageModel
{
    [BindProperty]
    public ViewModel Article { get; set; } = new();

    public void OnGet()
    {
        Article = new ViewModel
        {
            Title = "Pubblichiamo la nostra prima applicazione su AWS",
            Author = "UGIdotNET",
            Content = "Installa l'estensione di AWS per Visual Studio. Crea un nuovo progetto, tasto destro sul progetto 'Publish to AWS' ed è fatta"
        };
    }

    public class ViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;
    }
}
