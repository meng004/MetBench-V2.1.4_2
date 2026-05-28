namespace MetBench_BLL.SystemMT.Metadata.Editing;

/// <summary>
/// One row in the merged equation list returned by
/// <see cref="ISystemMtEquationEditor.ListEquations"/>. <see cref="Source"/>
/// distinguishes seed (<c>Built-in</c>) from user-authored (<c>User</c>) rows
/// so the WPF surface can gate edit / delete controls.
/// </summary>
public sealed record EquationListItem(
    string EquationKey,
    string Name,
    string CanonicalForm,
    string Source);

public static class EquationSourceKinds
{
    public const string BuiltIn = "Built-in";
    public const string User = "User";
}
