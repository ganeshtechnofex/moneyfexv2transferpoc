using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Infrastructure.Repositories;
using MoneyFex.Infrastructure.Services;
using MoneyFex.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor for accessing session in views
builder.Services.AddHttpContextAccessor();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MoneyFexDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(warnings => 
               warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Register repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBankAccountDepositRepository, BankAccountDepositRepository>();
builder.Services.AddScoped<IMobileMoneyTransferRepository, MobileMoneyTransferRepository>();
builder.Services.AddScoped<ICashPickupRepository, CashPickupRepository>();
builder.Services.AddScoped<IKiiBankTransferRepository, KiiBankTransferRepository>();

// Register services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBankAccountDepositService, BankAccountDepositService>();
builder.Services.AddScoped<IMobileMoneyTransferService, MobileMoneyTransferService>();
builder.Services.AddScoped<ICashPickupService, CashPickupService>();
builder.Services.AddScoped<IKiiBankTransferService, KiiBankTransferService>();
builder.Services.AddScoped<IKiiBankAccountValidationService, KiiBankAccountValidationService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<TransactionHistoryService>();
builder.Services.AddScoped<TransactionActivityService>();
builder.Services.AddScoped<TransactionNoteService>();
builder.Services.AddScoped<TransferMoneyNowService>();
builder.Services.AddScoped<TransactionLimitService>();

var app = builder.Build();

// Apply database migrations and seed data on startup
try
{
    await DatabaseInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration failed during application startup.");
    throw;
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use session middleware (must be after UseRouting and before UseAuthorization)
app.UseSession();

app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

