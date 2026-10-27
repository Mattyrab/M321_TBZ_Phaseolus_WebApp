using System.Text.Json;
using WebApp.Models;

namespace WebApp.Cart;

public static class CartSessionExtensions
{
    private const string SessionKey = "cart";

    public static List<CartLine> GetCart(this ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? new List<CartLine>() : JsonSerializer.Deserialize<List<CartLine>>(json)!;
    }

    public static void AddToCart(this ISession session, string name, decimal price)
    {
        var cart = session.GetCart();
        var index = cart.FindIndex(line => line.Name == name);

        if (index >= 0)
        {
            cart[index] = cart[index] with { Quantity = cart[index].Quantity + 1 };
        }
        else
        {
            cart.Add(new CartLine(name, price, 1));
        }

        session.SetString(SessionKey, JsonSerializer.Serialize(cart));
    }

    public static void ClearCart(this ISession session) => session.Remove(SessionKey);
}
