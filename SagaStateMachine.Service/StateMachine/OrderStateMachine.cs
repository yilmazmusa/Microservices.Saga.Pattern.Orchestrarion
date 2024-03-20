using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.Settings;
using Shared.StockEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateMachine
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance> // Bir sınıfın StateMachine olabilmesi için MassTransitStateMachine'den inherit edilmesi gerekiyor. 
    {
        //Gelebilecek   eventleri(bak StateMachinin gönderecekleri değil StateMachine gelen) bu şekilde property olarak tanımlıyoruz ve böylece StateMachine de temsil ediyoruz.
        //Mesela burda OrderCreatedEvent yok çünkü o StateMachine gelmiyo onu zaten StateMachine göndericek(publish ya da send ile )

        public Event<OrderStartedEvent> OrderStartedEvent { get; set; }
        public Event<StockReservedEvent> StockReservedEvent { get; set; }
        public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
        public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

        //Burda yukarda oluşturduğumuz Event'lerin State'lerini oluşturuyoruz.
        public State OrderCreated { get; set; }
        public State StockReserved { get; set; }
        public State StockNotReserved { get; set; }
        public State PaymentCompleted { get; set; }
        public State PaymentFailed { get; set; }


        public OrderStateMachine()
        {
            InstanceState(instance => instance.CurrentSate);

            //Event fonksiyonu, gelen eventlere göre aksiyon almamızı sağlayan bir fonkdur.
            Event(() => OrderStartedEvent,
                orderStateInstance => orderStateInstance.CorrelateBy<int>(database => database.OrderId, @event => @event.Message.OrderId)
                .SelectId(e => Guid.NewGuid())); // yeni siparişinOrderId si databasedeki herhangi bir OrderId ile uyuşmuyorsa yeni bir CorrelationId olıştur dedik.Eğer uyuşuyorsa gelenin  yeni bir sipariş olmadığını anlıyo ve kaydetmiyoruz uyuşmuyorsa bu yeni bir sipariş deyip yeni bir CorrelationId oluşturup kaydediyoruz.

            //Esasında aşağıdaki yaptığımız işlemler, tetikleyici event(OrderStartedEvent) dışındaki eventlerde,
            //tetikleyici eventlerde oluşturulmuş olan  CorrelationId değerine sahip olan stateInstance üzerinde
            //çalışma yapıyoruz aşağıda onu belirtiyoruz.

            Event(() => StockNotReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
            Event(() => StockReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

            Event(() => PaymentCompletedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
            Event(() => PaymentFailedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));


            //Aşağıda eğerki gelen event tetikleyici event olan OrderStartedEvent ise oluşturulacak olan instancenin verilerinin neler olacağını tanımladık.
            Initially(When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId; //eventten(istekten) gelen OrderId yi database ekledik
                    context.Instance.BuyerId = context.Data.BuyerId; //eventten(istekten) gelen BuyerId yi database ekledik
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                    context.Instance.CreatedDate = DateTime.UtcNow;
                })
                .TransitionTo(OrderCreated) //Siparişimiz oluşturulur oluşturlmaz TransitionTo ile ilgili siparişin durumunu OrderCreated'e çekeriz.Sonrasında orderCreated eventini Stock_OrderCreatedEventQueue kuyruğuna  yollayacağız. 
                .Send(new Uri($"queue: {RabbitMQSettings.Stock_OrderCreatedEventQueue}"),
                context => new OrderCreatedEvent(context.Instance.CorrelationId) // CorrelationId yi veritabanından alıp Stock.API ye gönderiyoruz
                {
                    OrderItems = context.Data.OrderItems
                }));


                        
            During(OrderCreated,
                //1.Durum 
                When(StockReservedEvent)
                .TransitionTo(StockReserved)
                .Send(new Uri($"queue: {RabbitMQSettings.Payment_StartedEventQueue}"),
                context => new PaymentStartedEvent(context.Instance.CorrelationId)
                {
                    TotalPrice = context.Instance.TotalPrice, // TotalPrice'ı veritabanından getittirebiliriz
                    OrderItems = context.Data.OrderItems      // OrderItems'ı event üzerinden aldık
                }),
                //2.Durum
                When(StockNotReservedEvent)
                .TransitionTo(StockNotReserved)
                .Send(new Uri($"queue: {RabbitMQSettings.Order_OrderFailedEventQueue}"),
                context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                }));

            //3.Durum 
            During(StockReserved,
                When(PaymentCompletedEvent)
                .TransitionTo(PaymentCompleted)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderComletedEventQueue}"),
                context => new OrderCompletedEvent
                {
                    OrderId = context.Instance.OrderId
                })
                .Finalize(),
                When(PaymentFailedEvent)
                .TransitionTo(PaymentFailed)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"),
                context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                })
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_RollbackMessageQueue}"),
                context => new StockRollbackMessage
                {
                    OrderItems = context.Data.OrderItems
                }));

            SetCompletedWhenFinalized();


        }
    }
}


