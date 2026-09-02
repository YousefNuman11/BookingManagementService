using BookingManagement.Domain.Enums;

namespace BookingManagement.Domain.Entities
{
    public class Booking
    {
        public Guid Id{ get; private set; }
        public string ResourceId { get; private set; } = null!;
        public string UserId { get; private set; } = null!;
        public DateTime StartDateTime{ get; private set; }
        public DateTime EndDateTime { get; private set; }
        public BookingStatus Status{ get; private set; }
        public DateTime CreatedAt{ get; private set; }
        public DateTime? CancelledAt{ get; private set; }


        private Booking()
        {

        }

        public Booking(
            string resourceId,
            string userId,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            Id = Guid.NewGuid();
            ResourceId = resourceId;
            UserId = userId;
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
            Status = BookingStatus.Active;
            CreatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = BookingStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
        }
    }
}