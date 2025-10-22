using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Api.Tests.Endpoints
{
    public class HealthEndpointsTests
    {
        [Fact]
        public async Task Get_Health_Returns_200_And_Valid_Payload()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.UseEnvironment("Testing"));

            using var client = factory.CreateClient();

            var resp = await client.GetAsync("/health");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

            // Must contain required properties
            json.TryGetProperty("status", out var status).Should().BeTrue();
            json.TryGetProperty("uptime", out var uptime).Should().BeTrue();
            json.TryGetProperty("serverTimeUtc", out var serverTimeUtc).Should().BeTrue();

            // Must report healthy
            status.GetString().Should().Be("Healthy");

            // Uptime format must be d.hh:mm:ss
            var uptimeStr = uptime.GetString();
            uptimeStr.Should().NotBeNullOrWhiteSpace();
            Regex.IsMatch(uptimeStr!, @"^\d+\.\d{2}:\d{2}:\d{2}$").Should().BeTrue();

            // Server time must be close to now (6s tolerance)
            var serverNow = serverTimeUtc.GetDateTimeOffset();
            (DateTimeOffset.UtcNow - serverNow).Duration().Should().BeLessThan(TimeSpan.FromSeconds(6));
        }
    }
}
