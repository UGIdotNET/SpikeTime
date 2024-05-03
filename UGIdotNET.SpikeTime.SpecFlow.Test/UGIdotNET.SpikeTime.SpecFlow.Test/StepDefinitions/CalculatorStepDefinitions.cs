using System;
using TechTalk.SpecFlow;

namespace UGIdotNET.SpikeTime.SpecFlow.Test.StepDefinitions
{
    [Binding]
    public class CalculatorStepDefinitions
    {
        private int firstNumber;

        private int secondNumber;

        private int result;

        [Given(@"the first number is (.*)")]
        public void GivenTheFirstNumberIs(int p0)
        {
            firstNumber = p0;
        }


        [Given(@"the second number is (.*)")]
        public void GivenTheSecondNumberIs(int p0)
        {
            secondNumber = p0;
        }

        [When(@"the two numbers are added")]
        public void WhenTheTwoNumbersAreAdded()
        {
            result = firstNumber + secondNumber;
        }

        [Then(@"the result should be (.*)")]
        public void ThenTheResultShouldBe(int p0)
        {
            result.Should().Be(p0);
        }
    }
}
