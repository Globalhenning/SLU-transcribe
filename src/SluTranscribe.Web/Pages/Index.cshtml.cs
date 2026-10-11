using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SluTranscribe.Web.Services;

namespace SluTranscribe.Web.Pages;

public class IndexModel(
    BergetTranscriptionClient transcriber,
    IConfiguration configuration,
    ILogger<IndexModel> logger) : PageModel
{
    public int MaxMinutes { get; } = Math.Max(1, configuration.GetValue("Recording:MaxMinutes", 20));

    public void OnGet() { }

    public async Task<IActionResult> OnPostTranscribeAsync(IFormFile? audio, CancellationToken ct)
    {
        if (audio is null || audio.Length == 0)
            return Partial("_Transcript", TranscriptResult.Fail("Ingen ljudfil mottogs. Försök spela in igen."));

        try
        {
            await using var stream = audio.OpenReadStream();
            var text = await transcriber.TranscribeAsync(stream, audio.FileName, audio.ContentType, ct);

            return Partial("_Transcript", string.IsNullOrWhiteSpace(text)
                ? TranscriptResult.Fail("Inget tal kunde uppfattas i inspelningen.")
                : TranscriptResult.Ok(text));
        }
        catch (BergetException ex) when (ex.StatusCode is 401 or 403)
        {
            return Partial("_Transcript", TranscriptResult.Fail("Servern kunde inte autentisera mot Berget AI. Kontrollera API-nyckeln."));
        }
        catch (Exception ex) when (ex is BergetException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Transkribering misslyckades");
            return Partial("_Transcript", TranscriptResult.Fail("Transkriberingen misslyckades. Försök igen om en stund."));
        }
    }
}

public sealed record TranscriptResult(bool Success, string Text)
{
    public static TranscriptResult Ok(string text) => new(true, text);
    public static TranscriptResult Fail(string message) => new(false, message);
}
