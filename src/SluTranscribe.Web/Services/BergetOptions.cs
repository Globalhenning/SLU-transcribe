using System.ComponentModel.DataAnnotations;

namespace SluTranscribe.Web.Services;

public sealed class BergetOptions
{
    public const string SectionName = "Berget";

    [Required]
    public string BaseUrl { get; set; } = "https://api.berget.ai/v1/";

    [Required(ErrorMessage = "Berget:ApiKey saknas. Sätt den med 'dotnet user-secrets set Berget:ApiKey <nyckel>' eller miljövariabeln Berget__ApiKey.")]
    public string ApiKey { get; set; } = "";

    [Required]
    public string Model { get; set; } = "KBLab/kb-whisper-large";

    public string? Language { get; set; } = "sv";
}
