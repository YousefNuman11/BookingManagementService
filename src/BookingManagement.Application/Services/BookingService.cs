using BookingManagement.Application.Exceptions;
using BookingManagement.Application.Interfaces;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;

namespace BookingManagement.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<Booking> CreateBookingAsync(
            string resourceId,
            string userId,
            DateTime startDateTime,
            DateTime endDateTime,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new InvalidBookingException(
                    "ResourceId is required.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidBookingException(
                    "UserId is required.");
            }

            if (startDateTime >= endDateTime)
            {
                throw new InvalidBookingException(
                    "Start date/time must be earlier than end date/time.");
            }

            var booking = new Booking(
                resourceId,
                userId,
                startDateTime,
                endDateTime);

            var created = await _bookingRepository.TryAddBookingAsync(
                booking,
                cancellationToken);

            if (!created)
            {
                throw new BookingConflictException(
                    "The resource is already booked for the requested time.");
            }

            return booking;
        }

        public async Task<Booking?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _bookingRepository.GetByIdAsync(
                id,
                cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetBookingsAsync(
            string resourceId,
            DateTime from,
            DateTime to,
            BookingStatus status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new InvalidBookingException(
                    "ResourceId is required.");
            }

            if (from >= to)
            {
                throw new InvalidBookingException(
                    "From must be earlier than To.");
            }

            if (page < 1)
            {
                throw new InvalidBookingException(
                    "Page must be greater than or equal to 1.");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new InvalidBookingException(
                    "PageSize must be between 1 and 100.");
            }

            return await _bookingRepository.GetBookingsAsync(
                resourceId,
                from,
                to,
                status,
                page,
                pageSize,
                cancellationToken);
        }

        public async Task CancelBookingAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(
                id,
                cancellationToken);

            if (booking is null)
            {
                throw new BookingNotFoundException(
                    $"Booking with ID '{id}' was not found.");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                throw new BookingConflictException(
                    "The booking has already been cancelled.");
            }

            booking.Cancel();

            await _bookingRepository.UpdateAsync(
                booking,
                cancellationToken);
        }
    }
}