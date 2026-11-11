using System.Net.Http.Json;
using AiSdlc.Application.Analysis;

namespace AiSdlc.IntegrationTests.Infrastructure;

public static class AnalysisApiExtensions
{
    private const string SampleRequirements =
        "Customers can pay with a saved card.\nThe system must encrypt card data and keep an audit trail.\nRefunds are possible.";

    /// <summary>Creates a project with requirements and a team sheet uploaded, so analysis can start.</summary>
    public static async Task<Guid> CreateReadyProjectAsync(this HttpClient manager)
    {
        var project = await manager.CreateProjectAsync();
        await manager.UploadOkAsync(project.Id, "requirements", "requirements.txt", TestFiles.Text(SampleRequirements));
        await manager.UploadOkAsync(project.Id, "team-template", "team.xlsx", TestFiles.Xlsx());
        return project.Id;
    }

    public static async Task<AnalysisVersionDto> GenerateAsync(this HttpClient manager, Guid projectId, string? feedback = null)
    {
        var response = await manager.PostAsJsonAsync($"/api/v1/projects/{projectId}/analysis/generate", new { feedback });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AnalysisVersionDto>(ApiJson.Options))!;
    }

    public static async Task<AnalysisOverviewDto> GetAnalysisAsync(this HttpClient client, Guid projectId) =>
        (await client.GetFromJsonAsync<AnalysisOverviewDto>($"/api/v1/projects/{projectId}/analysis", ApiJson.Options))!;
}
