using FluentValidation;
using ShiftSoftware.ShiftEntity.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Tests
{
    public class Sample : ShiftEntityDTOBase
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public int Revisions { get; set; }
    }

    public class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("You must enter Sample Name")
                .MaximumLength(50).WithMessage("Sample Name cannot be longer than 50 characters");

            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("You must enter Sample LastName")
                .MaximumLength(250).WithMessage("Sample LastName cannot be longer than 250 characters");
        }
    }
}
