namespace BookingManagement.Application.DTOs
{
    public record CreateBookingRequest(
        string ResourceId,
        string UserId,
        DateTime StartDateTime,
        DateTime EndDateTime);
}