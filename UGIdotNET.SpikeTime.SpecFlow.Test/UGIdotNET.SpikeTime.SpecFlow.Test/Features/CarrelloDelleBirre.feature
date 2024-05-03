Feature: CarrelloDelleBirre

A short summary of the feature

@PreparaElencoBirre
Scenario: Quando aggiungo una birra al carrello deve aumentare il numero degli elementi nel carrello
	When Seleziono la birra rossa
	Then Il numero di elementi nel carrello deve aumentare

@PreparaElencoBirre
Scenario: Quando aggiungo di nuovo la stessa birra deve incrementare la quantità dell'elemento nel carrello
	Given Un carrello con una birra bionda
	When Aggiungo di nuovo la stessa birra
	Then Deve incrementare la quantità dell'elemento nel carrello
