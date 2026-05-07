namespace MetBench_BLL
{
    // 具体的函数程序子类
    public class FunctionProgram : TargetProgram
    {
        // 属性
        public string FilePath { get; private set; }
        //目标函数 如sin.py
        public string FunctionName { get; private set; }
        //输入参数个数 如 1
        public int ParameterCount { get; private set; }
        //输入参数范围 如(-10,10) ((-10,10),(20,30))
        public string ParameterRanges { get; private set; }
        //输入参数类型 如float (int,float)
        public string ParameterTypes { get; private set; }
        //程序的源码

        // 构造函数
        public FunctionProgram(ProgramLanguage language,
                             string filePath,
                             string functionName,
                             int parameterCount,
                             string parameterRanges,
                             string parameterTypes)
            : base(language)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
            ParameterCount = parameterCount;
            ParameterRanges = parameterRanges ?? throw new ArgumentNullException(nameof(parameterRanges));
            ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
        }

        // 实现抽象成员
        public override string ProgramType => "Function";

        public override object GetProgramData()
        {
            return new
            {
                Language = GetLanguageName(),
                FilePath,
                FunctionName,
                ParameterCount,
                ParameterRanges,
                ParameterTypes
            };
        }

        // 重写验证方法
        public override void Validate()
        {
            base.Validate();

            if (ParameterCount < 0)
                throw new ArgumentException("Parameter count cannot be negative");

            if (string.IsNullOrWhiteSpace(ParameterRanges))
                throw new ArgumentException("Parameter ranges cannot be empty");

            if (string.IsNullOrWhiteSpace(ParameterTypes))
                throw new ArgumentException("Parameter types cannot be empty");

            // 语言特定验证
            ValidateForLanguage();
        }

        // 语言特定的验证逻辑
        private void ValidateForLanguage()
        {
            switch (Language)
            {
                case ProgramLanguage.Python:
                    // Python特有的验证逻辑
                    break;

                case ProgramLanguage.Java:
                    // Java特有的验证逻辑
                    if (!FunctionName[0].ToString().Equals(FunctionName[0].ToString().ToUpper()))
                        Console.WriteLine("Warning: Java methods usually start with lowercase");
                    break;

                case ProgramLanguage.C:
                    // C特有的验证逻辑
                    break;

                case ProgramLanguage.CSharp:
                    // C#特有的验证逻辑
                    break;
            }
        }
    }
}
