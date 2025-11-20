# Producer and Consumer Usage in Money Transfer System

## Quick Answer

### 🚀 **PRODUCER** is used:
**Location**: After payment is confirmed (Status = `Paid`)  
**File**: `MobileMoneyTransferController.QueueTransferForProcessingAsync()`  
**When**: Payment gateway confirms payment successfully  
**Purpose**: Queue the transfer for background processing

### 🔄 **CONSUMER** is used:
**Location**: BackgroundService continuously listening  
**File**: `TransferProcessingHandler.HandleAsync()`  
**When**: Automatically when message arrives in Kafka  
**Purpose**: Process the transfer (currency conversion + cash out APIs)

---

## Complete Flow with API Integration

### Phase 1: Payment Collection (CASH IN) - ⚠️ SYNCHRONOUS - NO KAFKA

```
User → Controller → Payment Gateway API → Response
```

**APIs Called:**
- Stripe API (Card payments)
- WorldPay API (Card payments)
- Bank Account Verification API
- Mobile Wallet Balance Check API

**Location**: `MobileMoneyTransferController.DebitCreditCardDetails()` (POST)

**Why Synchronous:**
- User needs immediate feedback (payment success/failure)
- Must validate payment before proceeding
- Cannot queue unpaid transactions

**Code Example:**
```csharp
// In Controller - SYNCHRONOUS
transaction.Status = TransactionStatus.Paid; // Payment confirmed
await _context.SaveChangesAsync();

// NOW call Producer
await QueueTransferForProcessingAsync(transaction, mobileTransfer);
```

---

### Phase 2: Queue Transfer - 🚀 PRODUCER USED HERE

```
Controller → Producer → Kafka Topic
```

**Location**: `MobileMoneyTransferController.QueueTransferForProcessingAsync()`

**What Happens:**
1. Create `TransferQueueMessage` with transaction details
2. Send to Kafka topic `money-transfers`
3. Update transaction status: `Paid` → `InProgress`
4. Return immediately to user

**Code:**
```csharp
var queueMessage = new TransferQueueMessage
{
    TransactionId = transaction.Id,
    ReceiptNo = transaction.ReceiptNo,
    TransferType = TransferType.MobileMoneyTransfer,
    Payload = JsonSerializer.Serialize(transferDetails),
    CreatedAt = DateTime.UtcNow,
    RetryCount = 0
};

await _transferQueueProducer.EnqueueTransferAsync(queueMessage);
```

**Why Producer:**
- Payment is confirmed, now we can process asynchronously
- User doesn't need to wait for slow external APIs
- Ensures message is persisted in Kafka

---

### Phase 3: Background Processing - 🔄 CONSUMER USED HERE

```
Kafka Topic → Consumer → Process Transfer → Update Database
```

**Location**: `TransferProcessingHandler.HandleAsync()`

**What Happens:**

#### Step 1: Currency Conversion (if needed)
```csharp
// In Consumer
var exchangeRateService = GetService<IExchangeRateService>();
var conversion = await exchangeRateService.GetRealTimeRateAsync(
    transaction.SendingCurrency, 
    transaction.ReceivingCurrency);
```

#### Step 2: Cash Out - Transfer to Recipient
```csharp
// In Consumer - Call External APIs
var walletApiService = GetService<IWalletApiService>();

// Mobile Wallet APIs
await walletApiService.CashOutAsync(new WalletCashOutRequest
{
    WalletOperator = "MTN", // or "AIRTEL", "M-PESA", etc.
    MobileNumber = mobileTransfer.PaidToMobileNo,
    Amount = transaction.ReceivingAmount,
    Currency = transaction.ReceivingCurrency
});

// OR Bank Transfer APIs
await bankApiService.TransferAsync(new BankTransferRequest
{
    BankCode = bankDeposit.BankCode,
    AccountNumber = bankDeposit.AccountNumber,
    Amount = transaction.ReceivingAmount
});

// OR Cash Pickup APIs
await cashPickupApiService.CreatePickupAsync(new CashPickupRequest
{
    Location = cashPickup.Location,
    Amount = transaction.ReceivingAmount
});
```

#### Step 3: Update Transaction
```csharp
// In Consumer
transaction.TransferReference = apiResponse.Reference;
transaction.Status = TransactionStatus.Completed;
await context.SaveChangesAsync();
```

**Why Consumer:**
- These APIs can be slow (5-30 seconds)
- Should not block the main API
- Can retry if API fails
- Can process multiple transfers in parallel

---

## Summary Table

| Phase | Operation | Location | Kafka? | Why |
|-------|-----------|----------|--------|-----|
| **1. Payment** | Collect money from customer | Controller | ❌ No | Must be synchronous for immediate feedback |
| **2. Queue** | Send to background processing | Controller → Producer | ✅ Yes | Payment confirmed, queue for async processing |
| **3. Process** | Currency conversion + Cash out | Consumer → Handler | ✅ Yes | Slow APIs, must be async |

---

## Example: Complete Mobile Money Transfer

### Step-by-Step:

1. **User creates transfer**
   - Transaction created (Status: `PaymentPending`)
   - No Kafka involved

2. **User pays with card**
   - Controller calls Stripe API (SYNCHRONOUS)
   - Stripe confirms payment
   - Status: `PaymentPending` → `Paid`
   - No Kafka involved

3. **Producer sends to Kafka** ✅
   - `QueueTransferForProcessingAsync()` called
   - Message sent to Kafka topic `money-transfers`
   - Status: `Paid` → `InProgress`
   - API returns immediately

4. **Consumer processes** ✅
   - BackgroundService consumes message
   - `TransferProcessingHandler.HandleAsync()` called
   - Step 1: Get exchange rate (if needed)
   - Step 2: Call MTN Mobile Money API to transfer money
   - Step 3: Update transaction with reference
   - Status: `InProgress` → `Completed`

5. **User checks status**
   - Transaction shows as `Completed`
   - Transfer reference available

---

## Key Takeaways

1. **Payment APIs (Cash In)** = Synchronous, No Kafka
   - User needs immediate feedback
   - Must validate payment before queuing

2. **Producer** = After payment confirmation
   - Queue the transfer for background processing
   - Fast response to user

3. **Consumer** = Background processing
   - Currency conversion APIs
   - Cash out APIs (wallet, bank, cash pickup)
   - Can be slow, async is required

4. **Separation of Concerns:**
   - Payment collection = Fast, synchronous
   - Transfer processing = Slow, asynchronous

---

## Files Reference

- **Producer Location**: `MobileMoneyTransferController.QueueTransferForProcessingAsync()`
- **Consumer Location**: `TransferProcessingHandler.HandleAsync()`
- **BackgroundService**: `TransferProcessingBackgroundService`
- **Kafka Topic**: `money-transfers`

