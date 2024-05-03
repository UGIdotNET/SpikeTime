using System;
using TechTalk.SpecFlow;
using UGIdotNET.SpikeTime.SpecFlow.Test.Hooks;

namespace UGIdotNET.SpikeTime.SpecFlow.Test.StepDefinitions
{
    [Binding]
    public class TotaliCarrelloStepDefinitions
    {
        private int quantitaBirraBiondaNelCarrello;

        [Given(@"Un carrello con una birra bionda")]
        public void GivenUnCarrelloConUnaBirraBionda()
        {
            ElencoBirreCommonStep.Carrello.SvuotaCarrello();
            ElencoBirreCommonStep.Carrello.AggiungiBirra(
                ElencoBirreCommonStep.Birre.First(b => b.Tipo == "Bionda"));

            quantitaBirraBiondaNelCarrello = ElencoBirreCommonStep.Carrello.Birre
                .First(b => b.Birra.Tipo == "Bionda")
                .Quantita;
        }

        [When(@"Aggiungo di nuovo la stessa birra")]
        public void WhenAggiungoDiNuovoLaStessaBirra()
        {
            var birraBionda = ElencoBirreCommonStep.Birre.First(b => b.Tipo == "Bionda");
            ElencoBirreCommonStep.Carrello.AggiungiBirra(birraBionda);
        }

        [Then(@"Deve incrementare la quantità dell'elemento nel carrello")]
        public void ThenDeveIncrementareLaQuantitaDellelementoNelCarrello()
        {
            var birraBiondaNelCarrello = ElencoBirreCommonStep.Carrello.Birre
                .First(b => b.Birra.Tipo == "Bionda");

            birraBiondaNelCarrello.Quantita
                .Should()
                .Be(quantitaBirraBiondaNelCarrello + 1);
        }
    }
}
