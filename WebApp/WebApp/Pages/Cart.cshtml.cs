using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Cart;
using WebApp.Models;

namespace WebApp.Pages
{
    public class CartModel : PageModel
    {
        public List<CartLine> Lines { get; private set; } = new();
        public decimal Total => Lines.Sum(line => line.LineTotal);

        public void OnGet()
        {
            Lines = HttpContext.Session.GetCart();
        }
    }
}
