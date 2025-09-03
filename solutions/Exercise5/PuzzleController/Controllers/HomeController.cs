using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using PuzzleController.Models;
using System.Diagnostics;

namespace PuzzleController.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage()
        {
            await using var client = new ServiceBusClient("connection string");
            await using var sender = client.CreateSender("williampuzzle");

            var message = new ServiceBusMessage("Hello from PuzzleController!");
            await sender.SendMessageAsync(message);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Index()
        {
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
