using BookingManagement.Application.Interfaces;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;
using BookingManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingManagement.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly BookingDbContext _context;

        public BookingRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TryAddBookingAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                // Serialize booking attempts for the same resource.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                   $"""
                            DECLARE @Result INT;

                            EXEC @Result = sp_getapplock
                                @Resource = {booking.ResourceId},
                                @LockMode = 'Exclusive',
                                @LockOwner = 'Transaction',
                                @LockTimeout = 5000;

                            IF @Result < 0
                                THROW 50001, 'Could not acquire booking lock.', 1;
                    """,
                    cancellationToken);

                var hasOverlap = await _context.Bookings
                    .AnyAsync(
                        b =>
                            b.ResourceId == booking.ResourceId &&
                            b.Status == BookingStatus.Active &&
                            booking.StartDateTime < b.EndDateTime &&
                            booking.EndDateTime > b.StartDateTime,
                        cancellationToken);

                if (hasOverlap)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                await _context.Bookings.AddAsync(
                    booking,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Booking?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.Bookings
                .FirstOrDefaultAsync(
                b => b.Id == id,
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
            return await _context.Bookings
                .Where(b =>
                    b.ResourceId == resourceId &&
                    b.Status == status &&
                    b.StartDateTime < to &&
                    b.EndDateTime > from)
                .OrderBy(b => b.StartDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
