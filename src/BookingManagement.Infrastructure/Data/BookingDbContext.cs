using BookingManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingManagement.Infrastructure.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> option)
            : base(option)
        {
            
        }

        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.ResourceId)
                .IsRequired()
                .HasMaxLength(100);

                entity.Property(b => b.UserId)
                .IsRequired()
                .HasMaxLength(100);

                entity.Property(b => b.Status)
                .IsRequired();

                entity.Property(b => b.StartDateTime)
                    .IsRequired();

                entity.Property(b => b.EndDateTime)
                    .IsRequired();

                entity.Property(b => b.CreatedAt)
                    .IsRequired();

                entity.Property(b => b.CancelledAt)
                    .IsRequired(false);
            });
        }
    }
}