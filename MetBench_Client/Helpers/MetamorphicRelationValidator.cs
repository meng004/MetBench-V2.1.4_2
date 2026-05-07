 using FluentValidation;
using MetBench_BLL;
using MetBench_Domain;
using System.Linq;
using System.Text.RegularExpressions;

namespace MetBench_Client.Helpers
{
    public class MetamorphicRelationValidator : AbstractValidator<MetamorphicRelation>
    {

        public MetamorphicRelationValidator()
        {
            RuleFor(t => t.InputPattern).NotEmpty().WithMessage("请填写InputPattern");
            RuleFor(t => t.OutputPattern).NotEmpty().WithMessage("请填写OutputPattern");
            RuleFor(t => t.DimensionOfInputPattern).NotEmpty().WithMessage("请填写DimensionOfInputPattern");
            RuleFor(t => t.DimensionOfOutputPattern).NotEmpty().WithMessage("请填写DimensionOfOutputPattern");
            RuleFor(t => t.ApplicationName).NotEqual(string.Empty).WithMessage("请填写ApplicationName");
            RuleFor(t => t.OrderOfMR).NotEqual("全部关系").WithMessage("请选择OrderOfMR");
            RuleFor(t => t.Granularity).NotNull().NotEmpty().WithMessage("请选择Granularity");
            RuleFor(t => t.Hierarchy).NotNull().NotEmpty().WithMessage("请选择Hierarchy");
            RuleFor(t => t.Operator).NotNull().NotEmpty().WithMessage("请选择Operator");
            RuleFor(t => t.Expression).NotNull().NotEmpty().WithMessage("请选择Expression");
            //// 添加条件，当 InputPattern 非空时才执行判断latex是否合法
            When(t => !string.IsNullOrEmpty(t.InputPattern), () =>
            {
                RuleFor(t => t.InputPattern).Must(IsValidLatexCharacters).WithMessage("请填写标准LaTeX格式的InputPattern");
            });

            // 添加条件，当 OutputPattern 非空时才执行判断latex是否合法
            When(t => !string.IsNullOrEmpty(t.OutputPattern), () =>
            {
                RuleFor(t => t.OutputPattern).Must(IsValidLatexCharacters).WithMessage("请填写标准LaTeX格式的OutputPattern");
            });

            // 添加条件，当 DimensionOfInputPattern 非空时才执行判断数据必须为正整数
            When(t => !string.IsNullOrEmpty(t.DimensionOfInputPattern), () =>
            {
                RuleFor(t => t.DimensionOfInputPattern).Must(IsValidPositiveInteger).WithMessage("DimensionOfInputPattern应为正整数");
            });

            // 添加条件，当 DimensionOfOutputPattern 非空时才执行判断数据必须为正整数
            When(t => !string.IsNullOrEmpty(t.DimensionOfOutputPattern), () =>
            {
                RuleFor(t => t.DimensionOfOutputPattern).Must(IsValidPositiveInteger).WithMessage("DimensionOfOutputPattern应为正整数");
            });
        }

        // 正确的Latex形式的规范
        private bool IsValidLatexCharacters(string input)
        {
            // 检查是否包含非法字符
            string invalidCharactersPattern = @"[^a-zA-Z0-9\\{}\^_+\-*\/.,()=<>[\]\s]";

            //检查是否有相邻的反斜杠，因为 LaTeX 中通常是一个命令
            string adjacentBackslashPattern = @"\\{2,}";

            //检查是否有相邻的下划线，因为 LaTeX 中通常是一个命令
            string adjacentUnderscorePattern = @"_{2,}";

            //检查逗号后是否有空格
            string commaWithoutSpacePattern = @",(?!\s)";
            //检查连续的井号
            string consecutiveHashPattern = @"#{2,}";

            //使用正则表达式匹配输入字符串
            if (Regex.IsMatch(input, invalidCharactersPattern) ||
                Regex.IsMatch(input, adjacentBackslashPattern) ||
                Regex.IsMatch(input, adjacentUnderscorePattern)||
                Regex.IsMatch(input, commaWithoutSpacePattern) ||
                Regex.IsMatch(input, consecutiveHashPattern))
            {
                return false;
            }

            return true;
        }

        // 正数的规范
        private bool IsValidPositiveInteger(string input)
        {
            return int.TryParse(input, out int result) && result > 0;
        }
    }
}
