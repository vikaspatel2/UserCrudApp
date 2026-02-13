using Microsoft.AspNetCore.Identity;
using RabbitMQ.Client;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UserCrudApp.Helpers
{
    public class EmailQueuePublisher
    {
        public static async Task PublishEmail(string email)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "email_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var body = Encoding.UTF8.GetBytes(email);
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "email_queue",
                body: body);
        }
    }
}
