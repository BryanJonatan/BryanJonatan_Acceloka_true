using System.ComponentModel.DataAnnotations;

namespace BryanJonatan_Acceloka.Model
{
    public class Ticket
    {

        public Ticket()
        {
            TicketCode = string.Empty;
            TicketName = string.Empty;
            CategoryName = string.Empty;
            BookedTickets = new List<BookedTicket>();
        }

        [Key]
        [Required]
        [StringLength(255)]
        public string TicketCode { get; set; }

        [Required]
        [StringLength(255)]
        public string TicketName { get; set; }

        [Required]
        [StringLength(255)]
        public string CategoryName { get; set; }

        [Required]
        public DateTime EventDateMinimum { get; set; }

        [Required]
        public DateTime EventDateMaximum { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quota { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Price { get; set; }

        // Navigation property
        public ICollection<BookedTicket> BookedTickets { get; set; }


    }
}
