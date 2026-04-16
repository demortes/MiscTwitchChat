using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DadController : ControllerBase
    {
        /// <summary>
        /// Gets a random dad joke.
        /// </summary>
        /// <summary>
        /// Retrieves a random dad joke from icanhazdadjoke.com as plain text.
        /// </summary>
        /// <returns>The dad joke text.</returns>
        [HttpGet]
        public async Task<string> GetDadJoke()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var resp = await client.GetAsync("https://icanhazdadjoke.com");
            var joke = await resp.Content.ReadAsStringAsync();
            return joke;
        }
    }
}