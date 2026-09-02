using BookingManagement.Domain.Enums;

namespace BookingManagement.Application.DTOs;

public record BookingQueryRequest(
    string ResourceId,
    DateTime From,
    DateTime To,
    BookingStatus Status = BookingStatus.Active,
    int Page = 1,
    int PageSize = 10);