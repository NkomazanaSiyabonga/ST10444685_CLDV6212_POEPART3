using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ABCRetailers.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;
        private readonly ISimpleHasher _hasher;

        public LoginController(AuthDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger, ISimpleHasher hasher)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
            _hasher = hasher;
        }

        // GET: /Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // DUAL PASSWORD VERIFICATION: Try hashed first, then plain text as fallback
                bool passwordValid = _hasher.VerifyPassword(model.Password, user.PasswordHash);

                // If hashed verification fails, check if it's an old plain text password
                if (!passwordValid)
                {
                    // Check if it matches the plain text (for existing users)
                    if (user.PasswordHash == model.Password)
                    {
                        // Convert to hashed password for future logins
                        user.PasswordHash = _hasher.HashPassword(model.Password);
                        await _db.SaveChangesAsync();
                        passwordValid = true;
                        _logger.LogInformation("Upgraded password to hashed for user: {Username}", user.Username);
                    }
                }

                if (!passwordValid)
                {
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // Rest of your login code...
                Customer customer = null;
                try
                {
                    customer = await _functionsApi.GetCustomerByUsernameAsync(user.Username);
                }
                catch (Exception apiEx)
                {
                    _logger.LogWarning(apiEx, "Azure Functions API unavailable for user {Username}, using fallback", user.Username);
                    customer = new Customer
                    {
                        RowKey = user.Username,
                        Username = user.Username
                    };
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("CustomerId", customer?.RowKey ?? user.Username)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    });

                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("CustomerId", customer?.RowKey ?? user.Username);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                // FIXED: Redirect based on user role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("AdminDashboard", "Home");
                }
                else if (user.Role == "Customer")
                {
                    return RedirectToAction("CustomerDashboard", "Home");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for user {Username}", model.Username);
                ViewBag.Error = $"Login failed: {ex.Message}";
                return View(model);
            }
        }

        // GET: /Login/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Login/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check duplicate username
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            try
            {
                // Save local user WITH HASHED PASSWORD
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = _hasher.HashPassword(model.Password), // Hashed!
                    Role = model.Role
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();


                try
                {
                    var customer = new Customer
                    {
                        Username = model.Username,
                        Name = model.FirstName,
                        Surname = model.LastName,
                        Email = model.Email,
                        ShippingAddress = model.ShippingAddress,
                        PartitionKey = "CUSTOMER",
                        RowKey = Guid.NewGuid().ToString()
                    };

                    await _functionsApi.CreateCustomerAsync(customer);
                }
                catch (Exception apiEx)
                {
                    _logger.LogWarning(apiEx, "Azure Functions API unavailable during registration for {Username}", model.Username);
                }

                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ViewBag.Error = "Could not complete registration. Please try again later.";
                return View(model);
            }
        }

        // LOGOUT
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ACCESS DENIED
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}