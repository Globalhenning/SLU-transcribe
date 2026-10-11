using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SluTranscribe.Web.Services;

public sealed class BergetTranscriptionClient(
    HttpClient http,
    IOptions<BergetOptions> options,
    ILogger<BergetTranscriptionClient> logger)
{
    private readonly BergetOptions _options = options.Value;

    public async Task<string> TranscribeAsync(
        Stream audio, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();

        var file = new StreamContent(audio);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(_options.Model), "model");
        form.Add(new StringContent("json"), "response_format");
        if (!string.IsNullOrWhiteSpace(_options.Language))
            form.Add(new StringContent(_options.Language), "language");

        using var response = await http.PostAsync("audio/transcriptions", form, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Berget AI svarade {Status}: {Body}", (int)response.StatusCode, body);
            throw new BergetException((int)response.StatusCode, body);
        }

        var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>(ct);
        return result?.Text?.Trim() ?? "";
    }

    private sealed record TranscriptionResponse([property: JsonPropertyName("text")] string? Text);
}

public sealed class BergetException(int statusCode, string body)
    : Exception($"Berget AI returned {statusCode}: {body}")
{
    public int StatusCode { get; } = statusCode;
}
