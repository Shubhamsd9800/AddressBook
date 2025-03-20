using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BusinessLayer.Service;
using Microsoft.Extensions.Hosting;
using RepositoryLayer.Interface;
using Microsoft.Extensions.Logging;
namespace BusinessLayer.Helper
{
    public class RabbitMQConsumer:BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IModel _channel;
        private IConnection _connection;

        public RabbitMQConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                UserName = _configuration["RabbitMQ:Username"],
                Password = _configuration["RabbitMQ:Password"]
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare("UserRegistrationQueue", true, false, false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ RabbitMQ Connection Error: {ex.Message}");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (_connection == null || _channel == null)
            {
                _logger.LogError("❌ RabbitMQ connection failed. Consumer will not start.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("✅ RabbitMQ Consumer Service Started.");

            var userConsumer = new EventingBasicConsumer(_channel);
            userConsumer.Received += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var userEvent = JsonConvert.DeserializeObject<dynamic>(message);
                string email = userEvent?.Email;
                string name = userEvent?.Name;

                _logger.LogInformation($"📨 Processing User Registration: {email}");

                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                string subject = "Welcome to Address Book Application!";
                string bodyHtml = $"Hello {name},<br><br> Your account has been successfully created! 🎉<br> Start managing your contacts now!";

                bool isSent = await emailService.SendEmailAsync(email, subject, bodyHtml);
                if (isSent)
                    _logger.LogInformation($"✅ Welcome email sent to {email}");
                else
                    _logger.LogError($"❌ Failed to send welcome email to {email}");
            };

            _channel.BasicConsume("UserRegistrationQueue", true, userConsumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
