using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

public class CustomerLoginController : Controller
{
    private readonly ILogger<CustomerLoginController> _logger;
    
    // Static credentials for POC
    private const string DEFAULT_CUSTOMER_USERNAME = "customer";
    private const string DEFAULT_CUSTOMER_PASSWORD = "customer123";
    private const string CUSTOMER_SESSION_KEY = "CustomerLoggedIn";
    private const string CUSTOMER_USERNAME_KEY = "CustomerUsername";

    public CustomerLoginController(ILogger<CustomerLoginController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // If already logged in, redirect to customer dashboard
        if (IsCustomerLoggedIn())
        {
            return RedirectToAction("Dashboard", "Customer");
        }

        var model = new CustomerLoginViewModel
        {
            Username = DEFAULT_CUSTOMER_USERNAME, // Pre-fill for convenience
            Password = string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(CustomerLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Static authentication for POC
        if (model.Username == DEFAULT_CUSTOMER_USERNAME && model.Password == DEFAULT_CUSTOMER_PASSWORD)
        {
            // Set session
            HttpContext.Session.SetString(CUSTOMER_SESSION_KEY, "true");
            HttpContext.Session.SetString(CUSTOMER_USERNAME_KEY, model.Username);
            
            _logger.LogInformation("Customer login successful: {Username}", model.Username);
            
            return RedirectToAction("Dashboard", "Customer");
        }

        ModelState.AddModelError("", "Invalid username or password");
        model.Password = string.Empty; // Clear password on error
        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        _logger.LogInformation("Customer logout");
        return RedirectToAction("Index", "CustomerLogin");
    }

    private bool IsCustomerLoggedIn()
    {
        return HttpContext.Session.GetString(CUSTOMER_SESSION_KEY) == "true";
    }
}

