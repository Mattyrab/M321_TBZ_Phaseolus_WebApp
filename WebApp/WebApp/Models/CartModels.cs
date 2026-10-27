namespace WebApp.Models;

public record CartLine(string Name, decimal Price, int Quantity)
{
    public decimal LineTotal => Price * Quantity;
}
