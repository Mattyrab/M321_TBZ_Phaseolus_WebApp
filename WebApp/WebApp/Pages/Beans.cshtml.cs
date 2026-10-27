using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Cart;
using WebApp.Models;

namespace WebApp.Pages
{
    public class BeansModel : PageModel
    {
        public IReadOnlyList<MenuItem> Menu => WebApp.Models.Menu.Items;

        [TempData]
        public string? AddedMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPostAddToCart(string name, decimal price)
        {
            HttpContext.Session.AddToCart(name, price);
            AddedMessage = $"Added {name} to your cart.";
            return RedirectToPage();
        }
    }
}
