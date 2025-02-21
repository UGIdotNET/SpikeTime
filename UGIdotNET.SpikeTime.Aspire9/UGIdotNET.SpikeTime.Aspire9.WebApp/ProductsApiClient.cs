using UGIdotNET.SpikeTime.Aspire9.Entities;

namespace UGIdotNET.SpikeTime.Aspire9.WebApp;

public class ProductsApiClient(HttpClient httpClient)
{
    public async Task<Product[]> GetProductsAsync()
    {
        var products = await httpClient.GetFromJsonAsync<Product[]>("api/products");
        return products ?? [];
    }

    public async Task<Product?> GetProductAsync(Guid id)
    {
        var product = await httpClient.GetFromJsonAsync<Product>($"api/products/{id}");
        return product;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var response = await httpClient.PostAsJsonAsync("api/products", product);
        response.EnsureSuccessStatusCode();

        var productCreated = await response.Content.ReadFromJsonAsync<Product>();
        return productCreated!;
    }
}
