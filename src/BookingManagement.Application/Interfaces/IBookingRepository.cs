using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;

namespace BookingManagement.Application.Interfaces
{
    public interface IBookingRepository
    {
        Task<bool> TryAddBookingAsync(
            Booking booking,
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

        Task UpdateAsync(
            Booking booking,
            CancellationToken cancellationToken);
    }
}