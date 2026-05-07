using FluentValidation;
using MetBench_BLL;
using MetBench_Domain;
using System.Linq;

namespace MetBench_Client.Helpers
{
    public class DomainValidator : AbstractValidator<Domain>
    {

        public DomainValidator()
        {
            RuleFor(t => t.Name).NotEmpty().WithMessage("请填写Name");
        }
    }
}
