using Api.Auth.DTOs;
using Application.Columns.DTOs;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using Application.TaskItems.DTOs;
using Application.TaskNotes.DTOs;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Columns;
using TestHelpers.Api.Lanes;
using TestHelpers.Api.TaskItems;
using TestHelpers.Api.TaskNotes;

namespace TestHelpers.Api.Projects
{
    public static class BoardSetupHelper
    {
        public static async Task<(ProjectReadDto project, LaneReadDto lane)> CreateProjectAndLane(
            HttpClient client,
            ProjectCreateDto? projectDto = null,
            LaneCreateDto? laneDto = null)
        {
            var project = await ProjectTestHelper.PostProjectDtoAsync(client, projectDto);
            var lane = await LaneTestHelper.PostLaneDtoAsync(client, project.Id, laneDto);

            return (project, lane);
        }

        public static async Task<(ProjectReadDto project, LaneReadDto lane, ColumnReadDto column)> CreateProjectLaneAndColumn(
            HttpClient client,
            ProjectCreateDto? projectDto = null,
            LaneCreateDto? laneDto = null,
            ColumnCreateDto? columnDto = null)
        {
            var (project, lane) = await CreateProjectAndLane(client, projectDto, laneDto);
            var column = await ColumnTestHelper.PostColumnDtoAsync(client, project.Id, lane.Id, columnDto);

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
            ProjectCreateDto? projectDto = null,
            LaneCreateDto? laneDto = null,
            ColumnCreateDto? columnDto = null,
            TaskItemCreateDto? taskDto = null)
        {
            var user = await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var (project, lane, column) = await CreateProjectLaneAndColumn(
                client,
                projectDto,
                laneDto,
                columnDto);
            var task = await TaskItemTestHelper.PostTaskItemDtoAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                taskDto);

            return (project, lane, column, task, user);
        }

        public static async Task<(
            ProjectReadDto project,
            LaneReadDto lane,
            ColumnReadDto column,
            TaskItemReadDto task,
            TaskNoteReadDto note,
            AuthTokenReadDto user)>
            SetupBoardWithNote(HttpClient client)
        {
            var (project, lane, column, task, user) = await SetupBoard(client);
            var note = await TaskNoteTestHelper.PostNoteDtoAsync(
                client, project.Id, lane.Id, column.Id, task.Id);
            return (project, lane, column, task, note, user);
        }
    }
}
