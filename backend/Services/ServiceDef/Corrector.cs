using System.Net.Http.Json;
using backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace backend.Services.ServiceDef;

public class Corrector : ICorrector
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public Corrector(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
        _http.Timeout = TimeSpan.FromMinutes(3);
    }

    public async Task<string> CorrectAsync(string text, string language)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var baseUrl = _config["MT5:BaseUrl"]
            ?? throw new InvalidOperationException("MT5:BaseUrl missing");

        var res = await _http.PostAsJsonAsync($"{baseUrl}/correct", new
        {
            text,
            lang = language
        });

        if (!res.IsSuccessStatusCode)
            return text;

        var json = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return json?.GetValueOrDefault("text") ?? text;
    }
}

