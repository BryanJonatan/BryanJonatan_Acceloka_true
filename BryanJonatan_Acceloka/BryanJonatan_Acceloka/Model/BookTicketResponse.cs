namespace BryanJonatan_Acceloka.Model
{
    public class BookTicketResponse
    {
        public required string BookingId {  get; set; }
        public required string TicketName { get; set; }
        public required string TicketCode { get; set; }
        public int Price { get; set; }  
        public int Quantity { get; set; }
        public int TotalPrice { get; set; } 
    }
}
