using System.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace UserCrudApp.Services
{
    public class EmailQueueConsumer : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(queue: "email_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null,
                                 cancellationToken: stoppingToken);

            var consumer =  new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var email = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[x] Will send welcome email to {email}");

                // simulate email sending
                await Task.Delay(500);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(
               queue: "email_queue",
               autoAck: false,
               consumer: consumer,
               cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
