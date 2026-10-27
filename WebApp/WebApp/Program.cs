using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
            builder.Services.AddHttpClient("BeanNotifs", client =>
                client.BaseAddress = new Uri(builder.Configuration["BeanNotifs:BaseUrl"]
                    ?? throw new InvalidOperationException("Configuration 'BeanNotifs:BaseUrl' not found.")));
            builder.Services.AddSingleton<KeycloakTokenClient>();

            var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
                ?? throw new InvalidOperationException("Configuration section 'Keycloak' not found.");

            // Admin login (architecture plan admin-login follow-up): Keycloak Authorization
            // Code flow, cookie-backed session, realm_access.roles mapped to ClaimTypes.Role
            // so [Authorize(Roles = "admin")] works on the Admin page.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie()
                .AddOpenIdConnect(options =>
                {
                    options.Authority = keycloakOptions.Authority;
                    options.ClientId = keycloakOptions.ClientId;
                    options.ClientSecret = keycloakOptions.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UsePkce = true;
                    options.SaveTokens = true;
                    options.RequireHttpsMetadata = false; // in-cluster Keycloak is HTTP-only
                    options.MapInboundClaims = false;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");

                    options.Events = new OpenIdConnectEvents
                    {
                        // The backend reaches Keycloak over the in-cluster Service DNS name
                        // (Authority), but the browser can only reach it via the dev
                        // port-forward — rewrite the front-channel redirect accordingly.
                        OnRedirectToIdentityProvider = ctx => RewriteToPublicAuthority(ctx.ProtocolMessage, keycloakOptions),
                        OnRedirectToIdentityProviderForSignOut = ctx => RewriteToPublicAuthority(ctx.ProtocolMessage, keycloakOptions),
                        OnTokenValidated = ctx =>
                        {
                            // Keycloak puts realm roles in "realm_access.roles", which isn't
                            // a claim ASP.NET Core maps to ClaimTypes.Role on its own.
                            if (ctx.Principal?.Identity is not ClaimsIdentity identity)
                            {
                                return Task.CompletedTask;
                            }

                            var realmAccess = identity.FindFirst("realm_access")?.Value;
                            if (realmAccess is null)
                            {
                                return Task.CompletedTask;
                            }

                            using var doc = JsonDocument.Parse(realmAccess);
                            if (doc.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                                }
                            }

                            return Task.CompletedTask;
                        },
                    };
                });

            builder.Services.AddAuthorization();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapMetrics();
            app.MapHealthChecks("/healthz/live", new() { Predicate = c => c.Tags.Contains("live") });
            app.MapHealthChecks("/healthz/ready", new() { Predicate = c => c.Tags.Contains("ready") });

            app.Run();
        }

        private static Task RewriteToPublicAuthority(OpenIdConnectMessage message, KeycloakOptions options)
        {
            if (!string.IsNullOrEmpty(options.PublicAuthority))
            {
                message.IssuerAddress = message.IssuerAddress.Replace(options.Authority, options.PublicAuthority);
            }

            return Task.CompletedTask;
        }
    }
}
