using System;

namespace UGIdotNET.SpikeTime.FluxorApp;

public record LoadProductsAction
{
}

public record ProductsLoadedAction(IEnumerable<Product> Products);
