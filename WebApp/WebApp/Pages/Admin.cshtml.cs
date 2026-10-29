using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages
{
    [Authorize(Roles = "admin")]
    public class AdminModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<NotificationRecord> Notifications { get; private set; } = new();
        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            // SSO plan Step 4: forward the logged-in admin's own Keycloak token
            // (not a service-level credential) so BeanNotifs can check the
            // admin's actual realm role, not just "a request came from BeanApp."
            var token = await HttpContext.GetTokenAsync("access_token");

            var client = _httpClientFactory.CreateClient("BeanNotifs");
            using var message = new HttpRequestMessage(HttpMethod.Get, "/notifications");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(message);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "Your session has expired — please log in again.";
                    return;
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<List<NotificationRecord>>();
                Notifications = (result ?? new())
                    .OrderByDescending(n => n.OccurredAt)
                    .ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Could not reach BeanNotifs.";
            }
        }

        public record NotificationRecord(Guid OrderId, string Type, string Message, DateTimeOffset OccurredAt);
    }
}
