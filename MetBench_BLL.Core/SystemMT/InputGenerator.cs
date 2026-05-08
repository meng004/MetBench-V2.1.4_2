namespace MetBench_BLL.SystemMT;

public sealed class InputGenerator
{
    private readonly PythonInputAdapter _inputAdapter;
    private readonly string _adapterPath;

    public InputGenerator(PythonInputAdapter inputAdapter, string adapterPath)
    {
        _inputAdapter = inputAdapter ?? throw new ArgumentNullException(nameof(inputAdapter));
        if (string.IsNullOrWhiteSpace(adapterPath))
        {
            throw new ArgumentException("Adapter path cannot be empty", nameof(adapterPath));
        }

        _adapterPath = adapterPath;
    }

    public async Task<InputGenerationResult> GenerateAsync(
        string sourceInputPath,
        string followUpInputPath,
        MrTransformation transformation,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = await _inputAdapter.TransformAsync(
                _adapterPath, sourceInputPath, followUpInputPath, transformation, cancellationToken);

            return new InputGenerationResult(
                sourceInputPath,
                followUpInputPath,
                transformation,
                Succeeded: true,
                Log: log,
                FailureReason: string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return new InputGenerationResult(
                sourceInputPath,
                followUpInputPath,
                transformation,
                Succeeded: false,
                Log: string.Empty,
                FailureReason: ex.Message);
        }
    }
}
