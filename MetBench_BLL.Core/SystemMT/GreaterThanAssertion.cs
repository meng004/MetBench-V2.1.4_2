namespace MetBench_BLL.SystemMT;

public sealed class GreaterThanAssertion : IMrAssertion
{
    public string Name => "GreaterThan";

    public SystemMtAssertionResult Evaluate(string valueName, ParsedOutput source, ParsedOutput followUp)
    {
        if (!source.Values.TryGetValue(valueName, out var sourceValue))
        {
            return new SystemMtAssertionResult(
                Name,
                valueName,
                double.NaN,
                double.NaN,
                false,
                $"Assertion failure: Missing parsed value '{valueName}' in source output");
        }

        if (!followUp.Values.TryGetValue(valueName, out var followUpValue))
        {
            return new SystemMtAssertionResult(
                Name,
                valueName,
                sourceValue,
                double.NaN,
                false,
                $"Assertion failure: Missing parsed value '{valueName}' in follow-up output");
        }

        var passed = followUpValue > sourceValue;
        return new SystemMtAssertionResult(
            Name,
            valueName,
            sourceValue,
            followUpValue,
            passed,
            passed
                ? string.Empty
                : $"Assertion failure: Expected follow-up value {followUpValue} to be greater than source value {sourceValue} for '{valueName}'");
    }
}
