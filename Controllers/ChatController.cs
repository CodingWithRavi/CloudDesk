using CloudDesk.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudDesk.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserName == User.Identity!.Name);

            if (currentUser == null)
                return Unauthorized();

            ViewBag.IsViewer = User.IsInRole("Viewer");

            ViewBag.DisplayName =
                currentUser.DisplayName ?? currentUser.UserName;

            // Current user ke alawa ChatUser
            var partner = await _context.Users
                .Where(u => u.Id != currentUser.Id)
                .FirstOrDefaultAsync();

            ViewBag.ChatPartnerName =
                partner?.DisplayName ?? "User";

            ViewBag.ChatPartnerId =
                partner?.Id ?? "";

            return View();
        }
    }
}