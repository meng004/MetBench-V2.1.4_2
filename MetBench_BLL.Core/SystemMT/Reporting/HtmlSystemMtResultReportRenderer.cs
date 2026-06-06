using System.Globalization;
using System.Net;
using System.Text;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

public sealed class HtmlSystemMtResultReportRenderer : ISystemMtResultReportRenderer
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public string Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null)
        => Render(records, evidenceByExecutionId: null, context);

    public string Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        var ctx = context ?? new ReportContext();
        var generatedAt = ctx.GeneratedAt ?? DateTimeOffset.UtcNow;

        var list = records.ToList();
        var passed = list.Count(r => r.Passed);
        var failed = list.Count - passed;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.Append("<title>").Append(Esc(ctx.Title)).AppendLine("</title>");
        sb.AppendLine(InlineStyle());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.Append("<h1>").Append(Esc(ctx.Title)).AppendLine("</h1>");
        sb.AppendLine("<section class=\"summary\">");
        sb.Append("<p class=\"meta\">Generated at <time datetime=\"")
          .Append(generatedAt.ToString("o", Inv))
          .Append("\">")
          .Append(Esc(generatedAt.ToString("u", Inv)))
          .AppendLine("</time></p>");
        sb.Append("<p class=\"counts\">")
          .Append("<span class=\"total\">Total: ").Append(list.Count).Append("</span> ")
          .Append("<span class=\"passed\">Passed: ").Append(passed).Append("</span> ")
          .Append("<span class=\"failed\">Failed: ").Append(failed).Append("</span>")
          .AppendLine("</p>");
        sb.AppendLine("</section>");

        if (list.Count == 0)
        {
            sb.AppendLine("<p class=\"empty\">No run results to display.</p>");
        }
        else
        {
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>Run At</th><th>Scenario</th><th>Assertion</th><th>Source</th><th>Follow-up</th><th>Result</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");
            foreach (var r in list)
            {
                TypedVerificationEvidence? typed = null;
                PairQualitySummary? pairQuality = null;
                if (evidenceByExecutionId is not null
                    && evidenceByExecutionId.TryGetValue(r.Id, out var ev))
                {
                    typed = ev?.TypedVerification;
                    pairQuality = HasPairQuality(ev?.PairQuality) ? ev!.PairQuality : null;
                }
                RenderRow(sb, r, typed, pairQuality);
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void RenderRow(
        StringBuilder sb,
        SystemMtResultRecord r,
        TypedVerificationEvidence? typed = null,
        PairQualitySummary? pairQuality = null)
    {
        var rowClass = r.Passed ? "row-pass" : "row-fail";
        sb.Append("<tr class=\"").Append(rowClass).AppendLine("\">");
        sb.Append("<td>").Append(Esc(r.RunAt.ToString("u", Inv))).AppendLine("</td>");
        sb.Append("<td>").Append(Esc(r.MrName)).AppendLine("</td>");
        sb.Append("<td>").Append(Esc(r.AssertionName)).Append(" on <code>").Append(Esc(r.ValueName)).AppendLine("</code></td>");
        sb.Append("<td>").Append(Esc(r.SourceValue.ToString("G", Inv))).AppendLine("</td>");
        sb.Append("<td>").Append(Esc(r.FollowUpValue.ToString("G", Inv))).AppendLine("</td>");
        sb.Append("<td><span class=\"badge ")
          .Append(r.Passed ? "badge-pass" : "badge-fail")
          .Append("\">")
          .Append(r.Passed ? "PASS" : "FAIL")
          .AppendLine("</span></td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("<tr class=\"detail\"><td colspan=\"6\">");
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Details</summary>");
        sb.AppendLine("<dl>");
        AppendField(sb, "Source case", r.SourceCaseName);
        AppendField(sb, "Follow-up case", r.FollowUpCaseName);
        AppendField(sb, "Source exit code", r.SourceExitCode.ToString(Inv));
        AppendField(sb, "Follow-up exit code", r.FollowUpExitCode.ToString(Inv));
        AppendField(sb, "Source elapsed", r.SourceElapsed.ToString());
        AppendField(sb, "Follow-up elapsed", r.FollowUpElapsed.ToString());
        if (!r.Passed && !string.IsNullOrEmpty(r.FailureReason))
        {
            AppendField(sb, "Failure reason", r.FailureReason);
        }
        if (r.TransformationName is not null)
        {
            AppendField(sb, "Transformation", r.TransformationName);
            if (r.TransformationParameters is { Count: > 0 })
            {
                sb.Append("<dt>Transformation parameters</dt><dd>")
                  .Append(FormatStringDict(r.TransformationParameters))
                  .AppendLine("</dd>");
            }
            if (r.InputGenerationSucceeded.HasValue)
            {
                AppendField(sb, "Input generation", r.InputGenerationSucceeded.Value ? "succeeded" : "failed");
            }
            if (!string.IsNullOrEmpty(r.InputGenerationLog))
            {
                AppendField(sb, "Input generation log", r.InputGenerationLog!);
            }
        }
        if (r.SourceMetrics.Count > 0)
        {
            sb.Append("<dt>Source metrics</dt><dd>")
              .Append(FormatNumericDict(r.SourceMetrics))
              .AppendLine("</dd>");
        }
        if (r.FollowUpMetrics.Count > 0)
        {
            sb.Append("<dt>Follow-up metrics</dt><dd>")
              .Append(FormatNumericDict(r.FollowUpMetrics))
              .AppendLine("</dd>");
        }
        if (typed is not null)
        {
            AppendTypedVerification(sb, typed);
        }
        if (pairQuality is not null)
        {
            AppendPairQuality(sb, pairQuality);
        }
        sb.AppendLine("</dl>");
        sb.AppendLine("</details>");
        sb.AppendLine("</td></tr>");
    }

    private static void AppendPairQuality(StringBuilder sb, PairQualitySummary pairQuality)
    {
        sb.AppendLine("<dt>Pair quality</dt><dd>");
        sb.AppendLine("<table class=\"pair-quality\">");
        sb.AppendLine("<thead><tr><th>planned_pairs</th><th>executed_pairs</th><th>valid_pairs</th><th>passed_pairs</th><th>failed_pairs</th><th>skipped_pairs</th><th>invalid_spec_pairs</th><th>pass_rate_valid</th><th>pass_rate_all</th></tr></thead>");
        sb.Append("<tbody><tr>")
          .Append("<td>").Append(pairQuality.PlannedPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.ExecutedPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.ValidPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.PassedPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.FailedPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.SkippedPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.InvalidSpecPairs.ToString(Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.PassRateValid.ToString("P1", Inv)).Append("</td>")
          .Append("<td>").Append(pairQuality.PassRateAll.ToString("P1", Inv)).Append("</td>")
          .AppendLine("</tr></tbody>");
        sb.AppendLine("</table>");
        AppendReasonDistribution(sb, "Skip reasons", pairQuality.SkipReasons);
        AppendReasonDistribution(sb, "Invalid spec reasons", pairQuality.InvalidSpecReasons);
        sb.AppendLine("</dd>");
    }

    private static void AppendReasonDistribution(
        StringBuilder sb,
        string title,
        IReadOnlyList<PairQualityReasonCount>? reasons)
    {
        if (reasons is not { Count: > 0 })
        {
            return;
        }

        sb.Append("<p class=\"pair-quality-reasons\"><strong>")
          .Append(Esc(title))
          .AppendLine(":</strong></p>");
        sb.AppendLine("<ul class=\"pair-quality-reasons\">");
        foreach (var reason in reasons)
        {
            sb.Append("<li><span class=\"reason-status\">")
              .Append(Esc(reason.Status))
              .Append("</span>: ")
              .Append(Esc(reason.Reason))
              .Append(" — count: ")
              .Append(reason.Count.ToString(Inv))
              .AppendLine("</li>");
        }
        sb.AppendLine("</ul>");
    }

    private static bool HasPairQuality(PairQualitySummary? pairQuality)
    {
        if (pairQuality is null)
        {
            return false;
        }

        return pairQuality.PlannedPairs != 0
            || pairQuality.ExecutedPairs != 0
            || pairQuality.ValidPairs != 0
            || pairQuality.PassedPairs != 0
            || pairQuality.FailedPairs != 0
            || pairQuality.SkippedPairs != 0
            || pairQuality.InvalidSpecPairs != 0
            || pairQuality.PassRateValid != 0.0
            || pairQuality.PassRateAll != 0.0
            || pairQuality.SkipReasons.Count != 0
            || pairQuality.InvalidSpecReasons.Count != 0;
    }

    private static void AppendTypedVerification(StringBuilder sb, TypedVerificationEvidence typed)
    {
        if (!string.IsNullOrEmpty(typed.SpecId))
        {
            AppendField(sb, "Spec ID", typed.SpecId);
        }
        if (!string.IsNullOrEmpty(typed.SpecKind))
        {
            AppendField(sb, "Spec kind", typed.SpecKind);
        }
        if (!string.IsNullOrEmpty(typed.PredicateId))
        {
            AppendField(sb, "Predicate", $"{typed.PredicateId} ({typed.PredicateKind})");
        }
        if (!string.IsNullOrEmpty(typed.Status))
        {
            sb.Append("<dt>Typed status</dt><dd><span class=\"badge typed-status-")
              .Append(Esc(typed.Status.ToLowerInvariant()))
              .Append("\">")
              .Append(Esc(typed.Status))
              .AppendLine("</span></dd>");
        }
        if (typed.Diagnostic is { } d)
        {
            AppendField(sb, "Expected", d.Expected.ToString("G", Inv));
            AppendField(sb, "Actual", d.Actual.ToString("G", Inv));
            AppendField(sb, "Residual", d.Residual.ToString("G", Inv));
            AppendField(sb, "Tolerance", d.Tolerance.ToString("G", Inv));
        }
        if (!string.IsNullOrEmpty(typed.SkipOrInvalidReason))
        {
            AppendField(sb, "Skip reason", typed.SkipOrInvalidReason!);
        }
        if (typed.PropertyPredicates is { Count: > 0 })
        {
            sb.AppendLine("<dt>Property predicates</dt><dd>");
            sb.AppendLine("<ol class=\"property-predicates\">");
            foreach (var p in typed.PropertyPredicates)
            {
                sb.Append("<li><code>").Append(Esc(p.PredicateId)).Append("</code> (")
                  .Append(Esc(p.PredicateKind)).Append(") - status: <span class=\"badge typed-status-")
                  .Append(Esc((p.Status ?? string.Empty).ToLowerInvariant()))
                  .Append("\">").Append(Esc(p.Status ?? string.Empty)).Append("</span>");
                if (p.Residual.HasValue)
                {
                    sb.Append("; residual=<code>")
                      .Append(Esc(p.Residual.Value.ToString("G", Inv)))
                      .Append("</code>");
                }
                if (p.Tolerance.HasValue)
                {
                    sb.Append("; tolerance=<code>")
                      .Append(Esc(p.Tolerance.Value.ToString("G", Inv)))
                      .Append("</code>");
                }
                if (!string.IsNullOrEmpty(p.ExpectedJson))
                {
                    sb.Append("; expected=<code>").Append(Esc(p.ExpectedJson!)).Append("</code>");
                }
                if (!string.IsNullOrEmpty(p.ActualJson))
                {
                    sb.Append("; actual=<code>").Append(Esc(p.ActualJson!)).Append("</code>");
                }
                if (!string.IsNullOrEmpty(p.Reason))
                {
                    sb.Append("; reason: ").Append(Esc(p.Reason!));
                }
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ol>");
            sb.AppendLine("</dd>");
        }
    }

    private static void AppendField(StringBuilder sb, string label, string value)
    {
        sb.Append("<dt>").Append(Esc(label)).Append("</dt><dd>").Append(Esc(value)).AppendLine("</dd>");
    }

    private static string FormatStringDict(IReadOnlyDictionary<string, string> dict)
    {
        var inner = string.Join(", ", dict.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"<code>{Esc(kv.Key)}</code>=<code>{Esc(kv.Value)}</code>"));
        return inner;
    }

    private static string FormatNumericDict(IReadOnlyDictionary<string, double> dict)
    {
        var inner = string.Join(", ", dict.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"<code>{Esc(kv.Key)}</code>=<code>{Esc(kv.Value.ToString("G", Inv))}</code>"));
        return inner;
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static string InlineStyle() => """
<style>
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
       margin: 2rem auto; max-width: 1100px; color: #1f2328; }
h1 { font-size: 1.6rem; margin-bottom: 0.4rem; }
.summary { margin-bottom: 1rem; padding: 0.6rem 0.9rem; background: #f6f8fa;
           border: 1px solid #d0d7de; border-radius: 6px; }
.meta { margin: 0; font-size: 0.85rem; color: #57606a; }
.counts { margin: 0.3rem 0 0 0; font-weight: 600; }
.counts .passed { color: #1a7f37; }
.counts .failed { color: #cf222e; }
.empty { padding: 1rem; background: #f6f8fa; border-radius: 6px; color: #57606a; }
table { width: 100%; border-collapse: collapse; }
thead th { text-align: left; padding: 0.4rem 0.6rem; border-bottom: 2px solid #d0d7de; }
tbody td { padding: 0.4rem 0.6rem; border-bottom: 1px solid #eaeef2; }
tr.row-pass { background: #ffffff; }
tr.row-fail { background: #fff8f8; }
tr.detail td { padding: 0; border-bottom: 1px solid #d0d7de; }
tr.detail dl { margin: 0.3rem 0.6rem 0.6rem 1.2rem; display: grid;
               grid-template-columns: max-content 1fr; gap: 0.2rem 0.8rem; }
tr.detail dt { font-weight: 600; color: #57606a; }
tr.detail dd { margin: 0; }
.badge { display: inline-block; padding: 0.1rem 0.5rem; border-radius: 999px;
         font-size: 0.75rem; font-weight: 700; letter-spacing: 0.04em; }
.badge-pass { background: #dcfce7; color: #166534; }
.badge-fail { background: #fee2e2; color: #991b1b; }
code { font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
       font-size: 0.85rem; background: #eaeef2; padding: 0 0.25rem; border-radius: 3px; }
</style>
""";
}
