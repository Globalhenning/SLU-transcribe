using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using SluTranscribe.Web.Services;

var builder = WebApplication.CreateBuilder(args);

const long maxUploadBytes = 25 * 1024 * 1024;

builder.Services.AddRazorPages();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUploadBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxUploadBytes);

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

app.Run();
