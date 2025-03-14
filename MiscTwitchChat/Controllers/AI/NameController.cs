using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Summary description for Class1
/// </summary>
[Route("api/ai/[controller]")]
[ApiController]
public class NameController : ControllerBase
{
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public NameController()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY");
        _baseUrl = Environment.GetEnvironmentVariable("OPEN_AI_URL");
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidOperationException("API key or base URL is not set in environment variables.");
        }
    }

    [HttpGet("{game}/{item}")]
    public async Task<string> GetAsync(string game, string item)
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        var prompt = $"Generate a short, casual, and punny name for a {item} within the game {game}. Say just the name, nothing else.";
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant. You will be asked to complete things related to certain video games. Provide only short answers, designed to be inserted into a livestream chat, like Twitch chat. Stay casual, but not insulting. Use puns whenever you can." },
                new { role = "user", content = prompt }
            },
            max_tokens = 100
        };

        string jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(_baseUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
        }

        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(responseStream);

        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}