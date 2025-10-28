using Api.Auth.DTOs;
using Application.Columns.DTOs;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using Application.TaskItems.DTOs;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Columns;

namespace TestHelpers.Api.Projects
{
    public static class BoardSetupHelper
    {
        public static async Task<(ProjectReadDto project, LaneReadDto lane)> CreateProjectAndLane(
            HttpClient client,
            string projectName = "Project",
            string laneName = "Lane",
            int laneOrder = 0)
        {
            var project = await ProjectTestHelper.CreateProject(client, projectName);
            var lane = await ProjectTestHelper.CreateLane(client, project.Id, laneName, laneOrder);

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
            var column = await ColumnTestHelper.PostColumnDtoAsync(client, project.Id, lane.Id, columnName, columnOrder);

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
            var user = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var (project, lane, column) = await CreateProjectLaneAndColumn(
                client,
                projectName,
                laneName,
                laneOrder,
                columnName,
                columnOrder);
            var task = await ProjectTestHelper.CreateTask(
                client,
                project.Id,
                lane.Id,
                column.Id,
                taskTitle,
                taskDescription,
                taskDueDate);

            return (project, lane, column, task, user);
        }
    }
}
