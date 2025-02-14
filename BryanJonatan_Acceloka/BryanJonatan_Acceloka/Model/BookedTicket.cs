using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BryanJonatan_Acceloka.Model
{
    public class BookedTicket
    {
        public BookedTicket()
        {
            BookingId = string.Empty;
            TicketCode = string.Empty;
        }

        [Key]
        [Required]
        [StringLength(255)]
        public string BookingId { get; set; }

        [ForeignKey("Ticket")]
        [StringLength(255)]
        public string? TicketCode { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        

        // Navigation property
        public Ticket? Ticket { get; set; }


    }
}
