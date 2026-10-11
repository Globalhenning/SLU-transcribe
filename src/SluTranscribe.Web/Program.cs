using System.Net.Http.Headers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using SluTranscribe.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = UploadLimits.MaxUploadBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = UploadLimits.MaxUploadBytes);

builder.Services.AddHealthChecks();

// I container: spara antiforgery-nycklar på en volym så att de överlever omstarter.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("SluTranscribe");
}

builder.Services.AddOptions<BergetOptions>()
    .Bind(builder.Configuration.GetSection(BergetOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<BergetTranscriptionClient>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<BergetOptions>>().Value;
    http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    http.Timeout = TimeSpan.FromMinutes(2);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapHealthChecks("/healthz");

app.Run();
