using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingManagement.Infrastructure.Data;

public class BookingDbContextFactory
    : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<BookingDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=BookingManagementDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new BookingDbContext(optionsBuilder.Options);
    }
}