using System.ComponentModel;

namespace UGIdotNET.SpikeTime.SpecFlow.Test.Domain;

public class Carrello
{
    private List<ElementoCarrello> _birre = new();

    public IEnumerable<ElementoCarrello> Birre => _birre;

    public void AggiungiBirra(Birra birra)
    {
        var birraEsistente = _birre.FirstOrDefault(b => b.Birra.Tipo == birra.Tipo);
        if (birraEsistente is null)
        {
            _birre.Add(new()
            {
                Birra = birra,
                Quantita = 1
            });
        }
        else
        {
            birraEsistente.Quantita += 1;
        }
    }

    public void SvuotaCarrello() => _birre.Clear();

    public record ElementoCarrello
    {
        public Birra Birra { get; init; }

        public int Quantita { get; set; } = 0;
    }
}
