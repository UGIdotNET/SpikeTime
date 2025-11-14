using Microsoft.Data.SqlTypes;

namespace UGIdotNET.SpikeTime.VectorSql.Data;

public class Episode
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SqlVector<float> Embedding { get; set; }
}
