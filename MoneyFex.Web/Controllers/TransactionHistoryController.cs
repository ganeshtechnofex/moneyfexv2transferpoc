using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.Services;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

public class TransactionHistoryController : Controller
{
    private readonly TransactionHistoryService _transactionHistoryService;
    private readonly TransactionActivityService _transactionActivityService;
    private readonly TransactionNoteService _transactionNoteService;
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionHistoryController> _logger;

    public TransactionHistoryController(
        TransactionHistoryService transactionHistoryService,
        TransactionActivityService transactionActivityService,
        TransactionNoteService transactionNoteService,
        MoneyFexDbContext context,
        ILogger<TransactionHistoryController> logger)
    {
        _transactionHistoryService = transactionHistoryService;
        _transactionActivityService = transactionActivityService;
        _transactionNoteService = transactionNoteService;
        _context = context;
        _logger = logger;
    }

    // GET: TransactionHistory
    public async Task<IActionResult> Index(TransactionHistorySearchParamsViewModel? searchParams)
    {
        // Check if staff is logged in
        if (HttpContext.Session.GetString("StaffLoggedIn") != "true")
        {
            return RedirectToAction("Index", "StaffLogin");
        }

        try
        {
            // Initialize search params if not provided
            if (searchParams == null)
            {
                searchParams = new TransactionHistorySearchParamsViewModel
                {
                    PageSize = 10,
                    PageNum = 1,
                    CurrentpageCount = 0,
                    TransactionServiceType = 0 // Default to "All" (0)
                };
            }
            
            // If TransactionServiceType is 7 (Select), treat as 0 (All)
            if (searchParams.TransactionServiceType == 7)
            {
                searchParams.TransactionServiceType = 0;
            }

            // Set ViewBag for dropdowns
            await SetViewBagsAsync();

            // Get transaction history
            var viewModel = await _transactionHistoryService.GetTransactionHistoryAsync(searchParams);

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)viewModel.TotalNumberOfTransaction / searchParams.PageSize);
            ViewBag.NumberOfPage = totalPages;
            ViewBag.ButtonCount = Math.Min(10, totalPages - searchParams.CurrentpageCount);
            ViewBag.TransferMethod = searchParams.TransactionServiceType;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transaction history");
            var errorViewModel = new TransactionHistoryViewModel
            {
                SearchParamVm = searchParams ?? new TransactionHistorySearchParamsViewModel()
            };
            await SetViewBagsAsync();
            return View(errorViewModel);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Index(TransactionHistoryViewModel viewModel)
    {
        try
        {
            // Parse date range if provided
            if (!string.IsNullOrEmpty(viewModel.SearchParamVm.DateRange))
            {
                var dateParts = viewModel.SearchParamVm.DateRange.Split(" - ");
                if (dateParts.Length == 2)
                {
                    if (DateTime.TryParse(dateParts[0], out var fromDate))
                    {
                        viewModel.SearchParamVm.FromDate = fromDate;
                    }
                    if (DateTime.TryParse(dateParts[1], out var toDate))
                    {
                        viewModel.SearchParamVm.ToDate = toDate;
                    }
                }
            }

            // Set ViewBag for dropdowns
            await SetViewBagsAsync();

            // Get transaction history
            var result = await _transactionHistoryService.GetTransactionHistoryAsync(viewModel.SearchParamVm);

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)result.TotalNumberOfTransaction / viewModel.SearchParamVm.PageSize);
            ViewBag.NumberOfPage = totalPages;
            ViewBag.ButtonCount = Math.Min(10, totalPages - viewModel.SearchParamVm.CurrentpageCount);
            ViewBag.TransferMethod = viewModel.SearchParamVm.TransactionServiceType;

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transaction history");
            await SetViewBagsAsync();
            return View(viewModel);
        }
    }

    private async Task SetViewBagsAsync()
    {
        // Countries
        var countries = await _context.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.CountryName)
            .ToListAsync();
        ViewBag.SendingCountries = new SelectList(countries, "CountryCode", "CountryName");
        ViewBag.ReceivingCountries = new SelectList(countries, "CountryCode", "CountryName");

        // Currencies
        var currencies = await _context.Countries
            .Where(c => c.IsActive)
            .Select(c => new { Code = c.Currency, Name = $"{c.Currency} - {c.CurrencySymbol}" })
            .Distinct()
            .OrderBy(c => c.Code)
            .ToListAsync();
        ViewBag.SendingCurrencies = new SelectList(currencies, "Code", "Name");
        ViewBag.ReceivingCurrencies = new SelectList(currencies, "Code", "Name");

        // Staff (if you have staff table)
        // var staffs = await _context.Staff.Where(s => s.IsActive).ToListAsync();
        // ViewBag.Staffs = new SelectList(staffs, "Id", "FirstName");
    }

    #region API Endpoints

    /// <summary>
    /// Get transactions as JSON (API endpoint)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionHistorySearchParamsViewModel? searchParams)
    {
        try
        {
            // Initialize search params if not provided or if all values are default/empty
            if (searchParams == null)
            {
                searchParams = new TransactionHistorySearchParamsViewModel
                {
                    PageSize = 10,
                    PageNum = 1,
                    CurrentpageCount = 0,
                    TransactionServiceType = 0 // All transactions
                };
            }
            else
            {
                // Ensure defaults are set
                if (searchParams.PageSize <= 0) searchParams.PageSize = 10;
                if (searchParams.PageNum <= 0) searchParams.PageNum = 1;
                if (searchParams.CurrentpageCount < 0) searchParams.CurrentpageCount = 0;
            }

            // If TransactionServiceType is 7 (Select), treat as 0 (All)
            if (searchParams.TransactionServiceType == 7)
            {
                searchParams.TransactionServiceType = 0;
            }
            
            // Ensure TransactionServiceType has a valid value
            if (searchParams.TransactionServiceType < 0)
            {
                searchParams.TransactionServiceType = 0;
            }

            // Parse date range if provided
            if (!string.IsNullOrWhiteSpace(searchParams.DateRange))
            {
                var dateParts = searchParams.DateRange.Split(" - ");
                if (dateParts.Length == 2)
                {
                    if (DateTime.TryParse(dateParts[0].Trim(), out var fromDate))
                    {
                        searchParams.FromDate = fromDate;
                    }
                    if (DateTime.TryParse(dateParts[1].Trim(), out var toDate))
                    {
                        searchParams.ToDate = toDate;
                    }
                }
            }

            _logger.LogInformation("GetTransactions called with PageSize={PageSize}, PageNum={PageNum}, TransactionServiceType={TransactionServiceType}",
                searchParams.PageSize, searchParams.PageNum, searchParams.TransactionServiceType);

            // Get transaction history
            var viewModel = await _transactionHistoryService.GetTransactionHistoryAsync(searchParams);

            _logger.LogInformation("GetTransactions returned {Count} transactions, Total={Total}",
                viewModel.SenderTransactionStatement?.Count ?? 0, viewModel.TotalNumberOfTransaction);

            return Json(new
            {
                success = true,
                data = viewModel.SenderTransactionStatement ?? new List<TransactionStatementViewModel>(),
                totalCount = viewModel.TotalNumberOfTransaction,
                totalAmount = viewModel.TotalAmountWithCurrency ?? "0",
                totalFee = viewModel.TotalFeePaidwithCurrency ?? "0",
                searchParams = searchParams
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transactions via API");
            return Json(new
            {
                success = false,
                error = ex.Message,
                data = new List<TransactionStatementViewModel>(),
                totalCount = 0,
                totalAmount = "0",
                totalFee = "0"
            });
        }
    }

    #endregion

    #region Transaction Actions

    /// <summary>
    /// Check payment gateway status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckPGStatus(string refno, int transferMethod)
    {
        try
        {
            // For POC, use a default staff ID (in production, get from session/auth)
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionActivityService.CheckPGStatusAsync(refno, transferMethod, staffId);
            return Json(new { Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PG status for {RefNo}", refno);
            return Json(new { Data = new { Status = "Error", Message = ex.Message } });
        }
    }

    /// <summary>
    /// Get status report from API
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStatusReport(string identifier, int method)
    {
        try
        {
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionActivityService.GetStatusReportAsync(identifier, method, staffId);
            return Json(new { Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status report for {Identifier}", identifier);
            return Json(new { Data = new { Status = "Error", Message = ex.Message } });
        }
    }

    /// <summary>
    /// Manually approve a transaction
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ManualApproveTransaction(string refNo, int method)
    {
        try
        {
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionActivityService.ManualApproveTransactionAsync(refNo, method, staffId);
            return Json(new { Data = result.Message, Status = result.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually approving transaction {RefNo}", refNo);
            return Json(new { Data = ex.Message, Status = 0 });
        }
    }

    /// <summary>
    /// Approve a held transaction
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ApproveHoldTransaction(int Id, int method)
    {
        try
        {
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionActivityService.ApproveHoldTransactionAsync(Id, method, staffId);
            return Json(new { result = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving held transaction {Id}", Id);
            return Json(new { result = new { Status = 0, Message = ex.Message } });
        }
    }

    /// <summary>
    /// Cancel a transaction
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CancelTransaction(int transactionId, int transactionServiceType)
    {
        try
        {
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionActivityService.CancelTransactionAsync(transactionId, transactionServiceType, staffId);
            return Json(new { Status = (int)result.Status, Message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transaction {TransactionId}", transactionId);
            return Json(new { Status = 0, Message = ex.Message });
        }
    }

    #endregion

    #region Transaction Notes

    /// <summary>
    /// Get transaction notes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactionNote(int TransactionId, string TransactionMethodName)
    {
        try
        {
            var notes = await _transactionNoteService.GetTransactionNotesAsync(TransactionId, TransactionMethodName);
            return Json(new { result = notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction notes for {TransactionId}", TransactionId);
            return Json(new { result = new List<TransactionNoteViewModel>() });
        }
    }

    /// <summary>
    /// Save a transaction note
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveNote([FromBody] TransactionNoteViewModel noteViewModel)
    {
        try
        {
            int? staffId = null; // TODO: Get from authentication context
            
            var result = await _transactionNoteService.SaveTransactionNoteAsync(noteViewModel, staffId);
            return Json(new { Data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving transaction note");
            return Json(new { Data = false });
        }
    }

    #endregion

    #region Excel Export

    /// <summary>
    /// Export transaction statement to Excel
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportExcelOfTransactionStatement(
        string DateRange = "",
        int TransferMethod = 0,
        string SendingCountry = "",
        string ReceivingCountry = "",
        string searchParam = "",
        string senderName = "",
        string receiverName = "",
        string Status = "",
        string telephone = "",
        string SendingCurrency = "",
        string transactionWithAndWithoutFee = "",
        string ResponsiblePerson = "",
        string SearchByStatus = "",
        string MFCode = "",
        string SenderEmail = "",
        int PageNum = 0,
        int PageSize = 0)
    {
        try
        {
            // Build search parameters
            var searchParams = new TransactionHistorySearchParamsViewModel
            {
                DateRange = DateRange,
                MFCode = MFCode,
                PageNum = PageNum,
                PageSize = PageSize > 0 ? PageSize : int.MaxValue, // Get all records for export
                PhoneNumber = telephone,
                ReceiverName = receiverName,
                ReceivingCountry = ReceivingCountry,
                ResponsiblePerson = ResponsiblePerson,
                SearchByStatus = SearchByStatus,
                searchString = searchParam,
                SenderName = senderName,
                SenderEmail = SenderEmail,
                SendingCountry = SendingCountry,
                SendingCurrency = SendingCurrency,
                Status = Status,
                TransactionServiceType = TransferMethod,
                TransactionWithAndWithoutFee = !string.IsNullOrEmpty(transactionWithAndWithoutFee) 
                    ? int.Parse(transactionWithAndWithoutFee) 
                    : null,
                IsBusiness = false
            };

            // Parse date range
            if (!string.IsNullOrEmpty(DateRange))
            {
                var dateParts = DateRange.Split(" - ");
                if (dateParts.Length == 2)
                {
                    if (DateTime.TryParse(dateParts[0], out var fromDate))
                    {
                        searchParams.FromDate = fromDate;
                    }
                    if (DateTime.TryParse(dateParts[1], out var toDate))
                    {
                        searchParams.ToDate = toDate;
                    }
                }
            }

            // Get transaction data
            var viewModel = await _transactionHistoryService.GetTransactionHistoryAsync(searchParams);

            // Create CSV content (simplified - in production, use a library like EPPlus or ClosedXML)
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Receipt No,Date,Time,Sender Name,Sender Email,Sender Phone,MF Code,Sending Country,Receiving Country,Sending Currency,Receiving Currency,Amount,Fee,Receiving Amount,Receiver Name,Account No,Payout Provider,Status,Reference,Responsible Person,Staff Name");

            foreach (var transaction in viewModel.SenderTransactionStatement)
            {
                csv.AppendLine($"{transaction.identifier}," +
                    $"{transaction.DateTime}," +
                    $"{transaction.TransactionTime}," +
                    $"\"{transaction.SenderName}\"," +
                    $"\"{transaction.SenderEmail}\"," +
                    $"\"{transaction.SenderPhoneNumber}\"," +
                    $"{transaction.SenderMFAccountNo}," +
                    $"{transaction.SendingCountry}," +
                    $"{transaction.ReceivingCountry}," +
                    $"{transaction.SendingCurrency}," +
                    $"{transaction.ReceivingCurrency}," +
                    $"{transaction.Amount}," +
                    $"{transaction.Fee}," +
                    $"{transaction.ReceivingAmount}," +
                    $"\"{transaction.ReceiverName}\"," +
                    $"{transaction.ReceivingAccountNo}," +
                    $"\"{transaction.PayoutProviderName}\"," +
                    $"{transaction.TransactionStatusForAdmin}," +
                    $"{transaction.Reference}," +
                    $"{transaction.TransactionPerformedBy}," +
                    $"\"{transaction.UpdatedByStaffName}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"TransactionStatement_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction statement to Excel");
            TempData["message"] = "Error exporting transaction statement";
            TempData["status"] = "false";
            return RedirectToAction("Index");
        }
    }

    #endregion
}

