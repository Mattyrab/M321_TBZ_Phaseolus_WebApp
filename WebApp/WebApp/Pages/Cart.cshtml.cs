using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Keycloak;
using WebApp.Models;

namespace WebApp.Pages
{
    public class CartModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KeycloakTokenClient _tokenClient;

        public CartModel(IHttpClientFactory httpClientFactory, KeycloakTokenClient tokenClient)
        {
            _httpClientFactory = httpClientFactory;
            _tokenClient = tokenClient;
        }

        [BindProperty]
        public string ItemName { get; set; } = "";

        [BindProperty]
        public int Quantity { get; set; } = 1;

        [BindProperty]
        public decimal TotalAmount { get; set; }

        public string? ResultMessage { get; private set; }
        public bool OrderSucceeded { get; private set; }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            var request = new CreateOrderRequest(
                new List<OrderItem> { new(ItemName, Quantity) },
                TotalAmount);

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
                ? "Order placed! OrderBeans accepted it."
                : $"OrderBeans rejected the order ({(int)response.StatusCode}).";
        }
    }
}
