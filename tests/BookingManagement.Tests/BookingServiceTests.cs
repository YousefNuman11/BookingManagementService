using BookingManagement.Application.Exceptions;
using BookingManagement.Application.Interfaces;
using BookingManagement.Application.Services;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BookingManagement.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();

        _service = new BookingService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingOverlaps_ShouldThrowConflictException()
    {
        // Arrange
        var resourceId = "room-101";
        var userId = "user-1";
        var start = new DateTime(2026, 9, 1, 14, 0, 0);
        var end = new DateTime(2026, 9, 1, 15, 0, 0);

        _repositoryMock
            .Setup(x => x.TryAddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateBookingAsync(
            resourceId,
            userId,
            start,
            end,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BookingConflictException>();
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBookingIsValid_ShouldCreateBooking()
    {
        // Arrange
        var resourceId = "room-101";
        var userId = "user-1";
        var start = new DateTime(2026, 9, 1, 14, 0, 0);
        var end = new DateTime(2026, 9, 1, 15, 0, 0);

        _repositoryMock
            .Setup(x => x.TryAddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateBookingAsync(
            resourceId,
            userId,
            start,
            end,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ResourceId.Should().Be(resourceId);
        result.UserId.Should().Be(userId);
        result.StartDateTime.Should().Be(start);
        result.EndDateTime.Should().Be(end);

        _repositoryMock.Verify(
            x => x.TryAddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenStartIsAfterEnd_ShouldThrowInvalidBookingException()
    {
        // Arrange
        var start = new DateTime(2026, 9, 1, 15, 0, 0);
        var end = new DateTime(2026, 9, 1, 14, 0, 0);

        // Act
        var act = () => _service.CreateBookingAsync(
            "room-101",
            "user-1",
            start,
            end,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidBookingException>();

        _repositoryMock.Verify(
            x => x.TryAddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ShouldReturnBooking()
    {
        // Arrange
        var booking = new Booking(
            "room-101",
            "user-1",
            new DateTime(2026, 9, 1, 14, 0, 0),
            new DateTime(2026, 9, 1, 15, 0, 0));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetByIdAsync(
            booking.Id,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.ResourceId.Should().Be("room-101");
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var result = await _service.GetByIdAsync(
            id,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingExists_ShouldCancelBooking()
    {
        // Arrange
        var booking = new Booking(
            "room-101",
            "user-1",
            new DateTime(2026, 9, 1, 14, 0, 0),
            new DateTime(2026, 9, 1, 15, 0, 0));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        await _service.CancelBookingAsync(
            booking.Id,
            CancellationToken.None);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                booking,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenAlreadyCancelled_ShouldThrowConflictException()
    {
        // Arrange
        var booking = new Booking(
            "room-101",
            "user-1",
            new DateTime(2026, 9, 1, 14, 0, 0),
            new DateTime(2026, 9, 1, 15, 0, 0));

        booking.Cancel();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var act = () => _service.CancelBookingAsync(
            booking.Id,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BookingConflictException>();

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var act = () => _service.CancelBookingAsync(
            id,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BookingNotFoundException>();
    }

    [Fact]
    public async Task GetBookingsAsync_WhenRequestIsValid_ShouldReturnBookings()
    {
        // Arrange
        var bookings = new List<Booking>
    {
        new Booking(
            "room-101",
            "user-1",
            new DateTime(2026, 9, 1, 10, 0, 0),
            new DateTime(2026, 9, 1, 11, 0, 0))
    };

        _repositoryMock
            .Setup(x => x.GetBookingsAsync(
                "room-101",
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                BookingStatus.Active,
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        // Act
        var result = await _service.GetBookingsAsync(
            "room-101",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 2),
            BookingStatus.Active,
            1,
            10,
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].ResourceId.Should().Be("room-101");
    }

    [Fact]
    public async Task GetBookingsAsync_WhenFromIsAfterTo_ShouldThrowInvalidBookingException()
    {
        // Arrange
        var from = new DateTime(2026, 9, 2);
        var to = new DateTime(2026, 9, 1);

        // Act
        var act = () => _service.GetBookingsAsync(
            "room-101",
            from,
            to,
            BookingStatus.Active,
            1,
            10,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidBookingException>();
    }

    [Fact]
    public async Task GetBookingsAsync_WhenPageIsLessThanOne_ShouldThrowInvalidBookingException()
    {
        // Act
        var act = () => _service.GetBookingsAsync(
            "room-101",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 2),
            BookingStatus.Active,
            0,
            10,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidBookingException>();
    }

    [Fact]
    public async Task GetBookingsAsync_WhenPageSizeIsInvalid_ShouldThrowInvalidBookingException()
    {
        // Act
        var act = () => _service.GetBookingsAsync(
            "room-101",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 2),
            BookingStatus.Active,
            1,
            101,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidBookingException>();
    }
}