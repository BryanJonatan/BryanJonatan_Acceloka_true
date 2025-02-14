namespace BryanJonatan_Acceloka.Model
{
    public class AvailableTicketResponse
    {
       
            public string CategoryName { get; set; } = string.Empty;
            public string TicketCode { get; set; } = string.Empty;
            public string TicketName { get; set; } = string.Empty;
            public int Price { get; set; }
            public int AvailableQuota { get; set; }
        

    }
}
