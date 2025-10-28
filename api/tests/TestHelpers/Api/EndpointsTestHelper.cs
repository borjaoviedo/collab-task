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
        public readonly static string DefaultEmail = $"{Guid.NewGuid():N}@demo.com";
        public readonly static string DefaultUserName = "User name";
        public readonly static string DefaultPassword = "Str0ngP@ss!";

        public readonly static UserRegisterDto DefaultUserRegisterDto = new()
        {
            Email = DefaultEmail,
            Name = DefaultUserName,
            Password = DefaultPassword
        };

        public readonly static UserLoginDto DefaultUserLoginDto = new()
        {
            Email = DefaultEmail,
            Password = DefaultPassword
        };

        public readonly static ColumnCreateDto DefaultColumnCreateDto = new()
        {
            Name = "Todo",
            Order = 0
        };

        public readonly static ColumnRenameDto DefaultColumnRenameDto = new() { NewName = "In Progress" };

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task<T> ReadContentAsDtoAsync<T>(this HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(Json);
            return result ?? throw new InvalidOperationException($"Response content could not be deserialized to {typeof(T).Name}.");
        }

        public static void SetAuthorization(this HttpClient client, string? parameter, string scheme = "Bearer")
            => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);

        public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, UserRegisterDto? registerDto = null)
        {
            // ensure anonymous for register
            client.SetAuthorization(null);

            registerDto ??= DefaultUserRegisterDto;

            var registerResponse = await client.PostAsJsonAsync("/auth/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            return registerResponse;
        }

        public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, UserLoginDto? loginDto = null)
        {
            // ensure anonymous for login
            client.SetAuthorization(null);

            loginDto ??= DefaultUserLoginDto;
            var loginResponse = await client.PostAsJsonAsync("/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();

            return loginResponse;
        }

        public static async Task<AuthTokenReadDto> RegisterAndLoginAsync(
            HttpClient client,
            string? email = null,
            string name = "User Name",
            string password = "Str0ngP@ss!")
        {
            email ??= $"{Guid.NewGuid():N}@demo.com";

            var userRegisterDto = new UserRegisterDto { Email = email, Name = name, Password = password };
            await RegisterAsync(client, userRegisterDto);

            var userLoginDto = new UserLoginDto { Email = userRegisterDto.Email, Password = password };
            var login = await LoginAsync(client, userLoginDto);

            var token = await login.Content.ReadFromJsonAsync<AuthTokenReadDto>(Json);
            return token!;
        }

        public static async Task<AuthTokenReadDto> RegisterLoginAndSetAuthorizationAsync(
            HttpClient client,
            string? email = null,
            string name = "User Name",
            string password = "Str0ngP@ss!")
        {
            var token = await RegisterAndLoginAsync(client, email, name, password);
            client.SetAuthorization(token.AccessToken);

            return token!;
        }

        public static async Task<UserReadDto> GetUser(HttpClient client, Guid userId, string adminBearer)
        {
            client.SetAuthorization(adminBearer);

            var response = await client.GetAsync($"/users/{userId}");
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserReadDto>(Json);
            return user!;
        }

        public static async Task<ProjectReadDto> CreateProject(HttpClient client, string name = "Project")
        {
            var createDto = new ProjectCreateDto() { Name = name };
            var response = await PostWithoutIfMatchAsync(client, "/projects", createDto);
            var project = await response.Content.ReadFromJsonAsync<ProjectReadDto>(Json);

            return project!;
        }

        public static async Task<LaneReadDto> CreateLane(
            HttpClient client,
            Guid projectId,
            string name = "Lane",
            int order = 0)
        {
            var createDto = new LaneCreateDto() { Name = name, Order = order };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes", createDto);
            var lane = await response.Content.ReadFromJsonAsync<LaneReadDto>(Json);

            return lane!;
        }

        public static async Task<ColumnReadDto> CreateColumn(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            string name = "Column",
            int order = 0)
        {
            var createDto = new ColumnCreateDto() { Name = name, Order = order };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes/{laneId}/columns", createDto);
            var column = await response.Content.ReadFromJsonAsync<ColumnReadDto>(Json);

            return column!;
        }

        public static async Task<TaskItemReadDto> CreateTask(
            HttpClient client,
            Guid projectId,
            Guid laneId,
            Guid columnId,
            string title = "Task Title",
            string description = "Task Description",
            DateTimeOffset? dueDate = null,
            decimal sortKey = 0m)
        {
            var createDto = new TaskItemCreateDto { Title = title, Description = description, DueDate = dueDate, SortKey = sortKey };
            var response = await PostWithoutIfMatchAsync(client, $"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks", createDto);
            var task = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(Json);

            return task!;
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane)> CreateProjectAndLane(
            HttpClient client,
            string projectName = "Project",
            string laneName = "Lane",
            int laneOrder = 0)
        {
            var project = await CreateProject(client, projectName);
            var lane = await CreateLane(client, project.Id, laneName, laneOrder);

            return (project, lane);
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane, ColumnReadDto column)> CreateProjectLaneAndColumn(
            HttpClient client,
            string projectName = "Project",
            string laneName = "Lane",
            int laneOrder = 0,
            string columnName = "Column",
            int columnOrder = 0)
        {
            var (project, lane) = await CreateProjectAndLane(client, projectName, laneName, laneOrder);
            var column = await CreateColumn(client, project.Id, lane.Id, columnName, columnOrder);

            return (project, lane, column);
        }

        public static async Task<(
            ProjectReadDto project,
            LaneReadDto lane,
            ColumnReadDto column,
            TaskItemReadDto task,
            AuthTokenReadDto user)>
            SetupBoard(
            HttpClient client,
            string projectName = "Project",
            string laneName = "Lane",
            int laneOrder = 0,
            string columnName = "Column",
            int columnOrder = 0,
            string taskTitle = "Task",
            string taskDescription = "Description",
            DateTimeOffset? taskDueDate = null)
        {
            var user = await RegisterLoginAndSetAuthorizationAsync(client);
            var (project, lane, column) = await CreateProjectLaneAndColumn(
                client,
                projectName,
                laneName,
                laneOrder,
                columnName,
                columnOrder);
            var task = await CreateTask(
                client,
                project.Id,
                lane.Id,
                column.Id,
                taskTitle,
                taskDescription,
                taskDueDate);

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
