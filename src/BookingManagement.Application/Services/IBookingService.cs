using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;

namespace BookingManagement.Application.Services
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(
            string resourceId,
            string userId,
            DateTime startDateTime,
            DateTime endDateTime,
            CancellationToken cancellationToken); 

        Task<Booking?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<Booking>> GetBookingsAsync(
            string resourceId,
            DateTime from,
            DateTime to,
            BookingStatus status,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        Task CancelBookingAsync(
        Guid id,
        CancellationToken cancellationToken);
    }
}