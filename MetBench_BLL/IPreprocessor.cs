using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    // 预处理
    public interface IPreprocessor
    {
        // 根据目标程序的编程语言、方法签名筛选候选程序，并且对目标程序进行去除注释和空白行
        //Dictionary<TargetProgram,ObservableCollection<Application>> Process(TargetProgram targetProgram);
        // 预处理核心函数
        Dictionary<string,object> Process(TargetProgram targetProgram,ObservableCollection<Application> candidateProgrms);
        // 对目标程序去除注释和空白行等无关信息
        TargetProgram ProcessTargetProgram(TargetProgram targetProgram);
        // 根据目标程序的编程语言、方法签名筛选候选程序
        ObservableCollection<Application> ProcessCandidateProgram(TargetProgram targetProgram,ObservableCollection<Application> candidateProgrms);
    }
}
