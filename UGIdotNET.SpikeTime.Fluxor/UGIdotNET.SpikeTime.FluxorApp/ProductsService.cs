using System;

namespace UGIdotNET.SpikeTime.FluxorApp;

public class ProductsService
{
    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        await Task.Delay(1000);
        return await Task.FromResult(ProductStore.ProductsCatalog);
    }
}
