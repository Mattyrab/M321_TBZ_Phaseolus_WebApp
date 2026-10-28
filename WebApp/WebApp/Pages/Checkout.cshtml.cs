using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Cart;
using WebApp.Keycloak;
using WebApp.Models;

namespace WebApp.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KeycloakTokenClient _tokenClient;

        public CheckoutModel(IHttpClientFactory httpClientFactory, KeycloakTokenClient tokenClient)
        {
            _httpClientFactory = httpClientFactory;
            _tokenClient = tokenClient;
        }

        [BindProperty]
        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = "";

        public List<CartLine> Lines { get; private set; } = new();
        public decimal Total => Lines.Sum(line => line.LineTotal);

        public string? ResultMessage { get; private set; }
        public bool OrderSucceeded { get; private set; }

        public IActionResult OnGet()
        {
            Lines = HttpContext.Session.GetCart();

            if (Lines.Count == 0)
            {
                return RedirectToPage("/Cart");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostPayAsync()
        {
            Lines = HttpContext.Session.GetCart();

            if (Lines.Count == 0)
            {
                return RedirectToPage("/Cart");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new CreateOrderRequest(
                Lines.Select(line => new OrderItem(line.Name, line.Quantity)).ToList(),
                Total,
                Email);

            var token = await _tokenClient.GetAccessTokenAsync();

            var client = _httpClientFactory.CreateClient("OrderBeans");
            using var message = new HttpRequestMessage(HttpMethod.Post, "/orders")
            {
                Content = JsonContent.Create(request),
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(message);

            OrderSucceeded = response.IsSuccessStatusCode;
            ResultMessage = OrderSucceeded
                ? $"Payment accepted! A receipt will be sent to {Email}."
                : $"OrderBeans rejected the order ({(int)response.StatusCode}).";

            if (OrderSucceeded)
            {
                HttpContext.Session.ClearCart();
                Lines = new List<CartLine>();
            }

            return Page();
        }
    }
}
