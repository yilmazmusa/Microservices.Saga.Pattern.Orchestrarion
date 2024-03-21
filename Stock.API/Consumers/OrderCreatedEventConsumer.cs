using MassTransit;
using MongoDB.Driver;
using Shared.OrderEvents;
using Shared.Settings;
using Shared.StockEvents;
using Stock.API.Models;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider) : IConsumer<OrderCreatedEvent>
    {//Stock.API de MongoDB kullandığımız için MongoDBService i inject ettik.
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResults = new(); //Bir siparişteki her bir ürünün VT da varmı yok mu kontrolü için eğer varsa her ürün için buraya true basıcak ve hepsi true ise bu siparişi StockReserved e çekicek yani stoğu var diyecek
            var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                stockResults.Add((await (await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId && s.Count >= (long)orderItem.Count)).AnyAsync()));
            }

            //Her iki durumda da yani if te StateMachineQueue kuyruğuna StockReservedEventini, else durumunda da yine StateMachineQueue kuyruğuna StockNotReservedEventini göndereceğimiz için hem if bloğu hemde else bloğu içinde yapmamak için kuyruğu burda tanımladık.
            var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

            //stockResults'ın içindeki tüm değerler true'mu kontrolü.Eğer hepsi true ise hepsinin stoğu vardır demektir.
            //Öyle ise StockReservedEvent'i yayınlarız, ama eğer 1 tane bile false varsa bir ürünün stoğu yoktur demektir o zamanda StockNotReservedEvent'ini yayınlarız.
            if (stockResults.TrueForAll(s => s.Equals(true)))
            {
                foreach (var orderItem in context.Message.OrderItems)
                {
                    var product = await (await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync(); //İlgili productId ye ait ürünü bulduk, şimdi istekte gelen kadar stoğunu azaltıcaz.
                    product.Count -= orderItem.Count;
                    await stockCollection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, product); // İligli ürünün istekte gelen count kadar countunu azalttık yukarıda burda da bu değişikliği Veritabanına yansıttık. 
                }

                StockReservedEvent stockReservedEvent = new(context.Message.CorrelationId) //StateMachine bu CorrelationId ye göre tanıyıp  ve burdan gönderdiğimiz Event bilgisine göre OrderStateMachine.cs te ona göre işlem yapıyo 
                {
                    OrderItems = context.Message.OrderItems
                };
                await sendEndpoint.Send(stockReservedEvent);
            }
            else
            {
                StockNotReservedEvent stockNotReservedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Stock yetersiz."
                };

                await sendEndpoint.Send(stockNotReservedEvent);
            }

        }
    }
}
