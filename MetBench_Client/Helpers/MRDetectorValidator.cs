using FluentValidation;
using MetBench_BLL;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MetBench_Client.Helpers
{
    public class MRDetectorValidator : AbstractValidator<FunctionProgram>
    {

        public MRDetectorValidator()
        {
            RuleFor(t => t.FilePath).NotEmpty().WithMessage("请上传Filename");

            RuleFor(t => t.ParameterCount)
                .NotEmpty().WithMessage("请填写InputParamCount")
                .GreaterThan(0).WithMessage("InputParamCount必须是正整数")
                .Must(BeInteger).WithMessage("InputParamCount必须是整数");

            RuleFor(t => t.ParameterTypes).NotEmpty().WithMessage("请填写InputDatatypes");

            RuleFor(t => t.ParameterRanges).NotEmpty().WithMessage("请填写InputParamRanges");
        }

        private bool BeInteger(int value) => value == (int)value;

    }
}
