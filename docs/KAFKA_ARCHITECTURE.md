# Kafka Producer and Consumer Architecture for Money Transfer

## Complete Money Transfer Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    CUSTOMER PAYMENT (CASH IN)                    │
│                    ⚠️ SYNCHRONOUS - NO KAFKA                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  User selects payment method            │
        │  (Card, Bank Account, Mobile Wallet)    │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Payment Gateway API Call               │
        │  (Stripe, WorldPay, etc.)               │
        │  ✅ SYNCHRONOUS - Immediate response    │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Payment Confirmed?                     │
        │  Status: PaymentPending → Paid          │
        └─────────────────────────────────────────┘
                              │
                              ▼ YES
┌─────────────────────────────────────────────────────────────────┐
│                    🚀 PRODUCER USED HERE                        │
│                    (After Payment Confirmation)                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Producer.EnqueueTransferAsync()        │
        │  Sends message to Kafka topic           │
        │  Status: Paid → InProgress              │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Kafka Topic: "money-transfers"          │
        │  Message stored in Kafka                │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  API returns immediately                │
        │  User sees "Processing..."              │
        └─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    🔄 CONSUMER USED HERE                        │
│                    (Background Processing)                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  BackgroundService consumes message     │
        │  TransferProcessingHandler processes     │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Step 1: Currency Conversion            │
        │  (If needed - exchange rate API)        │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Step 2: Cash Out API Calls             │
        │  - Mobile Wallet API (MTN, Airtel)      │
        │  - Bank Transfer API                    │
        │  - Cash Pickup API                      │
        │  - KiiBank API                          │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  Update Transaction Status              │
        │  Status: InProgress → Completed         │
        │  Save TransferReference                 │
        └─────────────────────────────────────────┘
```

## Key Points

### ✅ PRODUCER is used:
- **Location**: After payment is confirmed (Status = Paid)
- **When**: In `MobileMoneyTransferController.QueueTransferForProcessingAsync()`
- **Purpose**: Queue the transfer for background processing
- **Why**: Payment is confirmed, now we need to process the transfer asynchronously

### ✅ CONSUMER is used:
- **Location**: BackgroundService continuously listening to Kafka
- **When**: Automatically when message arrives in Kafka topic
- **Purpose**: Process the transfer (currency conversion + cash out APIs)
- **Why**: These operations can take time and should not block the API

## API Integration Points

### 1. Payment Collection (Cash In) - SYNCHRONOUS
```
Controller → Payment Gateway API → Response
- Stripe API
- WorldPay API
- Bank Account Verification
- Mobile Wallet Balance Check
```
**No Kafka needed** - Must be synchronous for immediate feedback

### 2. Currency Conversion - ASYNCHRONOUS (in Consumer)
```
Consumer → Exchange Rate API → Convert Amount
- Real-time exchange rate API
- Calculate receiving amount
- Update transaction
```
**In Consumer** - Can be done in background

### 3. Transfer to Recipient (Cash Out) - ASYNCHRONOUS (in Consumer)
```
Consumer → Recipient API → Transfer Money
- Mobile Wallet API (MTN, Airtel, etc.)
- Bank Transfer API
- Cash Pickup API
- KiiBank API
```
**In Consumer** - These APIs can be slow, must be async

## Example Flow

### Scenario: Mobile Money Transfer

1. **User creates transfer** → Transaction created (Status: PaymentPending)
2. **User pays with card** → Payment Gateway API called (SYNCHRONOUS)
3. **Payment confirmed** → Status: Paid
4. **PRODUCER sends to Kafka** → Status: InProgress
5. **API returns** → User sees "Processing..."
6. **CONSUMER processes**:
   - Get exchange rate (if needed)
   - Call Mobile Wallet API (MTN/Airtel) to transfer money
   - Update transaction with TransferReference
   - Status: Completed
7. **User can check status** → Transaction shows as Completed

## Benefits

1. **Fast API Response**: User doesn't wait for slow external APIs
2. **Reliability**: Kafka ensures message is processed even if service restarts
3. **Scalability**: Multiple consumers can process transfers in parallel
4. **Retry Logic**: Failed transfers can be retried automatically
5. **Separation of Concerns**: Payment collection vs. transfer processing

