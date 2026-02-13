using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using UserCrudApp.Hubs;
using UserCrudApp.Models;

namespace UserCrudApp.Controllers
{

    //[Route("api/[controller]")]
    //[ApiController]
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly string _connectionString;
        private readonly IEmailService _email;
        private readonly IHubContext<TicketHub> _hub;


        public TicketsController(IConfiguration config , IEmailService email, IHubContext<TicketHub> hub)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _email = email;
            _hub = hub;
        }

        //public IActionResult All()
        //{
        //    var tickets = GetAllTickets();
        //    return View("All", tickets);
        //}

        [HttpGet("Tickets/All")]
        [Authorize(Roles = "Admin")]  
        public IActionResult All()
        {
            var tickets = GetAllTickets();  
            return View("All", tickets);
        }

        // List Tickets
        [HttpGet("")]
        public IActionResult Index()
        {
            var tickets = new List<Ticket>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_GetAllTickets", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                conn.Open();
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    tickets.Add(new Ticket
                    {
                        Id = (int)rdr["Id"],
                        Subject = (string)rdr["Subject"],
                        Status = (string)rdr["Status"],
                        Priority = rdr["Priority"] as string,
                        createdt = rdr["createdt"] as DateTime?,
                        AssignedEmail = rdr["AssignedEmail"] as string,
                                                DueDate = rdr["DueDate"] as DateTime?

                    });
                }
            }
            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create new ticket
        //[HttpPost]
        ////[ValidateAntiForgeryToken]
        //public IActionResult Create(Ticket model)
        //{
        //    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    int userId = int.Parse(userIdStr);
        //    using (var conn = new SqlConnection(_connectionString))
        //    using (var cmd = new SqlCommand("Usp_AddTicket", conn))
        //    {
        //        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@Subject", model.Subject);
        //        cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
        //        cmd.Parameters.AddWithValue("@Status", "Open");
        //        cmd.Parameters.AddWithValue("@Priority", model.Priority ?? "Normal");
        //        cmd.Parameters.AddWithValue("@UserId", userId);
        //        cmd.Parameters.AddWithValue("@CreateUid", userId);
        //        conn.Open();
        //        cmd.ExecuteNonQuery();
        //    }
        //    TempData["StatusMessage"] = "Ticket created!";
        //    return RedirectToAction("Index");
        //}
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Ticket model, IFormFile Attachment)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            int ticketId;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_AddTicket", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Subject", model.Subject);
                cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                cmd.Parameters.AddWithValue("@Status", "Open");
                cmd.Parameters.AddWithValue("@Priority", model.Priority ?? "Normal");
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CreateUid", userId);

                var outParam = new SqlParameter("@TicketId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                conn.Open();
                cmd.ExecuteNonQuery();

                ticketId = (int)outParam.Value;
            }

            if (Attachment != null && Attachment.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid() + Path.GetExtension(Attachment.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("Usp_AddTicketAttachment", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TicketId", ticketId);
                cmd.Parameters.AddWithValue("@FileName", Attachment.FileName);
                cmd.Parameters.AddWithValue("@FilePath", "/uploads/" + fileName);
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrEmpty(userEmail))
            {
                await _email.SendAsync(
                    userEmail,
                    "Ticket Created Successfully",
                    $@"
                <h3>Ticket Created</h3>
                <p><b>Ticket ID:</b> {ticketId}</p>
                <p><b>Subject:</b> {model.Subject}</p>
                <p><b>Priority:</b> {model.Priority}</p>
            ");
            }

            TempData["StatusMessage"] = "Ticket created successfully!";
            return RedirectToAction("Index");
        }


        // GET: Details with replies
        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            Ticket ticket = null;
            var replies = new List<TicketReply>();
            ViewBag.Users = GetUserEmailList();
            //ViewBag.AssignedUser = ticket.AssignedTo; 
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_GetTicketById", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TicketId", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        ticket = new Ticket
                        {
                            Id = (int)rdr["Id"],
                            Subject = (string)rdr["Subject"],
                            Description = rdr["Description"] as string,
                            Status = rdr["Status"] as string,
                            Priority = rdr["Priority"] as string,
                            AssignedEmail = rdr["AssignedEmail"] as string,
                            createdt = rdr["createdt"] as DateTime?,
                            FileName = rdr["FileName"] as string,
                            FilePath = rdr["FilePath"] as string
                        };
                    }
                    if (rdr.NextResult())
                    {
                        while (rdr.Read())
                        {
                            replies.Add(new TicketReply
                            {
                                Id = (int)rdr["Id"],
                                ReplyText = rdr["ReplyText"] as string,
                                createdt = rdr["createdt"] as DateTime?
                            });
                        }
                    }
                }
            }
            
            ViewBag.Replies = replies;
            return View(ticket);
        }


        // POST: Add Reply
        [HttpPost]
        public async Task<IActionResult> AddReply(TicketReply reply)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdStr);
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_AddTicketReply", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TicketId", reply.TicketId);
                cmd.Parameters.AddWithValue("@ReplyText", reply.ReplyText);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CreateUid", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            await _hub.Clients.All.SendAsync("TicketUpdated", reply.TicketId);

            var ticketInfo = GetTicketEmails(reply.TicketId);

            string toEmail = userId == ticketInfo.UserId
                ? ticketInfo.AssignedEmail
                : ticketInfo.OwnerEmail;

            if (!string.IsNullOrEmpty(toEmail))
            {
                await _email.SendAsync(
                    toEmail,
                    "New Reply on Ticket",
                    $@"
                <h3>New Reply</h3>
                <p>A new reply has been added to your ticket.</p>
                <p><b>Message:</b></p>
                <p>{reply.ReplyText}</p>
            "
                );
            }

            return RedirectToAction("Details", new { id = reply.TicketId });
        }

        private TicketEmailInfo GetTicketEmails(int ticketId)
        {
            var info = new TicketEmailInfo();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("GetTicketEmails", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TicketId", ticketId);

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        info.UserId = Convert.ToInt32(rdr["UserId"]);
                        info.OwnerEmail = rdr["OwnerEmail"]?.ToString();
                        info.AssignedEmail = rdr["AssignedEmail"]?.ToString();
                    }
                }
            }

            return info;
        }


        private List<Ticket> GetAllTickets()
        {
            var tickets = new List<Ticket>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_GetAllTickets", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                conn.Open();
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    tickets.Add(new Ticket
                    {
                        Id = (int)rdr["Id"],
                        Subject = (string)rdr["Subject"],
                        Status = rdr["Status"] as string,
                        Priority = rdr["Priority"] as string,
                        createdt = rdr["createdt"] as DateTime?,
                        AssignedEmail = rdr["AssignedEmail"] as string,
                        DueDate = rdr["DueDate"] as DateTime?
                    });
                }
            }
            return tickets;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Assign(int TicketId, int AssignedTo,string Priority)
        {
            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_UpdateTicket", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", TicketId);
                cmd.Parameters.AddWithValue("@Status", "In Progress");
                cmd.Parameters.AddWithValue("@Priority", Priority ?? "Normal");
                cmd.Parameters.AddWithValue("@AssignedTo", AssignedTo);
                cmd.Parameters.AddWithValue("@lmodifyby", adminId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            LogAudit(TicketId, "Ticket Assigned", adminId);

            await _hub.Clients.All.SendAsync("TicketUpdated", TicketId);

            var assignedEmail = GetUserEmailById(AssignedTo);

            if (!string.IsNullOrEmpty(assignedEmail))
            {
                await _email.SendAsync(
                    assignedEmail,
                    "New Ticket Assigned",
                    $@"
                <h3>Ticket Assigned</h3>
                <p>You have been assigned a new ticket.</p>
                <p><b>Ticket ID:</b> {TicketId}</p>
                <p><b>Priority:</b> {Priority}</p>
            "
                );
            }

            TempData["StatusMessage"] = "Ticket assigned!";
            return RedirectToAction("Details", new { id = TicketId });
        }

        private string GetUserEmailById(int userId)
        {
            if (userId <= 0)
                return null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "SELECT Email FROM tbl_Users WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", userId);
                conn.Open();

                return cmd.ExecuteScalar()?.ToString();
            }
        }
        private void LogAudit(int ticketId, string action, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("Usp_InsertAuditLogs", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TicketId", ticketId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@UserId", userId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }



        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Close(int TicketId)
        {
            int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("Usp_UpdateTicketStatus", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", TicketId);
                cmd.Parameters.AddWithValue("@Status", "Closed");
                cmd.Parameters.AddWithValue("@lmodifyby", adminId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["StatusMessage"] = "Ticket closed.";
            return RedirectToAction("Details", new { id = TicketId });
        }

        private List<SelectListItem> GetUserEmailList()
        {
            var list = new List<SelectListItem>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.Usp_GetAllUsersForAssign", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            conn.Open();
            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new SelectListItem
                {
                    Value = rdr["Id"].ToString(),
                    Text = rdr["Email"].ToString()
                });
            }
            return list;
        }

    }
}
