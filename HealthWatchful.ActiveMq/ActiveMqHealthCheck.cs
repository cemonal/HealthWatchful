using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HealthWatchful.ActiveMq
{
    public class ActiveMqHealthCheck : IHealthCheck
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly string _destinationName;

        public ActiveMqHealthCheck(IConnectionFactory connectionFactory, string destinationName)
        {
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

            _connectionFactory = connectionFactory;
            _destinationName = destinationName;
        }

        public ActiveMqHealthCheck(Uri brokerUri, string destinationName) : this(new ConnectionFactory(brokerUri), destinationName) { }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result;

            try
            {
                using (var connection = _connectionFactory.CreateConnection())
                {
                    using (var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                    {
                        using (var destination = new ActiveMQQueue(_destinationName))
                        {
                            using (var producer = session.CreateProducer(destination))
                            {
                                producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
                                producer.Priority = MsgPriority.AboveLow;

                                var message = new ActiveMQMessage
                                {
                                    NMSTimeToLive = TimeSpan.FromMilliseconds(1000)
                                };

                                producer.Send(message);
                            }
                        }
                    }
                }

                result = new HealthCheckResult(HealthStatus.Healthy, "OK");
            }
            catch (Exception ex)
            {
                result = new HealthCheckResult(context.Registration.FailureStatus, description: ex.Message, exception: ex);
            }

            return Task.FromResult(result);
        }
    }
}