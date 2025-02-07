using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace UGIdotNET.SpikeTime.SemanticKernel;

public class PizzaPlugin
{
    private readonly string[] ingredients = [
        "pomodoro",
        "mozzarella",
        "funghi",
        "prosciutto cotto",
        "prosciutto crudo",
        "burrata"
    ];

    private Order order = new();

    [KernelFunction("get_order")]
    [Description("Ottiene l'ordine corrente")]
    public async Task<Order> GetOrderAsync()
    {
        await Task.Delay(1000);
        return order;
    }

    [KernelFunction("add_ingredients")]
    [Description("Aggiunge gli ingredienti alla pizza dell'ordine")]
    public async Task<Order> AddIngredientsAsync(string[] ingredients)
    {
        await Task.Delay(1000);
        order.Pizza.Ingredients.AddRange(ingredients);

        return order;
    }

    [KernelFunction("change_order_status")]
    [Description("Cambia lo stato dell'ordine corrente")]
    public async Task<Order> ChangeOrderStatusAsync(Order.OrderStatus orderStatus)
    {
        await Task.Delay(1000);

        order.Status = orderStatus;
        return order;
    }

    [KernelFunction("get_order_status")]
    [Description("Ottiene lo stato dell'ordine corrente")]
    public async Task<Order.OrderStatus> GetOrderStatusAsync()
    {
        await Task.Delay(1000);
        return order.Status;
    }

    [KernelFunction("create_order")]
    [Description("Crea un nuovo ordine")]
    public async Task<Order> CreateNewOrderAsync(string[] ingredients)
    {
        await Task.Delay(1000);

        order = new();
        if (ingredients.Length > 0)
        {
            order.Pizza.Ingredients.AddRange(ingredients);
        }

        return order;
    }

    public class Order
    {
        public Pizza Pizza { get; set; } = new();

        public OrderStatus Status { get; set; }

        public enum OrderStatus
        {
            Pending,
            Delivery,
            Delivered
        }
    }

    public class Pizza
    {
        public List<string> Ingredients { get; set; } = ["pomodoro", "mozzarella"];
    }
}