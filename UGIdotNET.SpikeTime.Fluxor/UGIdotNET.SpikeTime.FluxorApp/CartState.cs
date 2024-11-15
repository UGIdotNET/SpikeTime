using System;
using Fluxor;

namespace UGIdotNET.SpikeTime.FluxorApp;

[FeatureState]
public record CartState
{
    public IReadOnlyList<Product> AvailableProducts { get; init; } = [];

    public IReadOnlyList<Product> Products = [];

    public int NumberOfProducts => Products.Count;

    public decimal TotalPrice => Products.Sum(p => p.Price);
}

public class CartEffect
{
    private readonly ProductsService _productsService;
    public CartEffect(ProductsService productsService)
    {
        _productsService = productsService;
    }

    [EffectMethod]
    public async Task HandleLoadProductsAction(LoadProductsAction action, IDispatcher dispatcher)
    {
        var products = await _productsService.GetProductsAsync();
        dispatcher.Dispatch(new ProductsLoadedAction(products));
    }
}