//Yukarda 1.durumda;
//kısaca yukarda şunu yaptık 
//Eğerki veritabanında ilgili siparişin o anki state'i OrderCreated ise(bunu During ile kontrol ettik)
//ve gelen eventte  StockReservedEvent ise (bunu When ile kontrol ettik) durumu StockReserved'e çek diyoruz.
//Yani sipariş başarılı bir şekilde oluşturuldu stoğuda kontrol edildi stoğuda var deyip işi Payment.API ye atıyoruz.
//Sonrasında  Payment.API nin dinlediği  Payment_StartedEventQueue kuyruğuna PaymentStartedEvent'i yolluyoruz.(bunu da Send ile yaptık).


//Yukarda 2.durumda;
//kısaca yukarda şunu yaptık 
//Eğerki o anki event StockNotReservent ise(bunu When ile kontrol ettik)
//state'i StockNotReserved'e çektik(bunu TransactionTo ile yaptık)
//Sonrasında da siparişin başarısız olduğunu haber verebilmek için Order.API nin dinlediği  Order_OrderFailedEventQueue kuyruğuna OrderFailedEvet'i yolluyoruz(bunu da Send ile yaptık).


//Sonrasında

//Eğer ki 
//1.Durum olur ise en son topu Payment_StartedEventQueue kuyruğuna PaymentStartedEvent'i atmıştık yani
//ödemeyi başlatacağımız eventi yayınladık.Tamam ödeme başladı ama ödeme başarılı mı olucak başarısız mı olucak ?


//Bunun için de bir During() işlemi daha yapıcaz.Bu During kontrolünde siparişin durumu(state'i) StockReserved olucak ve gelen evet 
//3.Durumda => PaymentCompletedEvent gelir ise  ki böyle olursa siparişin durumu(state'i) PaymentCompleted'e
//çekilecek ve sonrasında Order.API nin dinlediği OrderCompletedEventQueue kuyruğuna OrderCompletedEvent gönderilecek
//Başarılı bir şekilde süreç tamamlandığı için Finalize() fonk ile bu stateIntanceyi veritabanında siliyoruz.
//Çünkü başarılı olarak tamamlanan stateInstance'lerin veritabanında tutulmasına gerek yoktur çünkü zaten başarılı sonuçlandı.


//4.Durumda => PaymentFailedEvent gelir ise  ki böyle olursa siparişin durumu(state'i) PaymentFailed'e
//çekilecek ve Stock.API nin dinlediği OrderFailedEventQueue kuyruğuna StockRollBackMessage mesajı gönderilecektir.

//Son olarak durumunu Finalize() ye yani başarılıya çektiklerimizi SetCompletedWhenFinalized() ile veritabanından siliyoruz.

//context.Instance => veritabanındaki ilgili şiparişi karşılık gelir.
//context.Data => o anki ilgili eventten gelen datayı temsil eder.
