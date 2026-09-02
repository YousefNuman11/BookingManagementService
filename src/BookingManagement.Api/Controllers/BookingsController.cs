using BookingManagement.Application.DTOs;
using BookingManagement.Application.Exceptions;
using BookingManagement.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        private readonly IValidator<CreateBookingRequest> _validator;

        public BookingsController(
            IBookingService bookingService,
            IValidator<CreateBookingRequest> validator)
        {
            _bookingService = bookingService;
            _validator = validator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            CreateBookingRequest request,
            CancellationToken cancellationToken)
        {

            var validationResult = await _validator.ValidateAsync(
                request,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                var problemDetails = new ValidationProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                };

                foreach (var error in validationResult.Errors)
                {
                    if (!problemDetails.Errors.ContainsKey(error.PropertyName))
                    {
                        problemDetails.Errors[error.PropertyName] = new string[] { };
                    }

                    var errorList = problemDetails.Errors[error.PropertyName].ToList();
                    errorList.Add(error.ErrorMessage);
                    problemDetails.Errors[error.PropertyName] = errorList.ToArray();
                }

                return BadRequest(problemDetails);
            }

            var booking = await _bookingService.CreateBookingAsync(
                request.ResourceId,
                request.UserId,
                request.StartDateTime,
                request.EndDateTime,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetBookingById),
                new { id = booking.Id },
                booking);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBookingById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var booking = await _bookingService.GetByIdAsync(
                id,
                cancellationToken);

            if(booking is null)
            {
                throw new BookingNotFoundException(
                    $"Booking with ID '{id}' was not found.");
            }

            return Ok(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings(
            [FromQuery] BookingQueryRequest request,
            CancellationToken cancellationToken)
        {
            var bookings = await _bookingService.GetBookingsAsync(
                request.ResourceId,
                request.From,
                request.To,
                request.Status,
                request.Page,
                request.PageSize,
                cancellationToken);

            return Ok(bookings);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelBooking(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _bookingService.CancelBookingAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}