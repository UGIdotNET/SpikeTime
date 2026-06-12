namespace UGIdotNET.SpikeTime.SqlEvent.Data;

public class Order
{
    public Guid Id { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public OrderStatus? Status { get; set; }

    public enum OrderStatus
    {
        Sent,
        Accepted,
        Shipped
    }
}
