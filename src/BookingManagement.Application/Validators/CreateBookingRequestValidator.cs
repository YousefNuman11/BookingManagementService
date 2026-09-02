using BookingManagement.Application.DTOs;
using FluentValidation;

namespace BookingManagement.Application.Validators;

public class CreateBookingRequestValidator
    : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.EndDateTime)
            .GreaterThan(x => x.StartDateTime)
            .WithMessage(
                "End date/time must be later than start date/time.");
    }
}