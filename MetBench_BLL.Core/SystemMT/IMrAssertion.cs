namespace MetBench_BLL.SystemMT;

/// <summary>
/// Contract for a metamorphic-relation assertion. Implementations compare a
/// named numeric value between a source run and a follow-up run and return a
/// pass/fail result. The runner discovers assertions by <see cref="Name"/>;
/// adding a new assertion does not require changes to the runner or to any
/// Reqnroll step bindings — only DI registration of the new implementation.
/// </summary>
public interface IMrAssertion
{
    string Name { get; }

    SystemMtAssertionResult Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp);
}
