using MassTransit;
using Order.API.Context;
using Order.API.Models;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderCompletedEventConsumer(OrderDbContext orderDbContext) : IConsumer<OrderCompletedEvent> // Bir sınıfın Consumer olabilmesi için IConsumer interfacesinden implemente edilmesi gerekir
    {//Yukarda diyoruz ki OrderCompletedEventConsumer'ı OrderCompletedEvent te gelen mesaja göre işlem yapacak onu dinleyecek.Ve ilgili order'ı veritabanında bulmak için OrderDbContext i inject ettik.
        public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
        {
            Order.API.Models.Order order = await orderDbContext.Orders.FindAsync(context.Message.OrderId); //verdiğimiz OrderId ye ait siparişi vt den getir diyoruz
            if (order != null)
            {
                order.OrderStatus = Enums.OrderStatus.Completed; //İlgili siparişi bulup durumunu completed a çekiyoruz
                await orderDbContext.SaveChangesAsync();
            }
        }
    }
}
