using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BanksController : ControllerBase
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<BanksController> _logger;

    public BanksController(MoneyFexDbContext context, ILogger<BanksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBanks([FromQuery] string? countryCode)
    {
        try
        {
            var query = _context.Banks.AsQueryable();

            if (!string.IsNullOrEmpty(countryCode))
            {
                query = query.Where(b => b.CountryCode == countryCode);
            }

            var banks = await query
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new
                {
                    id = b.Id,
                    name = b.Name,
                    code = b.Code,
                    countryCode = b.CountryCode
                })
                .ToListAsync();

            return Ok(banks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting banks");
            return StatusCode(500, new { error = "Error retrieving banks" });
        }
    }
}

