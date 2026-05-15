using MetBench_BLL.Discovery;
using MetBench_DAL.V2;

// scaffold-mr — CLI 包装 MrFeatureGenerator 生成 .feature 骨架到磁盘。
//
// 用法:
//   scaffold-mr --code MR-X --metapattern m_mono --suts openmoc,openmc \
//               --param physics.fuel.nu_sigma_f \
//               [--assertion greater] [--tolerance 0.001] [--factors 0.5,1.0,1.5] \
//               [--db ./metbench.litedb] [--output <path>] [--title "..."] [--note "..."]
//
// 失败时退出码非 0；成功打印生成的文件相对路径。

int Main(string[] args)
{
    try
    {
        var opts = ParseArgs(args);
        if (opts is null) return 2;

        var dbPath = opts.GetValueOrDefault("db") ?? "metbench.litedb";
        EnsureSeeded(dbPath);

        var repo = new LiteDbMetaPatternRepository(dbPath);
        var gen = new MrFeatureGenerator(repo);

        var spec = new MrFeatureSpec(
            Code: opts["code"],
            MetaPatternCode: opts["metapattern"],
            Suts: opts["suts"].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList(),
            ParameterAbstract: opts["param"],
            Title: opts.GetValueOrDefault("title"),
            Assertion: opts.GetValueOrDefault("assertion"),
            ToleranceRel: opts.TryGetValue("tolerance", out var t) ? double.Parse(t, System.Globalization.CultureInfo.InvariantCulture) : null,
            NoiseAware: opts.TryGetValue("noise-aware", out var n) ? bool.Parse(n) : null,
            ValueName: opts.GetValueOrDefault("value"),
            Factors: opts.TryGetValue("factors", out var f)
                ? f.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                : null,
            RationaleNote: opts.GetValueOrDefault("note"));

        var content = gen.Generate(spec);
        var output = opts.GetValueOrDefault("output") ?? gen.TargetRelativePath(spec);

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, content);

        Console.WriteLine($"✓ wrote {output}");
        Console.WriteLine($"  {spec.Suts.Count} SUT(s) × {(spec.Factors.Count == 0 ? 3 : spec.Factors.Count)} factor(s) Examples 行");
        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"scaffold-mr: {ex.Message}");
        return 1;
    }
}

static Dictionary<string, string>? ParseArgs(string[] args)
{
    if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  scaffold-mr --code MR-X --metapattern m_mono --suts openmoc,openmc --param physics.fuel.nu_sigma_f");
        Console.WriteLine("Required: --code --metapattern --suts --param");
        Console.WriteLine("Optional: --assertion --tolerance --noise-aware --value --factors --db --output --title --note");
        return null;
    }
    var opts = new Dictionary<string, string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--")) continue;
        var key = args[i][2..];
        if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
        {
            Console.Error.WriteLine($"missing value for --{key}");
            return null;
        }
        opts[key] = args[++i];
    }
    foreach (var req in new[] { "code", "metapattern", "suts", "param" })
        if (!opts.ContainsKey(req))
        {
            Console.Error.WriteLine($"missing required arg --{req}");
            return null;
        }
    return opts;
}

static void EnsureSeeded(string dbPath)
{
    var repo = new LiteDbMetaPatternRepository(dbPath);
    var n = MetaPatternSeed.EnsureSeeded(repo);
    if (n > 0) Console.WriteLine($"  (seeded {n} MetaPatterns into {dbPath})");
}

return Main(args);
