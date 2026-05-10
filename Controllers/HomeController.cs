using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bachelor_s_Point.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRoomService _roomService;

        public HomeController(ILogger<HomeController> logger, IRoomService roomService)
        {
            _logger = logger;
            _roomService = roomService;
        }

        public async Task<IActionResult> Index()
        {
            // Anonymous users see the login chooser first
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("RoleSelect", "Auth");
            }

            var rooms = await _roomService.GetAllAvailableRoomsAsync();
            return View(rooms);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
