using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using System.Collections.Generic;

namespace MiscTwitchChat.Controllers
{
    [Route("twitch")]
    public class TwitchAuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public TwitchAuthController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var clientId = _config["Twitch:ClientId"];
            var redirectUri = _config["Twitch:RedirectUri"];
            var scope = "user:read:email"; // Adjust scopes as needed

            var twitchAuthUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={HttpUtility.UrlEncode(clientId)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&scope={HttpUtility.UrlEncode(scope)}&state=xyz";
            return Redirect(twitchAuthUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return Content($"OAuth error: {error}");
            }

            var clientId = _config["Twitch:ClientId"];
            var clientSecret = _config["Twitch:ClientSecret"];
            var redirectUri = _config["Twitch:RedirectUri"];

            var httpClient = _httpClientFactory.CreateClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                })
            };

            var response = await httpClient.SendAsync(tokenRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Content($"Token exchange failed: {responseBody}");
            }

            // Optionally, parse and display the access token
            var jsonDoc = JsonDocument.Parse(responseBody);
            var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();

            return Content($"Twitch OAuth successful! Access Token: {accessToken}");
        }
    }
}
