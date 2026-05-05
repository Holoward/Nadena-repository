# Nadena

Consent-based behavioral data marketplace. Contributors export their YouTube, Spotify, and Netflix activity via Google Takeout and choose to license anonymized behavioral records to researchers and AI companies. The platform handles consent documentation, validation, anonymization, delivery, and payment splitting automatically.

**nadena.tech** -- david@nadena.tech

---

## What it does

- Contributors authorize Google Drive access once, request a Takeout export, and the platform picks it up automatically
- A background service polls each contributor's Drive every 30 minutes for new exports
- Every submission runs through structural validation, schema checks, timestamp plausibility, and account ID cross-referencing before any payment is credited
- Anonymized behavioral signals are forwarded directly to the buyer's configured delivery endpoint -- no raw data is ever stored
- Revenue splits automatically: configurable percentages to contributors (PayPal Payouts), distribution partners, and platform

---

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, onion architecture, MediatR CQRS, FluentValidation, Serilog |
| Database | PostgreSQL -- Dual-Context (Application & Identity) -- EF Core migrations |
| Auth | ASP.NET Core Identity + JWT + Contributor OAuth Tokens |
| Buyer payments | Stripe Checkout + Webhook |
| Contributor payouts | PayPal Payouts API |
| Delivery | Real-time webhook delivery to Buyer's DeliveryEndpoint |
| Storage | Cloudflare R2 -- Zero-Persistence process-and-delete model |
| Google OAuth | drive.readonly scope -- AES-256 encrypted refresh tokens |
| Background service | DrivePollingService (IHostedService) -- 30-minute interval |
| Frontend | React + React Native apps |
| Testing | WebApplicationFactory + xUnit (InMemory integration tests) |

---

## Architecture

```
Domain/          -- entities only (Volunteer, DatasetPurchase, Wallet, LedgerTransaction, ContributorOAuthToken)
Application/     -- commands, queries, interfaces (ProcessDatasetSalePayments, PayVolunteers, ITakeoutValidationService)
Persistence/     -- implementations (TakeoutValidationService, DataDeliveryService, DrivePollingService, GoogleDriveService)
WebApi/          -- controllers + React frontend (TakeoutController, OAuthController, WebhookController, AdminController)
```

---

## Running locally

### Prerequisites
- .NET 10 SDK
- Node.js v18+
- PostgreSQL server running locally (default: localhost:5432)

### Backend
```bash
cd WebApi
# Apply PostgreSQL migrations to both DbContexts
dotnet ef database update -c ApplicationDbContext -p ../Persistence -s .
dotnet ef database update -c NadenaIdentityDbContext -p ../Persistence -s .
dotnet run
```
API runs at http://localhost:5000. Swagger at http://localhost:5000/swagger.

### E2E Integration Tests
```bash
dotnet test Tests/NadenaE2ETests/NadenaE2ETests.csproj
```
Tests automatically use an isolated `InMemory` database provider for safe execution.

### Frontend (development)
```bash
cd WebApi/ClientApp
NODE_OPTIONS=--openssl-legacy-provider npm install
NODE_OPTIONS=--openssl-legacy-provider npm start
```
React dev server runs at http://localhost:44391.

### Production build
```bash
cd WebApi/ClientApp
NODE_OPTIONS=--openssl-legacy-provider npm run build
cd ..
dotnet publish -c Release -o /var/www/nadena
dotnet /var/www/nadena/WebApi.dll
```

---

## Configuration

Copy appsettings.example.json to appsettings.Development.json and fill in:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=NadenaAppDb;Username=postgres;Password=postgres",
    "IdentityConnection": "Host=localhost;Database=NadenaIdentityDb;Username=postgres;Password=postgres"
  },
  "UseInMemoryDatabase": false,
  "NadenaSettings": {
    "GoogleClientId": "",
    "GoogleClientSecret": "",
    "GoogleRedirectUri": "http://localhost:5000/api/v1/oauth/callback",
    "TokenEncryptionKey": "",
    "StripeWebhookSecret": "",
    "StripeSecretKey": "",
    "PayPalClientId": "",
    "PayPalClientSecret": "",
    "PayPalMode": "sandbox",
    "FrontendUrl": "http://localhost:3000",
    "ContributorSharePercent": 60,
    "ModeSharePercent": 10,
    "NadenaSharePercent": 30
  }
}
```

Generate the encryption key:
```bash
openssl rand -base64 32
```

---

## Contributor submission flow

1. Contributor authorizes Google Drive (read-only) once via /upload
2. Requests a Google Takeout export -- one tap to a pre-configured URL
3. Google places the ZIP in their Drive (hours later)
4. DrivePollingService detects it automatically every 30 minutes
5. ZIP is validated, anonymized, forwarded to the buyer endpoint, and deleted
6. Contributor wallet is credited

Manual upload via phone browser is also supported at /upload without OAuth.

---

## Hard rules

- No AutoMapper -- .NET 10 reflection incompatibility
- JWT secret must appear in both appsettings.json and appsettings.Development.json
- No video titles, channel names, or URLs stored at any point
- Raw Takeout ZIPs deleted from memory after processing -- never written to disk
- Revenue split always reads from IConfiguration, never hardcoded
