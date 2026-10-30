namespace WebApp.Models;

public record OrderItem(string Name, int Quantity);

// matches OrderBeans' CreateOrderRequest shape (no shared package — architecture plan Q2)
public record CreateOrderRequest(List<OrderItem> Items, decimal TotalAmount, string Email, string? DeliveryNote = null);
