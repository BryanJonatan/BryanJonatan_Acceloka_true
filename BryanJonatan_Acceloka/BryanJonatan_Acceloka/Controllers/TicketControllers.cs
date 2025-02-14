using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BryanJonatan_Acceloka.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace BryanJonatan_Acceloka.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("get-available-ticket")]
        public async Task<IActionResult> GetAvailableTickets(
           string? categoryName, string? ticketCode, string? ticketName, int? price,
           DateTime? eventDateMin, DateTime? eventDateMax,
           string? orderBy = "TicketCode", string? orderState = "asc",
           int pageNumber = 1, int pageSize = 10)
        {
            var validColumns = new[] { "TicketCode", "TicketName", "CategoryName", "Price", "EventDateMinimum" };
            if (!validColumns.Contains(orderBy))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid OrderBy",
                    Detail = $"OrderBy must be one of: {string.Join(", ", validColumns)}."
                });
            }

            var query = _context.Tickets.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(categoryName))
                query = query.Where(t => t.CategoryName.Contains(categoryName));
            if (!string.IsNullOrEmpty(ticketCode))
                query = query.Where(t => t.TicketCode.Contains(ticketCode));
            if (!string.IsNullOrEmpty(ticketName))
                query = query.Where(t => t.TicketName.Contains(ticketName));
            if (price.HasValue)
                query = query.Where(t => t.Price <= price.Value);
            if (eventDateMin.HasValue)
                query = query.Where(t => t.EventDateMinimum >= eventDateMin.Value);
            if (eventDateMax.HasValue)
                query = query.Where(t => t.EventDateMaximum <= eventDateMax.Value);

            // Apply ordering
            query = orderState?.ToLower() == "desc"
                ? query.OrderByDescending(t => EF.Property<object>(t, orderBy ?? "TicketCode"))
                : query.OrderBy(t => EF.Property<object>(t, orderBy ?? "TicketCode"));

            var totalRecords = await query.CountAsync();
            var tickets = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var availableTickets = tickets.Select(t => new
            {
                t.CategoryName,
                t.TicketCode,
                t.TicketName,
                EventDateRange = new
                {
                    Minimum = t.EventDateMinimum,
                    Maximum = t.EventDateMaximum
                },
                t.Price,
                AvailableQuota = t.Quota - (_context.BookedTickets
                    .Where(bt => bt.TicketCode == t.TicketCode)
                    .Sum(bt => (int?)bt.Quantity) ?? 0)
            })
            .Where(t => t.AvailableQuota > 0)
            .ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = availableTickets
            });
        }


        [HttpPost("book-ticket")]
        public async Task<IActionResult> BookTicket([FromBody] List<BookTicketRequest> bookingRequests)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var response = new List<BookTicketResponse>();
                var categoryTotals = new Dictionary<string, int>();
                int totalPriceAllCategories = 0;

                foreach (var request in bookingRequests)
                {
                    var ticket = await _context.Tickets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TicketCode == request.TicketCode);

                    if (ticket == null)
                    {
                        return NotFound(CreateProblemDetails("Ticket Not Found", $"Ticket with code {request.TicketCode} does not exist."));
                    }

                    // Fetch available tickets using GetAvailableTickets logic
                    var availableTickets = await GetAvailableTicketsAsync(request.TicketCode);
                    var availableTicket = availableTickets.FirstOrDefault(t => t.TicketCode == request.TicketCode);

                    if (availableTicket == null || availableTicket.AvailableQuota == null || availableTicket.AvailableQuota <= 0)

                    {
                        return BadRequest(CreateProblemDetails("Ticket Unavailable", $"Ticket {request.TicketCode} is sold out or unavailable."));
                    }

                    if (request.Quantity > availableTicket.AvailableQuota)
                    {
                        return BadRequest(CreateProblemDetails("Insufficient Quota", $"Only {availableTicket.AvailableQuota} tickets available for {request.TicketCode}."));
                    }

                    if (ticket.EventDateMinimum <= DateTime.Now)
                    {
                        return BadRequest(CreateProblemDetails("Event Date Invalid", "The event date has already passed."));
                    }

                    var bookingId = Guid.NewGuid().ToString();
                    var bookedTicket = new BookedTicket
                    {
                        BookingId = bookingId,
                        TicketCode = request.TicketCode,
                        Quantity = request.Quantity
                    };

                    if (string.IsNullOrEmpty(bookedTicket.BookingId) || string.IsNullOrEmpty(bookedTicket.TicketCode))
                    {
                        return BadRequest(CreateProblemDetails("Invalid Data", "Booking ID or Ticket Code is missing."));
                    }
                    await _context.BookedTickets.AddAsync(bookedTicket);

                    int ticketTotalPrice = request.Quantity * ticket.Price;
                    response.Add(new BookTicketResponse
                    {
                        BookingId = bookingId,
                        TicketName = ticket.TicketName,
                        TicketCode = ticket.TicketCode,
                        Price = ticket.Price,
                        Quantity = request.Quantity,
                        TotalPrice = ticketTotalPrice
                    });

                    if (!categoryTotals.ContainsKey(ticket.CategoryName))
                        categoryTotals[ticket.CategoryName] = 0;
                    categoryTotals[ticket.CategoryName] += ticketTotalPrice;
                    totalPriceAllCategories += ticketTotalPrice;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Bookings = response,
                    Summary = new
                    {
                        CategoryTotals = categoryTotals,
                        GrandTotal = totalPriceAllCategories
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }

        }

        private async Task<List<AvailableTicketResponse>> GetAvailableTicketsAsync(string ticketCode)
        {
            var tickets = await _context.Tickets
                .Where(t => t.TicketCode == ticketCode)
                .Select(t => new AvailableTicketResponse
                {
                    CategoryName = t.CategoryName,
                    TicketCode = t.TicketCode,
                    TicketName = t.TicketName,
                    Price = t.Price,
                    AvailableQuota = t.Quota - (_context.BookedTickets
                        .Where(bt => bt.TicketCode == t.TicketCode)
                        .Sum(bt => (int?)bt.Quantity) ?? 0)
                })
                .Where(t => t.AvailableQuota > 0)
                .ToListAsync();

            return tickets;
        }

        private ProblemDetails CreateProblemDetails(string title, string detail)
        {
            return new ProblemDetails { Title = title, Detail = detail };
        }



        [HttpGet("get-booked-ticket/{bookedTicketId}")]
        public async Task<IActionResult> GetBookedTicket(string bookedTicketId)
        {
            var bookedTicket = await _context.BookedTickets
                .Include(bt => bt.Ticket)
                .FirstOrDefaultAsync(bt => bt.BookingId == bookedTicketId);

            if (bookedTicket == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "Booked Ticket Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = $"Booked Ticket with ID {bookedTicketId} does not exist."
                });
            }

            if (bookedTicket.Ticket == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "Associated Ticket Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = $"No ticket found for booking {bookedTicketId}"
                });
            }

            var response = new
            {
                KodeTiket = bookedTicket.TicketCode,
                NamaTiket = bookedTicket.Ticket?.TicketName,
                TanggalEvent = new
                {
                    Minimum = bookedTicket.Ticket?.EventDateMinimum,
                    Maximum = bookedTicket.Ticket?.EventDateMaximum
                },
                Quantity = bookedTicket.Quantity,
                Kategori = bookedTicket.Ticket?.CategoryName
            };

            return Ok(response);
        }

        [HttpDelete("revoke-ticket/{bookedTicketId}/{ticketCode}/{qty}")]
        public async Task<IActionResult> RevokeTicket(string bookedTicketId, string ticketCode, int qty)
        {
            var bookedTicket = await _context.BookedTickets
                .Include(bt => bt.Ticket)
                .FirstOrDefaultAsync(bt => bt.BookingId == bookedTicketId && bt.TicketCode == ticketCode);

            if (bookedTicket == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "Booked Ticket Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = $"Booked Ticket with ID {bookedTicketId} and Ticket Code {ticketCode} does not exist."
                });
            }

            if (qty > bookedTicket.Quantity)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "Invalid Quantity",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = $"The requested quantity exceeds the booked quantity for ticket {ticketCode}."
                });
            }

            bookedTicket.Quantity -= qty;
            if (bookedTicket.Quantity <= 0)
            {
                _context.BookedTickets.Remove(bookedTicket);
            }

            await _context.SaveChangesAsync();

            var response = new
            {
                KodeTicket = bookedTicket.TicketCode,
                NamaTicket = bookedTicket.Ticket?.TicketName,
                NamaKategori = bookedTicket.Ticket?.CategoryName,
                SisaQuantity = bookedTicket.Quantity
            };

            return Ok(response);
        }

        [HttpPut("edit-booked-ticket/{bookedTicketId}")]
        public async Task<IActionResult> EditBookedTicket(string bookedTicketId, [FromBody] List<BookedTicketUpdateRequest> updates)
        {
            var bookedTicket = await _context.BookedTickets
                .Include(bt => bt.Ticket)
                .FirstOrDefaultAsync(bt => bt.BookingId == bookedTicketId);

            if (bookedTicket == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7807",
                    Title = "Booked Ticket Not Found",
                    Status = (int)HttpStatusCode.NotFound,
                    Detail = $"Booked Ticket with ID {bookedTicketId} does not exist."
                });
            }

            foreach (var update in updates)
            {
                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketCode == update.TicketCode);
                if (ticket == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7807",
                        Title = "Ticket Not Found",
                        Status = (int)HttpStatusCode.NotFound,
                        Detail = $"Ticket with code {update.TicketCode} does not exist."
                    });
                }

                if (update.Quantity < 1)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7807",
                        Title = "Invalid Quantity",
                        Status = (int)HttpStatusCode.BadRequest,
                        Detail = "Quantity must be at least 1."
                    });
                }

                var totalBookedQuantity = await _context.BookedTickets
                    .Where(bt => bt.TicketCode == update.TicketCode)
                    .SumAsync(bt => bt.Quantity);

                if (update.Quantity > (ticket.Quota - totalBookedQuantity + bookedTicket.Quantity))
                {
                    return BadRequest(new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc7807",
                        Title = "Insufficient Quota",
                        Status = (int)HttpStatusCode.BadRequest,
                        Detail = $"The requested quantity exceeds the available quota for ticket {update.TicketCode}."
                    });
                }

                bookedTicket.Quantity = update.Quantity;
            }

            await _context.SaveChangesAsync();

            var response = new
            {
                KodeTicket = bookedTicket.TicketCode,
                NamaTicket = bookedTicket.Ticket?.TicketName,
                NamaKategori = bookedTicket.Ticket?.CategoryName,
                SisaQuantity = bookedTicket.Ticket?.Quota - await _context.BookedTickets
                    .Where(bt => bt.TicketCode == bookedTicket.TicketCode)
                    .SumAsync(bt => bt.Quantity) ?? 0
            };

            return Ok(response);
        }
    }
}