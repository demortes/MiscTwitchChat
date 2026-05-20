using System;
using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace MiscTwitchChat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly string _dictionaryApiKey;

        public DictionaryController(IConfiguration config)
        {
            _dictionaryApiKey = config["Dictionary:ApiKey"] ?? config["DictionaryApiKey"];
        }

        [HttpGet("{word}")]
        public async Task<IActionResult> Get(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return BadRequest("word is required");
            }

            using var client = new HttpClient();
            var encoded = Uri.EscapeDataString(word);
            var dictionaryLookupUrl = $"https://www.dictionaryapi.com/api/v3/references/collegiate/json/{encoded}?key={Uri.EscapeDataString(_dictionaryApiKey ?? string.Empty)}";

            var resp = await client.GetAsync(dictionaryLookupUrl);
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync();
            var entries = await JsonSerializer.DeserializeAsync<WordEntry[]>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (entries == null || entries.Length == 0)
            {
                return NotFound();
            }

            // Prefer shortdef if present, otherwise try to extract something from def
            var first = entries.First();
            var meaning = first.shortdef?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(meaning) && first.def?.Length > 0)
            {
                // def.sseq can be complex; fall back to fl + vd if present
                meaning = first.fl ?? first.def.First().vd;
            }

            if (string.IsNullOrWhiteSpace(meaning))
            {
                return NotFound();
            }

            return Ok(meaning);
        }
    }
}
