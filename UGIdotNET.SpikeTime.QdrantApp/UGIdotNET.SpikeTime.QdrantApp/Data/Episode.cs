using Microsoft.Extensions.VectorData;

namespace UGIdotNET.SpikeTime.QdrantApp.Data;

public class Episode
{
    [VectorStoreKey]
    public ulong Id { get; set; }

    [VectorStoreData(IsIndexed = true, StorageName = "title")]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true, StorageName = "description")]
    public string Description { get; set; } = string.Empty;

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}

public class EpisodeListModel
{
    public List<Episode> Episodes { get; init; } = [];
}
