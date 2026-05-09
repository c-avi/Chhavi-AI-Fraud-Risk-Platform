using Microsoft.ML;
using Microsoft.ML.Data;

var mlContext = new MLContext(seed: 42);

var rawRows = new List<RawFraudRow>
{
    new() { Amount = 40, RecentCount = 1, DistinctLocationCount = 1, LastLocation = "Home", Location = "Home", Label = false },
    new() { Amount = 120, RecentCount = 2, DistinctLocationCount = 1, LastLocation = "Home", Location = "Home", Label = false },
    new() { Amount = 250, RecentCount = 2, DistinctLocationCount = 2, LastLocation = "Home", Location = "Office", Label = false },
    new() { Amount = 400, RecentCount = 3, DistinctLocationCount = 2, LastLocation = "Office", Location = "Office", Label = false },
    new() { Amount = 850, RecentCount = 4, DistinctLocationCount = 3, LastLocation = "Office", Location = "Travel", Label = true },
    new() { Amount = 1200, RecentCount = 5, DistinctLocationCount = 3, LastLocation = "Travel", Location = "Travel", Label = true },
    new() { Amount = 1800, RecentCount = 6, DistinctLocationCount = 4, LastLocation = "Travel", Location = "Unknown", Label = true },
    new() { Amount = 2300, RecentCount = 7, DistinctLocationCount = 4, LastLocation = "Unknown", Location = "Unknown", Label = true },
    new() { Amount = 520, RecentCount = 3, DistinctLocationCount = 1, LastLocation = "Home", Location = "Home", Label = false },
    new() { Amount = 1600, RecentCount = 6, DistinctLocationCount = 5, LastLocation = "Unknown", Location = "Abroad", Label = true },
    new() { Amount = 90, RecentCount = 1, DistinctLocationCount = 2, LastLocation = "Home", Location = "Office", Label = false },
    new() { Amount = 2100, RecentCount = 8, DistinctLocationCount = 5, LastLocation = "Abroad", Location = "Abroad", Label = true }
};

var trainingRows = rawRows.Select(ToTrainingRow).ToList();
var trainingData = mlContext.Data.LoadFromEnumerable(trainingRows);

var pipeline = mlContext.Transforms.Concatenate(
        "Features",
        nameof(TrainingFraudRow.NormalizedAmount),
        nameof(TrainingFraudRow.AmountToAverageRatio),
        nameof(TrainingFraudRow.RecentTransactionCount),
        nameof(TrainingFraudRow.DistinctLocationCount),
        nameof(TrainingFraudRow.IsLocationChanged))
    .Append(mlContext.BinaryClassification.Trainers.FastTree(
        new Microsoft.ML.Trainers.FastTree.FastTreeBinaryTrainer.Options
        {
            LabelColumnName = nameof(TrainingFraudRow.Label),
            FeatureColumnName = "Features",
            NumberOfLeaves = 8,
            NumberOfTrees = 100,
            MinimumExampleCountPerLeaf = 12,
            LearningRate = 0.2
        }))
    .Append(mlContext.BinaryClassification.Calibrators.Platt(
        labelColumnName: nameof(TrainingFraudRow.Label),
        scoreColumnName: "Score"));

var model = pipeline.Fit(trainingData);

var repoRoot = ResolveRepoRoot();
var modelDirectory = Path.Combine(repoRoot, "api", "src", "FraudRiskApi", "Models", "ML");
Directory.CreateDirectory(modelDirectory);
var modelPath = Path.Combine(modelDirectory, "fraud-risk-model.zip");

mlContext.Model.Save(model, trainingData.Schema, modelPath);
Console.WriteLine($"Model generated at: {modelPath}");

static string ResolveRepoRoot()
{
    var current = Directory.GetCurrentDirectory();
    if (Directory.Exists(Path.Combine(current, "api")))
    {
        return current;
    }

    var parent = Directory.GetParent(current)?.FullName;
    if (parent is not null && Directory.Exists(Path.Combine(parent, "api")))
    {
        return parent;
    }

    throw new InvalidOperationException(
        "Could not locate repository root. Run this from the repository root or scripts directory.");
}

static TrainingFraudRow ToTrainingRow(RawFraudRow row)
{
    var normalizedAmount = ApplyLogCompression(Math.Clamp(row.Amount / 2500f, 0f, 1f));
    var amountToAverageRatio = ApplyLogCompression(MathF.Max(0f, row.Amount / 600f));
    var recentTransactionCount = Math.Clamp(row.RecentCount, 0f, 10f);
    var distinctLocationCount = ApplyLaplaceLocationSmoothing(row.DistinctLocationCount);
    var hasLocationChanged = !string.Equals(row.LastLocation, row.Location, StringComparison.OrdinalIgnoreCase);
    var isLocationChanged = hasLocationChanged
        ? GetLocationChangeWeight(row.DistinctLocationCount)
        : 0f;

    return new TrainingFraudRow
    {
        Label = row.Label,
        NormalizedAmount = normalizedAmount,
        AmountToAverageRatio = amountToAverageRatio,
        RecentTransactionCount = recentTransactionCount,
        DistinctLocationCount = distinctLocationCount,
        IsLocationChanged = isLocationChanged
    };
}

static float ApplyLogCompression(float value)
{
    var safe = MathF.Max(0f, value);
    // Preserve ordering while widening distance between baseline spend and outliers.
    return MathF.Log(1f + safe) * FeatureEngineeringConstants.AmountLogSensitivityMultiplier;
}

static float ApplyLaplaceLocationSmoothing(float distinctLocationCount)
{
    const float pseudoCount = FeatureEngineeringConstants.LocationSmoothingAlpha;
    const float pseudoWindow = FeatureEngineeringConstants.LocationSmoothingAlpha * 2f;
    var safeCount = MathF.Max(0f, distinctLocationCount);
    return (safeCount + pseudoCount) / (1f + pseudoWindow);
}

static float GetLocationChangeWeight(float distinctLocationCount)
{
    var safeCount = MathF.Max(0f, distinctLocationCount);
    return safeCount <= 1f ? FeatureEngineeringConstants.NewLocationChangeWeight : 1f;
}

file static class FeatureEngineeringConstants
{
    public const float AmountLogSensitivityMultiplier = 1.5f;
    public const float LocationSmoothingAlpha = 0.5f;
    public const float NewLocationChangeWeight = 0.65f;
}

file sealed class RawFraudRow
{
    public float Amount { get; init; }
    public float RecentCount { get; init; }
    public float DistinctLocationCount { get; init; }
    public required string LastLocation { get; init; }
    public required string Location { get; init; }
    public bool Label { get; init; }
}

file sealed class TrainingFraudRow
{
    public bool Label { get; init; }
    public float NormalizedAmount { get; init; }
    public float AmountToAverageRatio { get; init; }
    public float RecentTransactionCount { get; init; }
    public float DistinctLocationCount { get; init; }
    public float IsLocationChanged { get; init; }
}
