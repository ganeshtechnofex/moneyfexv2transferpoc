# Architecture Documentation

## System Architecture

### Overview

The MoneyFex system follows a **Modular Monolithic Architecture** with direct database access, which provides:
- Clear module boundaries
- Separation of concerns
- Maintainability and scalability
- Ease of deployment
- Simplified architecture (no separate API layer)
- Direct Entity Framework Core access from Web layer

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    MoneyFex.Web                          │
│              (MVC Application)                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Controllers│  │Views     │  │ViewModels│            │
│  └──────────┘  └──────────┘  └──────────┘            │
└────────────────────┬────────────────────────────────────┘
                     │ Direct Access
┌────────────────────▼────────────────────────────────────┐
│              MoneyFex.Infrastructure                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Services  │  │Repositories│  │DbContext │            │
│  └──────────┘  └──────────┘  └──────────┘            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                  MoneyFex.Core                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Entities  │  │Interfaces │  │Enums     │            │
│  └──────────┘  └──────────┘  └──────────┘            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              PostgreSQL Database                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │Transactions│  │Reference │  │Auxiliary │            │
│  │Tables     │  │Tables    │  │Tables    │            │
│  └──────────┘  └──────────┘  └──────────┘            │
└─────────────────────────────────────────────────────────┘
```

## Data Flow

### Request Flow

1. **User Request** → Web Controller receives HTTP request
2. **Controller** → Uses services/repositories or directly accesses DbContext
3. **Infrastructure** → Executes database queries via Entity Framework Core
4. **Database** → Returns data
5. **Response** → Controller builds ViewModel and returns View

### Example Flow: Creating a Transaction

```
User submits form
    ↓
Controller (e.g., KiiBankTransferController)
    ↓
Service/Repository (e.g., TransactionService) OR Direct DbContext access
    ↓
Entity Framework Core
    ↓
PostgreSQL Database
    ↓
Transaction saved
    ↓
Controller redirects to next page
```

## Layer Responsibilities

### 1. Web Layer (MoneyFex.Web)

**Responsibilities:**
- User interface and view rendering
- User input handling and validation
- Request routing and controller logic
- Session management
- Direct database access via Entity Framework Core

**Technologies:**
- ASP.NET Core MVC
- Razor views
- JavaScript/jQuery
- Entity Framework Core (direct access)

**Components:**
- **Controllers**: MVC controllers handling HTTP requests (15 controllers)
  - `HomeController` - Home page and exchange rate calculations
  - `TransferMoneyNowController` - Transfer money form
  - `KiiBankTransferController` - KiiBank transfer flow
  - `SenderBankAccountDepositController` - Bank deposit flow
  - `MobileMoneyTransferController` - Mobile money transfer flow
  - `SenderCashPickUpController` - Cash pickup flow
  - `TransactionHistoryController` - Transaction history and management
  - `TransactionsController` - Transaction listing and details
  - `CustomerController` - Customer dashboard
  - `StaffController` - Staff dashboard
  - `CustomerLoginController` - Customer authentication
  - `StaffLoginController` - Staff authentication
  - `BanksController` - Bank data endpoints
  - `CountriesController` - Country data endpoints
  - `WalletOperatorsController` - Wallet operator data endpoints
- **Views**: Razor views for UI rendering (44 views)
- **ViewModels**: Data transfer objects (23 ViewModels)
- **Services**: Web-specific services
  - `TransactionHistoryService` - Transaction history retrieval
  - `TransactionActivityService` - Transaction status and actions
  - `TransactionNoteService` - Transaction notes management
  - `TransferMoneyNowService` - Recent transfers and recipients
  - `TransactionLimitService` - Transaction limit validation

### 2. Infrastructure Layer (MoneyFex.Infrastructure)

**Responsibilities:**
- Data access (Entity Framework Core)
- Repository implementations
- Service implementations
- External service integration
- Database migrations

**Technologies:**
- Entity Framework Core
- PostgreSQL (Npgsql)
- Dependency Injection

**Components:**
- **Data**: 
  - `MoneyFexDbContext` - Main database context
  - `DatabaseInitializer` - Database initialization and seeding
  - `DbSeeder` - Data seeding
- **Repositories**: 
  - `TransactionRepository` - Transaction data access
  - `BankAccountDepositRepository` - Bank deposit data access
  - `MobileMoneyTransferRepository` - Mobile transfer data access
  - `CashPickupRepository` - Cash pickup data access
  - `KiiBankTransferRepository` - KiiBank transfer data access
- **Services**: 
  - `TransactionService` - Transaction business logic
  - `BankAccountDepositService` - Bank deposit business logic
  - `MobileMoneyTransferService` - Mobile transfer business logic
  - `CashPickupService` - Cash pickup business logic
  - `KiiBankTransferService` - KiiBank transfer business logic
  - `ExchangeRateService` - Exchange rate calculations
  - `KiiBankAccountValidationService` - KiiBank account validation

### 3. Core Layer (MoneyFex.Core)

**Responsibilities:**
- Domain entities
- Business interfaces
- Domain logic
- Enumerations

**Technologies:**
- .NET Standard
- Domain-Driven Design principles

**Components:**
- **Entities**: 
  - `Transaction` - Base transaction entity
  - `BankAccountDeposit` - Bank deposit entity
  - `MobileMoneyTransfer` - Mobile transfer entity
  - `CashPickup` - Cash pickup entity
  - `KiiBankTransfer` - KiiBank transfer entity
  - `Sender` - Sender entity
  - `Country` - Country entity
  - `Bank` - Bank entity
  - `MobileWalletOperator` - Mobile wallet operator entity
  - `CardPaymentInformation` - Card payment entity
  - And more...
- **Interfaces**: 
  - Repository interfaces (e.g., `ITransactionRepository`)
  - Service interfaces (e.g., `ITransactionService`)
  - `IExchangeRateService` - Exchange rate service interface
  - `IKiiBankAccountValidationService` - KiiBank validation interface
- **Enums**: 
  - `TransactionType` - Transaction type enumeration
  - `TransactionStatus` - Transaction status enumeration
  - `PaymentMode` - Payment mode enumeration
  - `TransactionModule` - Transaction module enumeration
  - And more...

## Design Patterns

### 1. Repository Pattern

**Purpose:** Abstract data access logic

**Implementation:**
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    // ...
}
```

