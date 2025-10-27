using Api.Tests.Testing;
using Application.Columns.DTOs;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers.Api;

namespace Api.Tests.Integration
{
    public class ETagIfMatchFlowTests
    {
        [Fact]
        public async Task Column_full_if_match_cycle()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            // 1) Create project
            var createPrj = await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "Team" });
            createPrj.EnsureSuccessStatusCode();
            var prj = await createPrj.Content.ReadFromJsonAsync<ProjectReadDto>(EndpointsTestHelper.Json);
            prj.Should().NotBeNull();
            var projectId = prj!.Id;

            // 2) Create lane
            var createLane = await client.PostAsJsonAsync($"/projects/{projectId}/lanes", new LaneCreateDto { Name = "Backlog", Order = 0 });
            createLane.EnsureSuccessStatusCode();
            var lane = await createLane.Content.ReadFromJsonAsync<LaneReadDto>(EndpointsTestHelper.Json);
            lane.Should().NotBeNull();
            var laneId = lane!.Id;

            // 3) Create column
            var createCol = await client.PostAsJsonAsync($"/projects/{projectId}/lanes/{laneId}/columns", new ColumnCreateDto { Name = "Todo", Order = 0 });
            createCol.EnsureSuccessStatusCode();
            var col = await createCol.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);
            col.Should().NotBeNull();
            var columnId = col!.Id;

            // 4) GET column -> capture ETag A
            var getCol = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}");
            getCol.EnsureSuccessStatusCode();
            var etagA = getCol.Headers.ETag?.Tag;
            etagA.Should().NotBeNullOrEmpty();

            // 5) PUT rename with If-Match = A -> success
            using var reqOk = new HttpRequestMessage(HttpMethod.Put, $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename")
            {
                Content = JsonContent.Create(new ColumnRenameDto { NewName = "In Progress" })
            };
            EndpointsTestHelper.SetIfMatchFromETag(reqOk, etagA!);

            var putOk = await client.SendAsync(reqOk);
            putOk.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            // 6) GET column again -> ETag B (must differ from A)
            var getCol2 = await client.GetAsync($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}");
            getCol2.EnsureSuccessStatusCode();
            var etagB = getCol2.Headers.ETag?.Tag;
            etagB.Should().NotBeNullOrEmpty().And.NotBe(etagA);

            // 7) PUT with stale If-Match = A -> 412
            using var reqStale = new HttpRequestMessage(HttpMethod.Put, $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename")
            {
                Content = JsonContent.Create(new ColumnRenameDto { NewName = "Done" })
            };
            EndpointsTestHelper.SetIfMatchFromETag(reqStale, etagA!);

            var putStale = await client.SendAsync(reqStale);
            putStale.StatusCode.Should().Be((HttpStatusCode)412);

            // 8) PUT with wildcard If-Match: * -> success
            using var reqStar = new HttpRequestMessage(HttpMethod.Put, $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename")
            {
                Content = JsonContent.Create(new ColumnRenameDto { NewName = "Done" })
            };
            reqStar.Headers.TryAddWithoutValidation("If-Match", "*");

            var putStar = await client.SendAsync(reqStar);
            putStar.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        }
    }
}
