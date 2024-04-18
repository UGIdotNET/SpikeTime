namespace UGIdotNET.SpikeTime.FastEndpoints;

public class MyResponse
{
    public string FullName { get; set; } = string.Empty;

    public bool IsOver18 { get; set; }

    public long Ticks { get; set; }
}
