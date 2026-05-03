# AI Model Integration

## Overview

The backend exposes an AI fraud prediction capability through `POST /api/ai/predict`. The implementation follows the existing layered backend structure:

- Controller: `AiController` receives and validates HTTP requests.
- Service: `AiPredictionService` builds the fraud-risk feature context and runs inference through `IFraudRiskModelEngine`.
- Model engine: `PredictiveFraudModelEngine` loads the ML.NET model artifact from `Models/ML/fraud-risk-model.zip` and performs prediction.
- Repository: `ITransactionRepository` provides historical transaction signals such as recent velocity, average amount, prior location, and distinct location count.

The service depends on abstractions rather than concrete implementations, so the model engine can be replaced with another ML provider without changing the API contract.

## API Contract

### POST `/api/ai/predict`

Request body:

```json
{
  "userId": "user-100",
  "amount": 12500.00,
  "location": "Bengaluru",
  "timestamp": "2026-04-30T12:00:00Z"
}
```

Successful response:

```json
{
  "userId": "user-100",
  "riskScore": 76,
  "riskLevel": "High",
  "confidenceScore": 0.76,
  "modelVersion": "fraud-risk-ml-v1",
  "predictedAtUtc": "2026-05-01T05:30:00Z"
}
```

Validation failures return `400 Bad Request`. Unexpected inference failures are logged and returned by the global exception middleware as problem details.

## Data Flow

1. Client sends a transaction-like payload to `POST /api/ai/predict`.
2. `AiController` delegates the request to `IAiPredictionService`.
3. `AiPredictionService` validates required fields and gathers historical user behavior from `ITransactionRepository`.
4. The service creates a `FraudRiskContext` containing amount, location, timestamp, recent transaction count, historical average amount, last transaction, and location diversity.
5. `IFraudRiskModelEngine` transforms that context into ML.NET model features and runs inference against the loaded model artifact.
6. The service returns the risk score, risk level, confidence score, model version, and prediction timestamp.
7. Logging captures prediction start, successful inference summary, and unexpected failures.

## Operational Notes

- The model artifact is copied to the output directory from `api/src/FraudRiskApi/Models/ML/fraud-risk-model.zip`.
- Dependency injection wires `IAiPredictionService` and `IFraudRiskModelEngine` in `Program.cs`.
- Integration tests run in the `Testing` environment and replace external dependencies with in-memory/test doubles.

