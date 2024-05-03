using UGIdotNET.SpikeTime.SpecFlow.Test.Hooks;

namespace UGIdotNET.SpikeTime.SpecFlow.Test.StepDefinitions
{
    [Binding]
    public class CarrelloDelleBirreStepDefinitions
    {

        [When(@"Seleziono la birra (.*)")]
        public void WhenSelezionoLaBirraBionda(string tipoDiBirra)
        {
            var birraScelta = ElencoBirreCommonStep.Birre.First(b => b.Tipo.Equals(tipoDiBirra, StringComparison.InvariantCultureIgnoreCase));
            ElencoBirreCommonStep.Carrello.AggiungiBirra(birraScelta);
        }

        [Then(@"Il numero di elementi nel carrello deve aumentare")]
        public void ThenIlNumeroDiElementiNelCarrelloDeveAumentare()
        {
            ElencoBirreCommonStep.Carrello.Birre.Count()
                .Should()
                .Be(ElencoBirreCommonStep.NumeroDiBirreInizialeNelCarrello + 1);
        }
    }
}
