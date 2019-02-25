using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MiscTwitchChat.Controllers
{
    /// <summary>
    /// Used to retrieve the link to the latest tweet of a user.
    /// </summary>
    public class TweetController : ApiController
    {
        private readonly string TwitterPrefix = "https://twitter.com/demortes/status/";
        /// <summary>
        /// Get the latest tweet of the twitter username passed in, and display the link to it.
        /// </summary>
        /// <param name="twitterUsername">Twitter handle to search for.</param>
        /// <returns>Link to the latest tweet of that user.</returns>
        public async Task<HttpResponseMessage> Get(string twitterUsername)
        {
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={twitterUsername}&count=1");
            string TwitterApiKey = ConfigurationManager.AppSettings["TwitterApiKey"].ToString();
            string TwitterApiSecretKey = ConfigurationManager.AppSettings["TwitterApiSecret"].ToString();
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(TwitterApiKey + ":" + TwitterApiSecretKey));

            var client = new HttpClient();
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

            HttpResponseMessage rval = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"https://twitter.com/{twitterUsername}/status/" + tweet[0].id)
            };

            return rval;
        }
    }
}
