using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using System.Diagnostics;

namespace MiscTwitchChat.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {

        }
        /// <summary>
        /// Displays the home page.
        /// </summary>
        /// <returns>The home page view.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the about page.
        /// </summary>
        /// <returns>The about page view.</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Displays the error page.
        /// </summary>
        /// <returns>The error page view.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
