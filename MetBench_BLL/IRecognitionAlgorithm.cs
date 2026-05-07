namespace MetBench_BLL
{
    public interface IRecognitionAlgorithm
    {
        DetectionResult Execute(TargetProgram target);
    }
}
