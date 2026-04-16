using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatController : ControllerBase
    {
        private string endpoint = "https://catfact.ninja/fact";
        /// <summary>
        /// Gets a random cat fact.
        /// </summary>
        /// <summary>
        /// Fetches a random cat fact from the configured API endpoint.
        /// </summary>
        /// <returns>A string containing the cat fact returned by the API.</returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<string> GetAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var response = await client.GetAsync(endpoint);
            var contentReader = new StreamReader(await response.Content.ReadAsStreamAsync());
            JsonSerializer serializer = new JsonSerializer();
            var catResponse = (CatResponse)serializer.Deserialize(contentReader, typeof(CatResponse));
            return catResponse.fact;
        }
    }
}