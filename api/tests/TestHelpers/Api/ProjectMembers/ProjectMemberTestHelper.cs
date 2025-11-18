using Application.ProjectMembers.DTOs;
using Domain.Enums;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.ProjectMembers
{
    public static class ProjectMemberTestHelper
    {

        // ----- POST -----

        public static async Task<HttpResponseMessage> PostProjectMemberResponseAsync(
            HttpClient client,
            Guid projectId,
            ProjectMemberCreateDto? dto = null)
        {
            Guid userId;
            ProjectRole role;

            if (dto is null)
            {
                userId = ProjectMemberDefaults.DefaultProjectMemberId;
                role = ProjectMemberDefaults.DefaultProjectMemberRole;
            }
            else
            {
                userId = dto.UserId;
                role = dto.Role;
            }

            var createDto = new ProjectMemberCreateDto() { UserId = userId, Role = role };
            var response = await client.PostWithoutIfMatchAsync(
                $"/projects/{projectId}/members",
                createDto);

            return response;
        }

        public static async Task<ProjectMemberReadDto> PostProjectMemberDtoAsync(
            HttpClient client,
            Guid projectId,
            ProjectMemberCreateDto? dto = null)
        {
            var response = await PostProjectMemberResponseAsync(client, projectId, dto);
            var member = await response.ReadContentAsDtoAsync<ProjectMemberReadDto>();

            return member;
        }

        // ----- GET MEMBER -----

        public static async Task<HttpResponseMessage> GetProjectMemberResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid userId)
        {
            var response = await client.GetAsync($"/projects/{projectId}/members/{userId}");
            return response;
        }

        public static async Task<ProjectMemberReadDto> GetProjectMemberDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid userId)
        {
            var response = await GetProjectMemberResponseAsync(client, projectId, userId);
            var member = await response.ReadContentAsDtoAsync<ProjectMemberReadDto>();

            return member;
        }

        // ----- GET ME/COUNT -----

        public static async Task<HttpResponseMessage> GetProjectMemberMeCountResponseAsync(HttpClient client)
        {
            var response = await client.GetAsync("/members/me/count");
            return response;
        }

        // ----- PATCH CHANGE ROLE -----

        public static async Task<HttpResponseMessage> ChangeProjectMemberRoleResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion,
            ProjectMemberChangeRoleDto? dto = null)
        {
            var newRole = dto is null
                ? ProjectMemberDefaults.DefaultProjectMemberChangeRole
                : dto.NewRole;
            var changeRoleDto = new ProjectMemberChangeRoleDto() { NewRole = newRole };

            var changeRoleResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/members/{userId}/role",
                changeRoleDto);

            return changeRoleResponse;
        }

        public static async Task<ProjectMemberRoleReadDto> ChangeProjectMemberRoleDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion,
            ProjectMemberChangeRoleDto? dto = null)
        {
            var response = await ChangeProjectMemberRoleResponseAsync(client, projectId, userId, rowVersion, dto);
            var role = await response.ReadContentAsDtoAsync<ProjectMemberRoleReadDto>();

            return role;
        }

        // ----- PATCH REMOVE -----

        public static async Task<HttpResponseMessage> RemoveProjectMemberResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion)
        {
            var removeResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/members/{userId}/remove", new object());

            return removeResponse;
        }

        public static async Task<ProjectMemberReadDto> RemoveProjectMemberDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion)
        {
            var response = await RemoveProjectMemberResponseAsync(client, projectId, userId, rowVersion);
            var member = await response.ReadContentAsDtoAsync<ProjectMemberReadDto>();

            return member;
        }


        // ----- PATCH RESTORE -----

        public static async Task<HttpResponseMessage> RestoreProjectMemberResponseAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion)
        {
            var restoreResponse = await client.PatchWithIfMatchAsync(
                rowVersion,
                $"/projects/{projectId}/members/{userId}/restore", new object());

            return restoreResponse;
        }

        public static async Task<ProjectMemberReadDto> RestoreProjectMemberDtoAsync(
            HttpClient client,
            Guid projectId,
            Guid userId,
            string rowVersion)
        {
            var response = await RestoreProjectMemberResponseAsync(client, projectId, userId, rowVersion);
            var member = await response.ReadContentAsDtoAsync<ProjectMemberReadDto>();

            return member;
        }
    }
}
