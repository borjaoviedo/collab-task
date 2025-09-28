using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
            resp.EnsureSuccessStatusCode();
        }
    }
}
