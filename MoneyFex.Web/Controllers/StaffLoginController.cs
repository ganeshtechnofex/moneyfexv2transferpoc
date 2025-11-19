using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

public class StaffLoginController : Controller
{
    private readonly ILogger<StaffLoginController> _logger;
    
    // Static credentials for POC
    private const string DEFAULT_STAFF_USERNAME = "admin";
    private const string DEFAULT_STAFF_PASSWORD = "admin123";
    private const string STAFF_SESSION_KEY = "StaffLoggedIn";
    private const string STAFF_USERNAME_KEY = "StaffUsername";

    public StaffLoginController(ILogger<StaffLoginController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // If already logged in, redirect to dashboard
        if (IsStaffLoggedIn())
        {
            return RedirectToAction("Dashboard", "Staff");
        }

        var model = new StaffLoginViewModel
        {
            Username = DEFAULT_STAFF_USERNAME, // Pre-fill for convenience
            Password = string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(StaffLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Static authentication for POC
        if (model.Username == DEFAULT_STAFF_USERNAME && model.Password == DEFAULT_STAFF_PASSWORD)
        {
            // Set session
            HttpContext.Session.SetString(STAFF_SESSION_KEY, "true");
            HttpContext.Session.SetString(STAFF_USERNAME_KEY, model.Username);
            
            _logger.LogInformation("Staff login successful: {Username}", model.Username);
            
            return RedirectToAction("Dashboard", "Staff");
        }

        ModelState.AddModelError("", "Invalid username or password");
        model.Password = string.Empty; // Clear password on error
        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        _logger.LogInformation("Staff logout");
        return RedirectToAction("Index", "StaffLogin");
    }

    private bool IsStaffLoggedIn()
    {
        return HttpContext.Session.GetString(STAFF_SESSION_KEY) == "true";
    }
}

