using MassTransit;
using Shared.PaymentEvents;
using Shared.Settings;

namespace Payment.API.Consumers
{
    public class PaymentStartedEventConsumer(ISendEndpointProvider sendEndpointProvider) : IConsumer<PaymentStartedEvent>
    {
        public  async Task Consume(ConsumeContext<PaymentStartedEvent> context)
        {

            var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}")); 
            if (true) //Ödeme başarılı ise burda mış gibi yapıyoruz.
            {
                PaymentCompletedEvent paymentCompletedEvent = new(context.Message.CorrelationId)
                {

                };

                await sendEndpoint.Send(paymentCompletedEvent);
            }
            else
            {
                PaymentFailedEvent paymentFailedEvent = new(context.Message.CorrelationId) 
                { 
                    OrderItems = context.Message.OrderItems, //Burda OrderItems'ları göndermeizin sebebi stoğu düşen ama ödemesi başarısız olan siparişi StateMachine.cs e gönderip orda RollBack yaparken bu OrderItems taki Count a göre geri ekleme yapıcak.
                    Message = "Yetersiz Bakiye."
                };

                await sendEndpoint.Send(paymentFailedEvent);
            }
        }

      
    }
}
