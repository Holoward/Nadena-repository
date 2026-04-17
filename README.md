# NADENA

NADENA is a B2B Data Licensing Engine and Consent-Based Data Monetization Platform. It acts as a secure, transparent technical bridge connecting everyday users who generate valuable digital data (Volunteers) with enterprises seeking high-quality analytic datasets (Buyers).

## Key Features

* **Consent & Privacy First**: A strict GDPR/CCPA respectful pipeline where volunteers actively choose to share specific data vectors and can instantly invoke full Takeout/Deletion requests to scrub their traces.
* **B2B Licensing Marketplace**: A data marketplace where enterprise clients actively browse pooled data (`DataPools`), build subscriptions, and securely purchase licenses utilizing seamless Stripe integrations.
* **Profit Sharing Ecosystem**: Volunteer data is anonymized and pooled. When enterprise clients purchase a license, the backend automatically calculates platform fees and distributes equitable `LedgerTransactions` down into the digital `Wallets` of contributing volunteers.
* **Universal Access**: Features specialized user interfaces for distinct roles — a React web dashboard for Enterprise Buyers and an Expo mobile application for Volunteers.

## System Architecture (The Onion)

The project backend is engineered leveraging **.NET 8/9 ASP.NET Core** and strict **Clean / Onion Architecture**. It enforces strict separation of concerns, heavily prioritizing CQRS design patterns.

### The Tech Stack
* **Backend Framework**: ASP.NET Core Web API
* **Application Logic**: CQRS driven by `MediatR`, fortified by `FluentValidation`. Telemetry runs via `Serilog`.
* **Database Pipeline**: Entity Framework Core. *(Note: Currently mapped to SQLite `OnionArchitecture.db` for rapid prototyping; ready to hot-swap to PostgreSQL).*
* **Authentication**: Token-based ASP.NET Core Identity & custom JWT configurations.
* **Frontends**: 
  * `WebApi/ClientApp/` — A comprehensive React.js Single Page Application functioning as the Buyer portal and Admin dashboard.
  * `NadenaApp/` — An Expo (React Native) build handling volunteer onboarding, surveys, and digital wallet tracking.
  * *Browser Extension Component (Chrome)* — A dedicated scraper injecting isolated content scripts to securely extract verified analytical traces.

### Navigating the Solution Layers
1. **`Domain`**: Pure enterprise logic. Absolutely zero external framework dependencies bleed into this layer. Contains core entities: `DataPool`, `Volunteer`, `Dataset`, `Wallet`, and `ConsentRecord`. 
2. **`Application`**: Application-specific handlers separated neatly into `/Features` (e.g., `Donation`, `Licensing`, `Takeout`, `Survey`).
3. **`Persistence`**: EF Core contexts, structural migrations, and automated database marketplace default `Seeders`.
4. **`WebApi`**: The Presentation layer edge. Contains the HTTP Controllers, API Rate Limiters, Stripe payload handlers, and global CORS policies inside `Program.cs`.

---

## Getting Started Locally

### Prerequisites
* **.NET 8.0 SDK** (or newer)
* **Node.js** (v18+ recommended)
* Optional: The Expo Go app on your mobile device for native testing.

### 1. Booting the Backend API Environment
First, restore NuGet dependencies and ensure your local database is migrated to the latest schema:
```bash
dotnet restore
dotnet ef database update --project Persistence --startup-project WebApi
```
Start the Kestrel Server:
```bash
cd WebApi
dotnet run
```
*The REST API spins up at `http://localhost:5034`. Test your endpoints visually via the automatic Swagger UI at `http://localhost:5034/swagger`.*

### 2. Booting the Web Dashboard (React SPA)
In a fresh terminal, deploy the Buyer/Admin dashboard:
```bash
cd WebApi/ClientApp
npm install
npm start
```
*The React application will ignite at `http://localhost:44391` (or 3000), interacting seamlessly with your .NET backend via configured endpoints.*

### 3. Booting the Mobile App (Expo)
To test the Volunteer flow interactively on your device, you **must substitute your physical LAN IP** (e.g. `192.168.x.x`) inward for your phone to route properly:
```bash
cd NadenaApp
npm install

# Write your network IP to the environment
echo "EXPO_PUBLIC_API_URL=http://<YOUR_LAN_IP>:5034/api/v1" > .env

# Start the metro bundler
npm run start
```
*Simply scan the generated QR Code using your device's camera mapping into Expo Go.*

---

## Deployment Notes
* **Secrets Management**: Local development runs via mock Stripe strings. In production, ensure you supply true tokens mapping to `StripeWebhookSecret` and `SmtpUser` inside `appsettings.json` or system variables.
* **Linux Deployments**: A bespoke `deploy.sh` script ships with this repository. It natively handles transpiling React, publishing the .NET build, establishing `nadena.service` locally on `systemd`, and locking down an Nginx reverse proxy via `nadena.conf`.
