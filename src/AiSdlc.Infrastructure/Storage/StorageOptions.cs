using System.ComponentModel.DataAnnotations;

namespace AiSdlc.Infrastructure.Storage;

/// <summary>Bound to the <c>Storage</c> section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public const string LocalProvider = "Local";

    /// <summary>Which <c>IFileStorage</c> adapter to use. Only <c>Local</c> exists today; cloud adapters plug in here.</summary>
    [AllowedValues(LocalProvider)]
    public string Provider { get; set; } = LocalProvider;

    /// <summary>Folder for the local adapter. Keep it outside any web root; files are only served through the API.</summary>
    [Required]
    public string RootPath { get; set; } = "App_Data/files";
}
