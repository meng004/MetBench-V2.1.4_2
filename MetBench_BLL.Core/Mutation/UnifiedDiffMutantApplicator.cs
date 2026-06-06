using MetBench_Domain;

namespace MetBench_BLL.Mutation;

/// <summary>
/// Pure-C# <see cref="IMutantApplicator"/> implementation handling the unified-diff
/// subset that <see cref="Mutant.AppliedDiff"/> uses in practice:
/// <list type="bullet">
///   <item>One or more file sections, each with a <c>--- a/path</c> + <c>+++ b/path</c> header.</item>
///   <item>One or more <c>@@ -oldStart,oldCount +newStart,newCount @@</c> hunks per file.</item>
///   <item>Body lines starting with <c>' '</c> (context), <c>'-'</c> (remove), <c>'+'</c> (add).</item>
/// </list>
///
/// <para><b>Out of scope (throws <see cref="MutationApplicationException"/>)</b>:
/// binary patches, file creation/deletion markers, rename/copy headers, hunks whose
/// context does not match the base file. This is intentional — T6 today is a Prototype
/// (see <see cref="IMutantApplicator"/> remarks) and supporting richer diff features is
/// part of the deferred work. A failing diff is reported explicitly (CLAUDE.md §6), not
/// silently dropped.</para>
/// </summary>
public sealed class UnifiedDiffMutantApplicator : IMutantApplicator
{
    public async Task<string> ApplyAsync(
        Mutant mutant,
        string baseSutRoot,
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutant);
        if (string.IsNullOrWhiteSpace(mutant.AppliedDiff))
            throw new MutationApplicationException(
                $"Mutant '{mutant.IdMutant}' has an empty AppliedDiff — this is a configuration bug, not a no-op.");
        if (string.IsNullOrWhiteSpace(baseSutRoot))
            throw new ArgumentException("Base SUT root must be non-blank.", nameof(baseSutRoot));
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentException("Workspace root must be non-blank.", nameof(workspaceRoot));
        if (!Directory.Exists(baseSutRoot))
            throw new MutationApplicationException($"Base SUT root not found: {baseSutRoot}");

        var patchedRoot = Path.Combine(workspaceRoot, $"mutant-{mutant.IdMutant}-{Guid.NewGuid():N}");
        await Task.Run(() => CopyDirectory(baseSutRoot, patchedRoot), cancellationToken);

        var fileSections = ParseUnifiedDiff(mutant.AppliedDiff);
        if (fileSections.Count == 0)
            throw new MutationApplicationException(
                $"Mutant '{mutant.IdMutant}' AppliedDiff contains no recognized file sections.");

        foreach (var section in fileSections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyFileSection(section, patchedRoot, mutant.IdMutant);
        }

