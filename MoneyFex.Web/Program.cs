using Common.Kafka.Consumer;
using Common.Kafka.Interfaces;
using Common.Kafka.Producer;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;
using MoneyFex.Core.Messages;
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

// Configure Kafka
var consumerConfigCredentials = new ConsumerConfig
{
    SaslUsername = builder.Configuration["KafkaConfig:SaslUsername"],
    BootstrapServers = builder.Configuration["KafkaConfig:BootstrapServers"],
    SaslPassword = builder.Configuration["KafkaConfig:SaslPassword"],
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.SaslSsl,
};
var producerConfigCredentials = new ProducerConfig
{
    SaslUsername = builder.Configuration["KafkaConfig:SaslUsername"],
    BootstrapServers = builder.Configuration["KafkaConfig:BootstrapServers"],
    SaslPassword = builder.Configuration["KafkaConfig:SaslPassword"],
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    Acks = Acks.All
};

var producerConfig = new ProducerConfig(producerConfigCredentials);
var consumerConfig = new ConsumerConfig(consumerConfigCredentials);

builder.Services.AddSingleton(producerConfig);
builder.Services.AddSingleton(consumerConfig);

// Register Kafka producers and consumers
builder.Services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));
builder.Services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));

// Register transfer queue producer
builder.Services.AddScoped<ITransferQueueProducer, TransferQueueProducer>();

// Register transfer processing handler and consumer
builder.Services.AddScoped<IKafkaHandler<string, TransferQueueMessage>, TransferProcessingHandler>();
builder.Services.AddHostedService<TransferProcessingBackgroundService>();

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

