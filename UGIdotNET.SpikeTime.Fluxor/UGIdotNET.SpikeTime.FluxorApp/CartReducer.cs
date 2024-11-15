using System;
using Fluxor;

namespace UGIdotNET.SpikeTime.FluxorApp;

public static class CartReducer
{
    [ReducerMethod]
    public static CartState AddProductToCartReducer(
        CartState state,
        AddProductToCartAction action)
    {
        return state with
        {
            Products = state.Products.Append(action.Product).ToList()
        };
    }

    [ReducerMethod]
    public static CartState RemoveProductFromCartReducer(
        CartState state,
        RemoveProductFromCartAction action)
    {
        return state with
        {
            Products = state.Products.Where(p => p != action.Product).ToList()
        };
    }

    [ReducerMethod]
    public static CartState LoadProductsReducer(
        CartState state,
        ProductsLoadedAction action)
    {
        return state with
        {
            AvailableProducts = action.Products.ToList()
        };
    }
}
