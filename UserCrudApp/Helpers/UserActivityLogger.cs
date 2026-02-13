using Microsoft.Data.SqlClient;
using System.Data;

namespace UserCrudApp.Helpers
{
    public class UserActivityLogger
    {
        public static async Task LogAsync(
        HttpContext context,
        string action,
        int? userId,
        string email,
        string connectionString)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var agent = context.Request.Headers["User-Agent"].ToString();
            var location = "Unknown";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Usp_InsertUserActivityLog", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", email ?? "");
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@IPAddress", ip ?? "");
            cmd.Parameters.AddWithValue("@UserAgent", agent ?? "");
            cmd.Parameters.AddWithValue("@Location", location);

            conn.Open();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
