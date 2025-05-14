using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace BusinessLayer.Helper
{
    public class RabbitMQPublisher
    {
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly string _exchange;

        public RabbitMQPublisher(IConfiguration configuration)
        {
            var rabbitConfig = configuration.GetSection("RabbitMQ");
            _host = rabbitConfig["Host"];
            _username = rabbitConfig["Username"];
            _password = rabbitConfig["Password"];
            _exchange = rabbitConfig["Exchange"];
        }

        public void PublishMessage(string queueName, object message)
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                UserName = _username,
                Password = _password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(_exchange, ExchangeType.Direct);
            channel.QueueDeclare(queueName, true, false, false, null);
            channel.QueueBind(queueName, _exchange, queueName);

            var jsonMessage = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            channel.BasicPublish(_exchange, queueName, null, body);
            Console.WriteLine($"📩 Published message to {queueName}: {jsonMessage}");
        }
    }
}
