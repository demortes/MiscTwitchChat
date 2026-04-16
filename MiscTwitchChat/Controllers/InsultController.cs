using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsultController : ControllerBase
    {
        /// <summary>
        /// Gets a random insult.
        /// </summary>
        /// <param name="channel">The channel the insult is being used in.</param>
        /// <param name="user">The user using the insult.</param>
        /// <param name="target">The target of the insult. If not specified, the insult will be generic.</param>
        /// <summary>
        /// Fetches a random insult text from the external insult.mattbas.org API.
        /// </summary>
        /// <param name="channel">Channel name (accepted but not used by this method).</param>
        /// <param name="user">User name (accepted but not used by this method).</param>
        /// <param name="target">Optional target name; when provided it is sent as the `who` query parameter to the API to customize the insult.</param>
        /// <returns>The insult text returned by the API.</returns>
        [HttpGet]
        public async Task<string> InsultAsync(string channel, string user, string target = null)
        {
            var url = "https://insult.mattbas.org/api/insult.txt";
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(target))
            {
                url += $"?who={target}";
            }
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}