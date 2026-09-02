using BookingManagement.Domain.Entities;
using BookingManagement.Infrastructure.Data;
using BookingManagement.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BookingManagement.Tests.Integration;

public class BookingConcurrencyTests
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=BookingManagement_Test;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public async Task ConcurrentBookingsForSameResourceAndTime_ShouldAllowOnlyOne()
    {
        // Arrange
        await ResetDatabaseAsync();

        var booking1 = new Booking(
            "room-101",
            "user-1",
            new DateTime(2026, 9, 1, 14, 0, 0),
            new DateTime(2026, 9, 1, 15, 0, 0));

        var booking2 = new Booking(
            "room-101",
            "user-2",
            new DateTime(2026, 9, 1, 14, 0, 0),
            new DateTime(2026, 9, 1, 15, 0, 0));

        try
        {
            // Act
            var task1 = CreateBookingAsync(booking1);
            var task2 = CreateBookingAsync(booking2);

            var results = await Task.WhenAll(task1, task2);

            // Assert
            results.Count(x => x).Should().Be(1);
        }
        finally
        {
            await CleanupDatabaseAsync();
        }
    }

    private static async Task<bool> CreateBookingAsync(
        Booking booking)
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new BookingDbContext(options);

        var repository = new BookingRepository(context);

        return await repository.TryAddBookingAsync(
            booking,
            CancellationToken.None);
    }

    private static async Task ResetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new BookingDbContext(options);

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task CleanupDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new BookingDbContext(options);

        await context.Database.EnsureDeletedAsync();
    }
}