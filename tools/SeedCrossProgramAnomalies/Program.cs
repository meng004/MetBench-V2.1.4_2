// T5 PR-2 — Seed cross-program anomaly findings into SystemMT.Litedb
// Reads the JSON manifest produced by tools/import_cross_program_anomalies.py
// and inserts one Anomaly row per case directly via LiteDB.
//
// Usage:
//   dotnet run --project tools/SeedCrossProgramAnomalies -- \
//       --input docs/experiments/cross-program-anomalies-2026-05-28.json \
//       --db MetBench_Client/bin/Debug/net8.0-windows7.0/SystemMT.Litedb
//
// Idempotency: skips rows already present in the target collection by
// matching (Category="cross-program-disagreement" AND Notes-prefix
// containing the transform name).

using System.Text.Json;
using LiteDB;
using MetBench_Domain;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("usage: seed-cross-program-anomalies --input <json> --db <path>");
    return 64;
}

string? inputPath = null;
string? dbPath = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--input" && i + 1 < args.Length) inputPath = args[++i];
    else if (args[i] == "--db" && i + 1 < args.Length) dbPath = args[++i];
}

if (inputPath is null || dbPath is null)
{
    Console.Error.WriteLine("--input and --db are both required");
    return 2;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input JSON not found: {inputPath}");
    return 2;
}

var json = await File.ReadAllTextAsync(inputPath);
using var doc = JsonDocument.Parse(json);
if (!doc.RootElement.TryGetProperty("anomalies", out var anomalyArray))
{
    Console.Error.WriteLine("JSON missing 'anomalies' array");
    return 3;
}

using var db = new LiteDatabase($"Filename={dbPath}");
db.Pragma("UTC_DATE", true);
var collection = db.GetCollection<Anomaly>("Anomalies");

int seeded = 0;
int skipped = 0;
foreach (var element in anomalyArray.EnumerateArray())
{
    string transform = element.GetProperty("transform").GetString() ?? "";
    string openmocScenario = element.TryGetProperty("openmoc_scenario", out var oms) ? oms.GetString() ?? "" : "";
    string severity = element.GetProperty("severity").GetString() ?? "minor";
    string category = element.GetProperty("category").GetString() ?? "cross-program-disagreement";
    double deltaK = element.TryGetProperty("delta_k", out var dk) ? dk.GetDouble() : 0.0;
    double budget = element.TryGetProperty("budget", out var b) ? b.GetDouble() : 0.0;
    double percent = element.TryGetProperty("percent_of_openmc", out var p) ? p.GetDouble() : 0.0;

    var notes =
        $"Cross-program disagreement (OpenMOC vs OpenMC). Transform={transform}, " +
        $"OpenMOC scenario={openmocScenario}, |Δk|={deltaK:F5} ({percent:F1}% of OpenMC), " +
        $"budget={budget:F5}. Root-cause discussion: docs/experiments/discussion-phase2.md. " +
        $"Seeded by tools/SeedCrossProgramAnomalies on {DateTime.UtcNow:yyyy-MM-dd}.";

    string notesPrefix = $"Cross-program disagreement (OpenMOC vs OpenMC). Transform={transform}";
    bool already = collection
        .Find(a => a.Category == category)
        .Any(a => a.Notes != null && a.Notes.StartsWith(notesPrefix, StringComparison.Ordinal));
    if (already)
    {
        skipped++;
        Console.WriteLine($"SKIP {transform} (already in collection)");
        continue;
    }

    var anomaly = new Anomaly
    {
        IdAnomaly = Guid.NewGuid(),
        ResultId = Guid.Empty, // No live Result row; report-only finding
        Severity = severity,
        Status = "investigating",
        Category = category,
        DiscoveredAt = DateTime.UtcNow,
        Notes = notes,
        ReplayCount = 0,
        LinkedKnownBugId = null,
    };

    collection.Insert(anomaly);
    seeded++;
    Console.WriteLine($"SEED {transform} → {severity} / {category} (Id={anomaly.IdAnomaly})");
}

Console.WriteLine($"Seeded {seeded} anomalies, skipped {skipped} duplicate(s) (category=cross-program-disagreement).");
return 0;
