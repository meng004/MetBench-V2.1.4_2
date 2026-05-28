namespace MetBench_BLL.SystemMT.Catalog.Editing;

/// <summary>
/// One sample-case file under <c>SUT/&lt;sutId&gt;/sample/</c>. Returned by
/// <see cref="ISystemMtSampleCaseEditor.ListSamples"/>; the raw JSON body is
/// fetched on demand through <see cref="ISystemMtSampleCaseEditor.LoadSample"/>.
/// </summary>
public sealed record SystemMtSampleCaseDescriptor(
    string SutId,
    string FileName,
    string AbsolutePath);
