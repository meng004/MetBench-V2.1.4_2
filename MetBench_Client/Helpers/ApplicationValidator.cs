using FluentValidation;
using MetBench_BLL;
using MetBench_Domain;
using System;
using System.Linq;

namespace MetBench_Client.Helpers
{
    public class ApplicationValidator : AbstractValidator<Application>
    {
        public ApplicationValidator()
        {
            RuleFor(t => t.Name).NotEmpty().WithMessage("请填写Name");
            //RuleFor(t => t.Name).Must(IsRepeatedName).WithMessage("填写的Name已存在");
            RuleFor(t => t.Code).NotNull().WithMessage("请上传SoftwareUnderTest");
            RuleFor(t => t.CodeName).NotEmpty().WithMessage("请上传SoftwareUnderTest");
            RuleFor(t => t.ProgrammingLanguage).NotEmpty().WithMessage("请填写ProgrammingLanguage");
            When(t => !string.IsNullOrEmpty(t.ProgrammingLanguage), () =>
            {
                RuleFor(t => t.ProgrammingLanguage).Must(IsValidProgrammingLanguage).WithMessage("ProgrammingLanguage应为“Python”、“Java”、“C”");
            });
            // 添加条件，当 DimensionOfInputPattern 非空时才执行判断数据必须为正整数
            RuleFor(t => t.LinesOfCode).NotEmpty().WithMessage("请填写LinesOfCode");
            RuleFor(t => t.LinesOfCode).NotEmpty().Must(IsValidPositiveInteger).WithMessage("LinesOfCode应为正整数");
        }

        // 检查是否为一个正数
        private bool IsValidPositiveInteger(int input)
        {
            return input > 0;
        }

        // 检查ProgrammingLanguage是否为规定语言
        private bool IsValidProgrammingLanguage(string input)
        {
            //return input == "java" || input == "python" || input == "c";
            string[] validExtensions = { "c", "java", "python" };

            string fileExtension = input;

            return validExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
