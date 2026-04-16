using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MiscTwitchChatCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TweetController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="TweetController"/> class with the given configuration.
        /// </summary>
        /// <param name="configuration">Configuration source used to retrieve Twitter API credentials and other settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <c>null</c>.</exception>
        public TweetController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets the latest tweet from a specified Twitter user.
        /// </summary>
        /// <param name="twitterUsername">The Twitter username to get the latest tweet from.</param>
        /// <summary>
        /// Retrieves the most recent tweet for the specified Twitter username and returns its URL.
        /// </summary>
        /// <param name="twitterUsername">The Twitter handle (screen name) to fetch the latest tweet for; do not include the '@' prefix.</param>
        /// <returns>The URL of the user's latest tweet, for example "https://twitter.com/{user}/status/{id}".</returns>
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
