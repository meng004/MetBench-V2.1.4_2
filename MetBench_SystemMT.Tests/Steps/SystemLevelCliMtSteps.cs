using Reqnroll;

namespace MetBench_SystemMT.Tests.Steps;

[Binding]
public sealed class SystemLevelCliMtSteps
{
    [Given("a system MT case named {string} with input file {string}")]
    public void GivenASystemMtCaseNamedWithInputFile(string caseName, string inputFile)
    {
        throw new NotImplementedException($"Case binding is not implemented: {caseName}, {inputFile}");
    }

    [When("I run both cases with program profile {string}")]
    public void WhenIRunBothCasesWithProgramProfile(string profileName)
    {
        throw new NotImplementedException($"Program profile is not implemented: {profileName}");
    }

    [Then("the parsed output value {string} of {string} should be greater than {string}")]
    public void ThenTheParsedOutputValueOfShouldBeGreaterThan(
        string valueName,
        string followUpCaseName,
        string sourceCaseName)
    {
        throw new NotImplementedException(
            $"Assertion is not implemented: {valueName}, {followUpCaseName}, {sourceCaseName}");
    }
}
