﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;

namespace MiscTwitchChat.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
            
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "This is a misc site for Demortes' random Twitch Chatbot API's.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
