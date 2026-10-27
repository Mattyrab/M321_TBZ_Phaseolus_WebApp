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
            var client = _httpClientFactory.CreateClient("BeanNotifs");

            try
            {
                var result = await client.GetFromJsonAsync<List<NotificationRecord>>("/notifications");
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
