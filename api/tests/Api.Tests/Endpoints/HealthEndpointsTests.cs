using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Api.Tests.Endpoints
{
    public class HealthEndpointsTests
    {
        [Fact]
        public async Task Get_Health_Returns200()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.UseEnvironment("Testing"));

            using var client = factory.CreateClient();
            var resp = await client.GetAsync("/health");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

            var body = await resp.Content.ReadAsStringAsync();
            body.Should().NotBeNullOrWhiteSpace();
        }
    }
}
