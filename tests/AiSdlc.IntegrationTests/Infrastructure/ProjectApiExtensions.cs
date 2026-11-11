using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Projects;
using AiSdlc.Application.Team.GetTeam;

namespace AiSdlc.IntegrationTests.Infrastructure;

public static class ProjectApiExtensions
{
    public static async Task<ProjectSummaryDto> CreateProjectAsync(this HttpClient manager)
    {
        var response = await manager.PostAsJsonAsync(
            "/api/v1/projects", new { name = $"Project {Guid.NewGuid():N}"[..20], description = "Test project" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectSummaryDto>(ApiJson.Options))!;
    }

    public static async Task<HttpResponseMessage> UploadAsync(
        this HttpClient client, Guid projectId, string kind, string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        form.Add(file, "file", fileName);
        return await client.PostAsync($"/api/v1/projects/{projectId}/files/{kind}", form);
    }

    public static async Task<ProjectFileDto> UploadOkAsync(
        this HttpClient client, Guid projectId, string kind, string fileName, byte[] content)
    {
        var response = await client.UploadAsync(projectId, kind, fileName, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectFileDto>(ApiJson.Options))!;
    }

    public static async Task<ProjectDetailDto> GetProjectAsync(this HttpClient client, Guid projectId) =>
        (await client.GetFromJsonAsync<ProjectDetailDto>($"/api/v1/projects/{projectId}", ApiJson.Options))!;

    public static async Task<TeamOverviewDto> GetTeamAsync(this HttpClient client, Guid projectId) =>
        (await client.GetFromJsonAsync<TeamOverviewDto>($"/api/v1/projects/{projectId}/team", ApiJson.Options))!;

    public static async Task AddMemberAsync(this HttpClient manager, Guid projectId, Guid userId)
    {
        var response = await manager.PostAsJsonAsync($"/api/v1/projects/{projectId}/members", new { userId });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Looks up a seeded demo user's id through the Project Manager's directory search.</summary>
    public static async Task<UserDto> FindUserAsync(this HttpClient manager, string email)
    {
        var users = await manager.GetFromJsonAsync<List<UserDto>>(
            $"/api/v1/directory/users?query={Uri.EscapeDataString(email)}", ApiJson.Options);
        return users!.Single(u => u.Email == email);
    }
}
