using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.UserName,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    // View Models
    public class RegisterViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "E-posta gereklidir.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Şifre gereklidir.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Şifre en az {2} karakter olmalıdır.", MinimumLength = 6)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Şifre")]
        public string Password { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Şifre Tekrar")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Şifre gereklidir.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Şifre")]
        public string Password { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
