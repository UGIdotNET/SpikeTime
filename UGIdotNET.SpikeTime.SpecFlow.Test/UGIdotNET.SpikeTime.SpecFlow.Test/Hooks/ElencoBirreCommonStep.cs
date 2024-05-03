using UGIdotNET.SpikeTime.SpecFlow.Test.Domain;

namespace UGIdotNET.SpikeTime.SpecFlow.Test.Hooks;

[Binding]
public class ElencoBirreCommonStep
{
    public static Carrello Carrello { get; set; } = new Carrello();

    public static int NumeroDiBirreInizialeNelCarrello { get; set; }

    public static IEnumerable<Birra> Birre { get; set; } = Enumerable.Empty<Birra>();

    [BeforeScenario("PreparaElencoBirre", Order = 0)]
    public void GivenLelencoDiBirre()
    {
        Birre = new[]
        {
            new Birra{ Tipo = "Bionda" },
            new Birra{ Tipo = "Rossa" },
            new Birra{ Tipo = "Scura" }
        };

        NumeroDiBirreInizialeNelCarrello = Carrello.Birre.Count();
    }
}
