using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TestHelpers.Common.Testing;

namespace Api.Tests.Realtime
{
    [IntegrationTest]
    public sealed class ProjectsHubAuthorizationTests
    {
        [Fact]
        public async Task StartAsync_Fails_With_Forbidden_When_ProjectReader_Policy_Not_Satisfied()
        {
            await using var factory = new UnauthorizedClientFactory();
            var baseAddress = factory.Server.BaseAddress;

            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(baseAddress, "/hubs/projects"), o =>
                {
                    // Use TestServer handler
                    o.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    // LongPolling is the most reliable in TestServer
                    o.Transports = HttpTransportType.LongPolling;
                })
                .Build();

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        private sealed class UnauthorizedClientFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, NoReaderAuthHandler>("Test", _ =>
                        {
                            // Provide TimeProvider to avoid obsolete clock
                            _.TimeProvider = TimeProvider.System;
                        });

                    services.PostConfigureAll<AuthenticationOptions>(o =>
                    {
                        o.DefaultScheme = "Test";
                        o.DefaultAuthenticateScheme = "Test";
                        o.DefaultChallengeScheme = "Test";
                    });
                });
            }
        }

        // Auth handler that AUTHENTICATES but lacks the claims for ProjectReader policy.
        private sealed class NoReaderAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
        {
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var identity = new ClaimsIdentity("Test"); // no required claims
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "Test");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }
    }
}
