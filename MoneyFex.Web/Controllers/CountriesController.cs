using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController : ControllerBase
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(MoneyFexDbContext context, ILogger<CountriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCountries()
    {
        try
        {
            // Check if Countries table exists and has data
            var countryCount = await _context.Countries.CountAsync();
            _logger.LogInformation("Countries table has {Count} records", countryCount);

            if (countryCount == 0)
            {
                _logger.LogWarning("Countries table is empty. Seeding may have failed.");
                // Return empty array instead of error to allow frontend to handle gracefully
                return Ok(new List<object>());
            }

            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new
                {
                    countryCode = c.CountryCode,
                    countryName = c.CountryName,
                    currency = c.Currency,
                    currencySymbol = c.CurrencySymbol
                })
                .ToListAsync();

            _logger.LogInformation("Returning {Count} active countries", countries.Count);
            return Ok(countries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting countries. Exception: {Message}, StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }

            return StatusCode(500, new { 
                error = "Error retrieving countries",
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }
}