**Benefits:**
- Testability
- Flexibility
- Separation of concerns

### 2. Service Layer Pattern

**Purpose:** Encapsulate business logic

**Implementation:**
```csharp
public interface ITransactionService
{
    Task<Transaction?> GetTransactionByIdAsync(int id);
    // ...
}
```

**Benefits:**
- Business logic encapsulation
- Transaction management
- Cross-cutting concerns

### 3. Dependency Injection

**Purpose:** Loose coupling and testability

**Implementation:**
```csharp
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
```

**Benefits:**
- Testability
- Maintainability
- Flexibility

## Database Design

### Normalization Strategy

1. **First Normal Form (1NF)**
   - Atomic values
   - No repeating groups

2. **Second Normal Form (2NF)**
   - 1NF + No partial dependencies
   - All non-key attributes depend on the full primary key

3. **Third Normal Form (3NF)**
   - 2NF + No transitive dependencies
   - Non-key attributes don't depend on other non-key attributes

### Transaction Model

```
Transaction (Base)
├── BankAccountDeposit
├── MobileMoneyTransfer
├── CashPickup
└── KiiBankTransfer
```

### Relationships

1. **One-to-One**
   - Transaction → BankAccountDeposit
   - Transaction → MobileMoneyTransfer
   - Transaction → CashPickup
   - Transaction → KiiBankTransfer

2. **One-to-Many**
   - Sender → Transactions
   - Country → Transactions (sending)
   - Country → Transactions (receiving)
   - Bank → BankAccountDeposits

3. **Many-to-One**
   - Transactions → Sender
   - Transactions → Countries
   - BankAccountDeposits → Bank

## Security Considerations

### 1. Data Protection
- Connection strings in configuration
- Sensitive data encryption
- SQL injection prevention (EF Core parameterized queries)
- Input validation and sanitization

### 2. Application Security
- HTTPS enforcement
- Session management
- Authentication (future)
- Authorization (future)

### 3. Database Security
- Role-based access
- Connection pooling
- Audit logging

## Performance Optimization

### 1. Database
- Strategic indexes
- Query optimization
- Connection pooling
- Pagination

### 2. Application
- Async/await patterns
- Caching (future)
- Response compression
- Lazy loading where appropriate

## Scalability

### Horizontal Scaling
- Stateless application design
- Database connection pooling
- Load balancing ready
- Session state management (can be moved to distributed cache)

### Vertical Scaling
- Efficient query patterns
- Proper indexing
- Resource optimization

## Monitoring and Logging

### Logging
- Structured logging
- Log levels
- Error tracking

### Monitoring
- Health checks
- Performance metrics
- Error rates

## Testing Strategy

### Unit Tests
- Service layer
- Repository layer
- Business logic

### Integration Tests
- Controller actions
- Database operations
- End-to-end flows
- Service layer integration

## Deployment

### Development
- Local PostgreSQL
- Development configuration
- Hot reload

### Production
- Production database
- Environment variables
- SSL/TLS
- Monitoring

## Future Enhancements

1. **API Layer (Optional)**
   - Add RESTful API layer if needed for mobile apps or third-party integrations
   - Separate API project for external access
   - Swagger/OpenAPI documentation

2. **Microservices Migration**
   - Split by transaction type
   - Independent scaling
   - Service mesh

3. **Event-Driven Architecture**
   - Event sourcing
   - CQRS pattern
   - Message queues

4. **Caching Layer**
   - Redis integration
   - Distributed caching
   - Cache invalidation

5. **Authentication & Authorization**
   - JWT token-based authentication
   - Role-based access control (RBAC)
   - Identity management

