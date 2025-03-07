using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using UGIdotNET.SpikeTime.AspireAzure.Entities;

namespace UGIdotNET.SpikeTime.AspireAzure.Web;

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

    public async Task UploadProductImageAsync(Guid productId, IBrowserFile image)
    {
        using var stream = image.OpenReadStream();

        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

        var content = new MultipartFormDataContent
        {
            { streamContent, "image", image.Name }
        };

        var response = await httpClient.PostAsync($"api/products/{productId}/image", content);
        response.EnsureSuccessStatusCode();
    }
}
