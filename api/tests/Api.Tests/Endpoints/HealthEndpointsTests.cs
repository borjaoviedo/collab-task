using Api.Tests.Testing;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using TestHelpers.Common.Testing;

namespace Api.Tests.Endpoints
{
    [IntegrationTest]
    public class HealthEndpointsTests
    {
        [Fact]
        public async Task Get_Health_Returns_200_And_Valid_Payload()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

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

            // Server time must be close to now
            var serverNow = serverTimeUtc.GetDateTimeOffset();
            (DateTimeOffset.UtcNow - serverNow).Duration().Should().BeLessThan(TimeSpan.FromSeconds(10));
        }
    }
}
