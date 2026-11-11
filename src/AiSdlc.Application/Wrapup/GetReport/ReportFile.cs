namespace AiSdlc.Application.Wrapup.GetReport;

/// <summary>A finished report ready to download.</summary>
public sealed record ReportFile(string FileName, string Content);
