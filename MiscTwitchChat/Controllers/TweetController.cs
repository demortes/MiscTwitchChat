using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System;

namespace MiscTwitchChatCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TweetController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TweetController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpGet("{twitterUsername}")]
        public async Task<string> GetAsync(string twitterUsername)
        {
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={twitterUsername}&count=1");
            string TwitterApiKey = _configuration["TwitterApiKey"].Trim();
            string TwitterApiSecretKey = _configuration["TwitterApiSecret"].Trim();

            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(TwitterApiKey + ":" + TwitterApiSecretKey));

            using var client = new HttpClient();
            var formDictionary = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials"}
            };
            var content = new FormUrlEncodedContent(formDictionary);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
            var response = await client.PostAsync("https://api.twitter.com/oauth2/token", content);
            var stringResponse = await response.Content.ReadAsStringAsync();
            dynamic tokenResponse = JsonConvert.DeserializeObject(stringResponse);
            string token = tokenResponse.access_token;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var tweetResponse = await client.GetStringAsync($"https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={twitterUsername}&count=1&exclude_replies=true&trim_user=true&include_rts=false");
            dynamic tweet = JsonConvert.DeserializeObject(tweetResponse);

            return $"https://twitter.com/{twitterUsername}/status/" + tweet[0].id;
        }
    }
}
