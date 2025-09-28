using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Api.Tests.Endpoints
{
    public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HealthEndpointsTests(WebApplicationFactory<Program> factory)
            => _factory = factory.WithWebHostBuilder(b => b.UseSetting("ENVIRONMENT", "Testing"));

        [Fact]
        public async Task Get_Health_Returns200()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
