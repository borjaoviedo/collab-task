using Api.Auth.DTOs;
using Application.Columns.DTOs;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using Application.TaskItems.DTOs;
using Application.Users.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestHelpers.Api
{
    public static class EndpointsTestHelper
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task<AuthTokenReadDto> RegisterAndLoginAsync(HttpClient client, string? email = null, string name = "User Name", string password = "Str0ngP@ss!")
        {
            // ensure anonymous for register/login
            client.DefaultRequestHeaders.Authorization = null;

            email ??= $"{Guid.NewGuid():N}@demo.com";

            var register = await client.PostAsJsonAsync("/auth/register", new UserRegisterDto { Email = email, Name = name, Password = password });
            register.EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            login.EnsureSuccessStatusCode();

            var token = await login.Content.ReadFromJsonAsync<AuthTokenReadDto>(Json);
            return token!;
        }

        public static async Task<UserReadDto> GetUser(HttpClient client, Guid userId, string adminBearer)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var resp = await client.GetAsync($"/users/{userId}");
            resp.EnsureSuccessStatusCode();

            var user = await resp.Content.ReadFromJsonAsync<UserReadDto>(Json);
            return user!;
        }

        public static async Task<ProjectReadDto> CreateProject(HttpClient client, string name = "Project")
        {
            var createDto = new ProjectCreateDto() { Name = name };
            var response = await PostWithoutIfMatchAsync(client, "/projects", createDto);
            var project = await response.Content.ReadFromJsonAsync<ProjectReadDto>(Json);

            return project!;
        }

        public static async Task<LaneReadDto> CreateLane(HttpClient client, Guid projectId, string name = "Lane", int order = 0)
        {
            var createDto = new LaneCreateDto() { Name = name, Order = order };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes", createDto);
            var lane = await response.Content.ReadFromJsonAsync<LaneReadDto>(Json);

            return lane!;
        }

        public static async Task<ColumnReadDto> CreateColumn(HttpClient client, Guid projectId, Guid laneId, string name = "Column", int order = 0)
        {
            var createDto = new ColumnCreateDto() { Name = name, Order = order };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes/{laneId}/columns", createDto);
            var column = await response.Content.ReadFromJsonAsync<ColumnReadDto>(Json);

            return column!;
        }

        public static async Task<TaskItemReadDto> CreateTask(HttpClient client, Guid projectId, Guid laneId, Guid columnId,
            string title = "Task Title", string description = "Task Description", DateTimeOffset? dueDate = null, decimal sortKey = 0m)
        {
            var createDto = new TaskItemCreateDto { Title = title, Description = description, DueDate = dueDate, SortKey = sortKey };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks", createDto);
            var task = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(Json);

            return task!;
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane)> CreateProjectAndLane(
            HttpClient client, string projectName = "Project", string laneName = "Lane", int laneOrder = 0)
        {
            var project = await CreateProject(client, projectName);
            var lane = await CreateLane(client, project.Id, laneName, laneOrder);

            return (project, lane);
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane, ColumnReadDto column)> CreateProjectLaneAndColumn(
            HttpClient client, string projectName = "Project", string laneName = "Lane", int laneOrder = 0, string columnName = "Column", int columnOrder = 0)
        {
            var (project, lane) = await CreateProjectAndLane(client, projectName, laneName, laneOrder);
            var column = await CreateColumn(client, project.Id, lane.Id, columnName, columnOrder);

            return (project, lane, column);
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane, ColumnReadDto column, TaskItemReadDto task, AuthTokenReadDto user)> SetupBoard(
            HttpClient client, string projectName = "Project", string laneName = "Lane", int laneOrder = 0, string columnName = "Column", int columnOrder = 0,
            string taskTitle = "Task", string taskDescription = "Description", DateTimeOffset? taskDueDate = null)
        {
            var user = await RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

            var (project, lane, column) = await CreateProjectLaneAndColumn(client, projectName, laneName, laneOrder, columnName, columnOrder);
            var task = await CreateTask(client, project.Id, lane.Id, column.Id, taskTitle, taskDescription, taskDueDate);

            return (project, lane, column, task, user);
        }

        public static async Task<HttpResponseMessage> PostWithoutIfMatchAsync<T>(HttpClient client, string url, T payload)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return await client.PostAsJsonAsync(url, payload);
        }

        public static void SetIfMatchFromRowVersion(HttpClient client, byte[] rowVersion)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                throw new ArgumentException("RowVersion is null or empty.", nameof(rowVersion));

            client.DefaultRequestHeaders.IfMatch.Clear();
            var etag = $"W/\"{Convert.ToBase64String(rowVersion)}\"";
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        }

        public static void SetIfMatchFromETag(HttpRequestMessage req, string etag)
        {
            req.Headers.IfMatch.Clear();

            if (EntityTagHeaderValue.TryParse(etag, out var parsed))
            {
                req.Headers.IfMatch.Add(parsed);
                return;
            }

            req.Headers.TryAddWithoutValidation("If-Match", etag);
        }
    }
}
