using CloudDesk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudDesk.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }


        // =========================
        // LOGIN GET
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Chat");

            return View();
        }


        // =========================
        // LOGIN POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);
            }

            if (user == null)
            {
                user = _userManager.Users
                    .FirstOrDefault(u =>
                        u.DisplayName == model.Username);
            }

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    "Chat");
            }

            ModelState.AddModelError(
                "",
                "Invalid username or password.");

            return View(model);
        }


        // =========================
        // FORGOT PASSWORD GET
        // =========================

        [HttpGet]
        public IActionResult ForgotPassword(
            string username)
        {
            // Har baar naya 6 digit code
            var random =
                new Random();

            var code =
                random.Next(
                    100000,
                    1000000
                ).ToString();

            // Code server side store
            TempData["ForgotPasswordCode"] =
                code;

            var model =
                new ForgotPasswordViewModel
                {
                    Username = username
                };

            // Code screen par dikhane ke liye
            ViewBag.Code = code;

            return View(model);
        }


        // =========================
        // FORGOT PASSWORD POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
    ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Code =
                    TempData["ForgotPasswordCode"]?.ToString();

                return View(model);
            }


            // =========================
            // CHECK OTP / CODE
            // =========================

            var savedCode =
                TempData["ForgotPasswordCode"]?.ToString();


            if (
                string.IsNullOrWhiteSpace(savedCode) ||
                model.Code.Trim() != savedCode
            )
            {
                ModelState.AddModelError(
                    "Code",
                    "Invalid code."
                );

                ViewBag.Code = savedCode;

                return View(model);
            }


            // =========================
            // FIND USER
            // =========================

            var username =
                model.Username.Trim();


            var user =
                await _userManager.FindByNameAsync(
                    username
                );


            // Try Email
            if (user == null)
            {
                user =
                    await _userManager.FindByEmailAsync(
                        username
                    );
            }


            // Try DisplayName
            if (user == null)
            {
                user =
                    _userManager.Users
                        .FirstOrDefault(
                            u => u.DisplayName != null &&
                                 u.DisplayName == username
                        );
            }


            // User still not found
            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "User not found."
                );

                ViewBag.Code = savedCode;

                return View(model);
            }


            // =========================
            // REMOVE OLD PASSWORD
            // =========================

            var removeResult =
                await _userManager.RemovePasswordAsync(
                    user
                );


            if (!removeResult.Succeeded)
            {
                foreach (
                    var error
                    in removeResult.Errors
                )
                {
                    ModelState.AddModelError(
                        "",
                        error.Description
                    );
                }

                ViewBag.Code = savedCode;

                return View(model);
            }


            // =========================
            // ADD NEW PASSWORD
            // =========================

            var addResult =
                await _userManager.AddPasswordAsync(
                    user,
                    model.NewPassword
                );


            if (!addResult.Succeeded)
            {
                foreach (
                    var error
                    in addResult.Errors
                )
                {
                    ModelState.AddModelError(
                        "",
                        error.Description
                    );
                }

                ViewBag.Code = savedCode;

                return View(model);
            }


            // =========================
            // SUCCESS
            // =========================

            TempData["ForgotPasswordSuccess"] =
                "Password changed successfully.";


            return RedirectToAction("Login");
        }


        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager
                .SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }
    }
}