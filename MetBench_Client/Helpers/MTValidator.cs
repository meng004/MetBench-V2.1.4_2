using FluentValidation;
using MetBench_BLL;
using System.Text.RegularExpressions;

namespace MetBench_Client.Helpers
{

    public class MTValidator : AbstractValidator<MTParam>
    {

        public MTValidator()
        {
            RuleFor(t => t.InputPatternSympy).NotEmpty().WithMessage("InputPatternSympy不能为空！");
            RuleFor(t => t.OutputPatternSympy).NotEmpty().WithMessage("OutputPatternSympy不能为空！");
            RuleFor(t => t.MinParam).NotEmpty().WithMessage("请填写MinNumber");
            RuleFor(t => t.MaxParam).NotEmpty().WithMessage("请填写MaxNumber");
            RuleFor(t => t.ExecutNumber).NotEmpty().WithMessage("请填写ExecutNumber");
            RuleFor(t => t.Threshold).NotEmpty().WithMessage("Threshold不能为空！");
            // 添加条件，当 MinParam 和 MaxParam 非空时才执行比较
            When(t => !string.IsNullOrEmpty(t.MinParam) && !string.IsNullOrEmpty(t.MaxParam)&&IsRealNumber(t.MinParam)&&IsRealNumber(t.MaxParam), () =>
            {
                RuleFor(t => double.Parse(t.MinParam))
                    .LessThanOrEqualTo(t => double.Parse(t.MaxParam))
                    .WithMessage("MaxNumber不能小于MinNumber");
            });
            // 添加条件，当 ExecutNumber 空时才执行判断
            When(t => !string.IsNullOrEmpty(t.ExecutNumber), () =>
            {
                RuleFor(t => t.ExecutNumber).Must(BePositiveInteger).WithMessage("ExecuteNumber应为正整数");
            });
            //添加条件，当 MinParam 不为空时才执行判断
            When(t => !string.IsNullOrEmpty(t.MinParam), () =>
            {
                RuleFor(t => t.MinParam).Must(IsRealNumber).WithMessage("MinParam应为实数");
            });
            // 添加条件，当 MaxParam 不为空时才执行判断
            When(t => !string.IsNullOrEmpty(t.MaxParam), () =>
            {
                RuleFor(t => t.MaxParam).Must(IsRealNumber).WithMessage("MaxParam应为实数");
            });
            // 添加条件，当 Threshold 不为空时才执行判断
            When(t => !string.IsNullOrEmpty(t.Threshold), () =>
            {
                RuleFor(t => t.Threshold).Must(IsValidThreshold).WithMessage("请填写正确格式的Threshold");
            });
        }
        // 自定义验证方法，验证是否为正整数
        private bool BePositiveInteger(string value)
        {
            // 检查是否为空或不能成功转换为整数
            return !string.IsNullOrEmpty(value) && int.TryParse(value, out int result) && result > 0;
        }
        private bool IsRealNumber(string input)
        {

            // 如果字符串中的每个字符都是数字，或者包含正负号或小数点，则认为是实数 
            return !string.IsNullOrEmpty(input) && double.TryParse(input, out double result);

        }
        //验证阈值
        public bool IsValidThreshold(string threshold)
        {
            // 使用正则表达式检查字符串是否匹配指定的模式
            if (Regex.IsMatch(threshold, @"^[+-]?\d*\.?\d+([eE][-+]?\d+)?$"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
