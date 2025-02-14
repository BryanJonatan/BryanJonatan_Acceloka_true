using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System;

namespace BryanJonatan_Acceloka
{
    public class SqlScript
    {
//        -- Create Tickets table
//        CREATE TABLE Tickets(
//    TicketCode NVARCHAR(255) PRIMARY KEY,
//    TicketName NVARCHAR(255) NOT NULL,
//    CategoryName NVARCHAR(255) NOT NULL,
//    EventDateMinimum DATETIME NOT NULL,
//    EventDateMaximum DATETIME NOT NULL,
//    Quota INT NOT NULL,
//    Price INT NOT NULL
//);

//-- Create BookedTickets table
//CREATE TABLE BookedTickets(
//        BookingId NVARCHAR(255) PRIMARY KEY,
//    TicketCode NVARCHAR(255),
//    Quantity INT NOT NULL DEFAULT 1 CHECK(Quantity > 0),
//    FOREIGN KEY(TicketCode) REFERENCES Tickets(TicketCode) ON DELETE SET NULL
//);
    }
}
