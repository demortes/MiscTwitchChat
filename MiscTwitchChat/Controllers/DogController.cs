using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DogController : ControllerBase
    {
        private readonly string endpoint = "https://dog-api.kinduff.com/api/facts?number=1";

        [HttpGet]
        public async System.Threading.Tasks.Task<string> GetAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var response = await client.GetAsync(endpoint);
            var contentReader = new StreamReader(await response.Content.ReadAsStreamAsync());
            JsonSerializer serializer = new JsonSerializer();
            var dogResponse = (DogResponse)serializer.Deserialize(contentReader, typeof(DogResponse));
            return dogResponse.facts[0];
        }
    }
}