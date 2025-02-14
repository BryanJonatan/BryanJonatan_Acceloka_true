namespace BryanJonatan_Acceloka.Model
{
    public class BookedTicketUpdateRequest
    {
        public required string TicketCode { get; set; } // Ticket code to update
        public int Quantity { get; set; } // New quantity
    }
}
