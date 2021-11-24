using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DogController : ControllerBase
    {
        [HttpGet]
        public async System.Threading.Tasks.Task<string> GetAsync()
        {
            var filePtr = System.IO.File.ReadAllLines("dog_facts.txt");
            var fact = filePtr[new Random().Next(filePtr.Length - 1)];
            JsonSerializer serializer = new JsonSerializer();
            return fact;
        }
    }
}