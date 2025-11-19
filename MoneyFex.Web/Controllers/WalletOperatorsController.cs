using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletOperatorsController : ControllerBase
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<WalletOperatorsController> _logger;

    public WalletOperatorsController(MoneyFexDbContext context, ILogger<WalletOperatorsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetWalletOperators([FromQuery] string? countryCode)
    {
        try
        {
            var query = _context.MobileWalletOperators.AsQueryable();

            if (!string.IsNullOrEmpty(countryCode))
            {
                query = query.Where(w => w.CountryCode == countryCode);
            }

            var operators = await query
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .Select(w => new
                {
                    id = w.Id,
                    name = w.Name,
                    code = w.Code,
                    countryCode = w.CountryCode
                })
                .ToListAsync();

            return Ok(operators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet operators");
            return StatusCode(500, new { error = "Error retrieving wallet operators" });
        }
    }
}

