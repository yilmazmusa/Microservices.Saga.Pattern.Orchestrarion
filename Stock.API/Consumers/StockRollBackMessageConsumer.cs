using MassTransit;
using MongoDB.Driver;
using Shared.Messages;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class StockRollBackMessageConsumer(MongoDBService mongoDBService) : IConsumer<StockRollbackMessage>
    {
        public async Task Consume(ConsumeContext<StockRollbackMessage> context)
        {
           var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems) 
            {
              var product =  (await ( await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync()); //İlgili ürünü bulduk

                product.Count += orderItem.Count; //Stoğunu eksilttiğimiz kadar(istekte gelen kadar eksiltmiştik yine istekte gelen kadar arttırdık) geri arttırdık.

                stockCollection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, product); // İlgili ürünün(product) bilgilerini veritabanında güncelledik.
            }
        }
    }
}
