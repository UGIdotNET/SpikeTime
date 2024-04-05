namespace SpikeTime.CatalogModels;

public record ProductDetail(
    Guid Id,
    string Name,
    decimal Price,
    string Description);
