using Microsoft.ML;
using Microsoft.ML.Data;

var mlContext = new MLContext(seed: 42);

var rawRows = new List<RawFraudRow>
{
    new() { Amount = 40, RecentCount = 1, Location = "Home", Label = false },
    new() { Amount = 120, RecentCount = 2, Location = "Home", Label = false },
    new() { Amount = 250, RecentCount = 2, Location = "Office", Label = false },
    new() { Amount = 400, RecentCount = 3, Location = "Office", Label = false },
    new() { Amount = 850, RecentCount = 4, Location = "Travel", Label = true },
    new() { Amount = 1200, RecentCount = 5, Location = "Travel", Label = true },
    new() { Amount = 1800, RecentCount = 6, Location = "Unknown", Label = true },
    new() { Amount = 2300, RecentCount = 7, Location = "Unknown", Label = true },
    new() { Amount = 520, RecentCount = 3, Location = "Home", Label = false },
    new() { Amount = 1600, RecentCount = 6, Location = "Abroad", Label = true },
    new() { Amount = 90, RecentCount = 1, Location = "Office", Label = false },
    new() { Amount = 2100, RecentCount = 8, Location = "Abroad", Label = true }
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
            NumberOfLeaves = 16,
            NumberOfTrees = 100,
            MinimumExampleCountPerLeaf = 1,
            LearningRate = 0.2
        }));

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
    var normalizedAmount = Math.Clamp(row.Amount / 2500f, 0f, 1f);
    var amountToAverageRatio = Math.Clamp(row.Amount / 600f, 0f, 5f);
    var recentTransactionCount = Math.Clamp(row.RecentCount, 0f, 10f);

    var locationRiskMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = 1f,
        ["Office"] = 1f,
        ["Travel"] = 2f,
        ["Unknown"] = 3f,
        ["Abroad"] = 4f
    };

    locationRiskMap.TryGetValue(row.Location, out var distinctLocationCount);
    if (distinctLocationCount <= 0f)
    {
        distinctLocationCount = 2f;
    }

    var isLocationChanged = string.Equals(row.Location, "Home", StringComparison.OrdinalIgnoreCase) ? 0f : 1f;

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

file sealed class RawFraudRow
{
    public float Amount { get; init; }
    public float RecentCount { get; init; }
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
