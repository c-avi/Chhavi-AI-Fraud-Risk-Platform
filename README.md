# Chhavi-AI-Fraud-Risk-Platform

Scores transactions based on fraud likelihood using behavioral and transactional patterns.

## AI Fraud Risk Scoring Setup

The backend now uses an injected AI-style scoring engine (`IFraudRiskModelEngine`) with a default `MockFraudRiskModelEngine` implementation. This preserves the `Controller -> Service -> Repository` flow while making it easy to swap in an ML.NET model later.

Backend requirements:

- .NET SDK 10
- SQL Server Express or another SQL Server instance reachable from the configured connection string

Frontend requirements:

- Node.js 22+
- Angular CLI 21

Backend setup:

```powershell
cd api\src\FraudRiskApi
dotnet restore
dotnet run
```

Update `api/src/FraudRiskApi/appsettings.json` if your SQL Server instance is different from `.\\SQLEXPRESS`.

Frontend setup:

```powershell
cd ui
npm install
npm start
```

The Angular app expects the API at `http://localhost:5257/api/v1`.

## Dashboard Preview

The dashboard now exposes API-backed summary cards for:

- Total Transactions Processed
- High-Risk Alerts Count
- Average Fraud Risk Score

Screenshot:

![Risk summary cards](docs/assets/risk-summary-cards.svg)

## Flow Documentation

- Sequence diagram: [docs/ai-risk-scoring-sequence.md](docs/ai-risk-scoring-sequence.md)
- UI verification checklist: [ui/tests/risk-summary-cards-checklist.md](ui/tests/risk-summary-cards-checklist.md)
