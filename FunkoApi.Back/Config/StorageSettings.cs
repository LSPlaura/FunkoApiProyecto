namespace FunkoApi.Back.Config;

public class StorageSettings
{
    public const string SectionName = "Storage";
    public string UploadPath { get; set; } = string.Empty;
    public string MaxFileSize { get; set; } = string.Empty;
    public string AllowedExtensions { get; set; } = string.Empty;
    public string AllowedContentTypes { get; set; } = string.Empty;
    public string DefaultImage { get; set; } = string.Empty;

}