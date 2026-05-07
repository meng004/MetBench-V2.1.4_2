namespace MetBench_BLL
{
    //执行蜕变关系识别所需的参数类
    public class MRDetectorCollection
    {
        //目标函数
        public string ApplicationName { get; set; } = string.Empty;
        //输入模式的Latex格式
        public string InputPattern { get; set; } = string.Empty;
        //输出模式的Latex格式
        public string OutputPattern { get; set; } 
        //输入维度
        public string DimensionOfInputPattern { get; set; } 
        //通过率
        public string TrueRatio { get; set; }

        // 输入模式渲染图片路径
        public string InputPatternimagepath { get; set; }
        // 输出模式渲染图片路径
        public string OutputPatternimagepath { get; set; }
    }

}
