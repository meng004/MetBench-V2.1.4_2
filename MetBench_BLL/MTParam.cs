using MetBench_Domain;

namespace MetBench_BLL
{
    //执行蜕变测试所需的参数类
    public class MTParam
    {
        //输入模式的Sympy格式
        public string InputPatternSympy { get; set; } = string.Empty;
        //输出模式的Sympy格式
        public string OutputPatternSympy { get; set; } = string.Empty;
        //最小值
        public string MinParam { get; set; } 
        //最大值
        public string MaxParam { get; set; } 
        //执行次数
        public string ExecutNumber { get; set; }
        //阈值
        public string Threshold {  get; set; }

        //MR
        public MetamorphicRelation relation { get; set; }
        //Applictaion
        public Application application { get; set; }    
    }

}
