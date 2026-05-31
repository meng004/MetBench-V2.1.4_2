# Mutation Campaign Page XamlParseException Debug Record

> Date: 2026-05-31
> Scope: WPF navigation from Candidate Review to the next page.

## Symptom

When navigating to the page after Candidate Review, the WPF client shows:

- `XamlParseException`
- Failing property: `System.Windows.Documents.Run.Text`
- XAML location: line 172, position 26
- Crash log path reported by VM: `C:\Users\codex\AppData\Local\Temp\MetBench_crash.log`

Navigation order in `MainWindowViewModel` shows the page after Candidate Review is
`MutationCampaignPage`.

## Root Cause Evidence

`MetBench_Client/Views/Pages/MutationCampaignPage.xaml` line 172 was the first
of five summary label bindings that placed localized indexer bindings directly
on `Run.Text`:

- `Mutation_SummaryTotal`
- `Mutation_SummaryDetected`
- `Mutation_SummaryMissed`
- `Mutation_SummaryErrors`
- `Mutation_SummaryDetectionRate`

Other localized page chrome uses `TextBlock.Text`. The problematic pattern was
unique to the Mutation Campaign summary block; the only other `Run.Text`
bindings in the client are simple numeric bindings in `PagingBar`.

## Fix

Replaced the summary inline `Run` elements with horizontal `StackPanel` groups
containing regular `TextBlock.Text` bindings. This preserves the same visible
label/value layout while avoiding the WPF XAML load failure on `Run.Text`.

Changed file:

- `MetBench_Client/Views/Pages/MutationCampaignPage.xaml`

Added regression guard:

- `MetBench_SystemMT.Tests/ClientI18n/MutationCampaignPageXamlTests.cs`

## Verification

Local static verification:

- `rg "<Run Text=\"{Binding ViewModel.Localization[Mutation_Summary" MetBench_Client/Views/Pages/MutationCampaignPage.xaml` returns no matches after the fix.

Local limitation:

- Focused `dotnet test MetBench_SystemMT.Tests/MetBench_SystemMT.Tests.csproj --filter "FullyQualifiedName~ClientI18n.MutationCampaignPageXamlTests" --no-restore -v minimal --blame-hang-timeout 60s` hung before producing test output in the local environment and was terminated. This is not counted as a passing test.

Required VM follow-up:

1. Build `MetBench.sln` on Windows.
2. Launch the WPF client.
3. Navigate `Candidate Review -> Mutation Campaign`.
4. Confirm the page loads without `XamlParseException`.
5. Capture a screenshot and, if available, attach the updated `MetBench_crash.log` absence/unchanged evidence.
