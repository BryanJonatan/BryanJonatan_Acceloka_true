using BryanJonatan_Acceloka.Model;
using Microsoft.EntityFrameworkCore;

namespace BryanJonatan_Acceloka
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<BookedTicket> BookedTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.TicketCode);
                entity.Property(e => e.TicketCode).HasMaxLength(255);
                entity.Property(e => e.TicketName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.CategoryName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Price).IsRequired();
                entity.Property(e => e.Quota).IsRequired();
            });

            modelBuilder.Entity<BookedTicket>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.BookingId).HasMaxLength(255);
                entity.Property(e => e.TicketCode).HasMaxLength(255);
                entity.Property(e => e.Quantity).HasDefaultValue(1);

                entity.HasOne(d => d.Ticket)
                    .WithMany(p => p.BookedTickets)
                    .HasForeignKey(d => d.TicketCode)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
