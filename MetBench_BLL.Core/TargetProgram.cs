namespace MetBench_BLL
{
    // 抽象的目标程序基类
    public abstract class TargetProgram
    {
        public ProgramLanguage Language { get; protected set; }
        public abstract string ProgramType { get; }
        public abstract object GetProgramData();

        // 构造函数保护，只能通过子类实例化
        protected TargetProgram(ProgramLanguage language)
        {
            Language = language;
        }

        // 公共方法
        public virtual void Validate()
        {
            // 基础验证逻辑
            if (GetProgramData() == null)
                throw new ArgumentNullException("Program data cannot be null");
        }

        // 获取语言名称的友好显示
        public string GetLanguageName()
        {
            return Language switch
            {
                ProgramLanguage.Python => "Python",
                ProgramLanguage.Java => "Java",
                ProgramLanguage.C => "C",
                ProgramLanguage.CSharp => "C#",
                _ => "Other"
            };
        }
    }
}
