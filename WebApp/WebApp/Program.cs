using Prometheus;
using WebApp.Keycloak;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // BeanApp has no RabbitMQ plumbing (service-api-template plan Q1) — it only
            // calls OrderBeans synchronously over HTTP with a Keycloak client-credentials token.
            builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
            builder.Services.AddHttpClient("Keycloak");
            builder.Services.AddHttpClient("OrderBeans", client =>
                client.BaseAddress = new Uri(builder.Configuration["OrderBeans:BaseUrl"]
                    ?? throw new InvalidOperationException("Configuration 'OrderBeans:BaseUrl' not found.")));
            builder.Services.AddSingleton<KeycloakTokenClient>();

            builder.Services.AddHealthChecks()
                .AddCheck("self-live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
                .AddCheck("self-ready", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["ready"]);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseHttpMetrics();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapMetrics();
            app.MapHealthChecks("/healthz/live", new() { Predicate = c => c.Tags.Contains("live") });
            app.MapHealthChecks("/healthz/ready", new() { Predicate = c => c.Tags.Contains("ready") });

            app.Run();
        }
    }
}