        return patchedRoot;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
    }

    private static List<FileSection> ParseUnifiedDiff(string diff)
    {
        var lines = diff.Replace("\r\n", "\n").Split('\n');
        var sections = new List<FileSection>();
        FileSection? current = null;
        Hunk? hunk = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("--- ", StringComparison.Ordinal) && i + 1 < lines.Length
                && lines[i + 1].StartsWith("+++ ", StringComparison.Ordinal))
            {
                current = new FileSection(StripDiffPathPrefix(lines[i + 1][4..].Trim()));
                sections.Add(current);
                hunk = null;
                i++;
                continue;
            }
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                if (current is null)
                    throw new MutationApplicationException("Hunk header @@ before any --- / +++ header.");
                hunk = ParseHunkHeader(line);
                current.Hunks.Add(hunk);
                continue;
            }
            if (hunk is null) continue; // skip diff preamble or rename/copy headers we don't model
            if (line.Length == 0) continue; // trailing blank between sections is fine
            var marker = line[0];
            if (marker is ' ' or '+' or '-')
                hunk.Body.Add(line);
        }

        return sections;
    }

    private static string StripDiffPathPrefix(string raw)
    {
        // git-style diffs prefix paths with "a/" or "b/"; strip that for filesystem lookup.
        if (raw.StartsWith("a/", StringComparison.Ordinal) || raw.StartsWith("b/", StringComparison.Ordinal))
            return raw[2..];
        return raw;
    }

    private static Hunk ParseHunkHeader(string header)
    {
        // @@ -oldStart[,oldCount] +newStart[,newCount] @@ [context]
        var firstAt = header.IndexOf("@@", StringComparison.Ordinal);
        var lastAt = header.IndexOf("@@", firstAt + 2, StringComparison.Ordinal);
        if (lastAt < 0)
            throw new MutationApplicationException($"Malformed hunk header: {header}");
        var inner = header.Substring(firstAt + 2, lastAt - firstAt - 2).Trim();
        var parts = inner.Split(' ');
        if (parts.Length < 2 || !parts[0].StartsWith('-') || !parts[1].StartsWith('+'))
            throw new MutationApplicationException($"Malformed hunk header: {header}");
        var (oldStart, _) = ParseRange(parts[0][1..]);
        var (newStart, _) = ParseRange(parts[1][1..]);
        return new Hunk(oldStart, newStart);
    }

    private static (int Start, int Count) ParseRange(string raw)
    {
        var comma = raw.IndexOf(',');
        if (comma < 0) return (int.Parse(raw), 1);
        return (int.Parse(raw[..comma]), int.Parse(raw[(comma + 1)..]));
    }

    private static void ApplyFileSection(FileSection section, string patchedRoot, int mutantId)
    {
        var targetPath = Path.Combine(patchedRoot, section.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(targetPath))
            throw new MutationApplicationException(
                $"Mutant '{mutantId}': target file not found in working tree: {section.Path}");

        var original = File.ReadAllLines(targetPath).ToList();
        // Hunks reference line numbers in the original file, in ascending order. We apply
        // them in reverse so earlier indices are not shifted by edits we have already made.
        foreach (var hunk in section.Hunks.OrderByDescending(h => h.OldStart))
            ApplyHunk(hunk, original, section.Path, mutantId);

        File.WriteAllLines(targetPath, original);
    }

    private static void ApplyHunk(Hunk hunk, List<string> lines, string path, int mutantId)
    {
        // OldStart is 1-based; the hunk body interleaves ' ' / '-' / '+' lines, where
        // ' ' and '-' must match the original sequence and '+' lines are the replacement.
        var cursor = hunk.OldStart - 1;
        var inserts = new List<string>();
        var deletes = 0;
        foreach (var bodyLine in hunk.Body)
        {
            var marker = bodyLine[0];
            var text = bodyLine[1..];
            if (marker == ' ')
            {
                FlushPendingEdits(lines, ref cursor, inserts, ref deletes);
                if (cursor >= lines.Count || lines[cursor] != text)
                    throw new MutationApplicationException(
                        $"Mutant '{mutantId}': hunk context mismatch in {path} at line {cursor + 1}.");
                cursor++;
            }
            else if (marker == '-')
            {
                if (cursor + deletes >= lines.Count || lines[cursor + deletes] != text)
                    throw new MutationApplicationException(
                        $"Mutant '{mutantId}': hunk delete mismatch in {path} at line {cursor + deletes + 1}.");
                deletes++;
            }
            else if (marker == '+')
            {
                inserts.Add(text);
            }
        }
        FlushPendingEdits(lines, ref cursor, inserts, ref deletes);
    }

    private static void FlushPendingEdits(List<string> lines, ref int cursor, List<string> inserts, ref int deletes)
    {
        if (deletes > 0)
        {
            lines.RemoveRange(cursor, deletes);
            deletes = 0;
        }
        if (inserts.Count > 0)
        {
            lines.InsertRange(cursor, inserts);
            cursor += inserts.Count;
            inserts.Clear();
        }
    }

    private sealed record FileSection(string Path)
    {
        public List<Hunk> Hunks { get; } = new();
    }

    private sealed record Hunk(int OldStart, int NewStart)
    {
        public List<string> Body { get; } = new();
    }
}
