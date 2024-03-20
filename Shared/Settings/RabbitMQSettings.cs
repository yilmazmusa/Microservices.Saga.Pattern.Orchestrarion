using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Settings
{
    public static class RabbitMQSettings //İçinde sadece kuyruk isimlerini barındıracağından ve değişmeyeceğinden dolayı static olabilir.
    {
        public const string StateMachineQueue = $"state-machine-queue";
        public const string Stock_OrderCreatedEventQueue = $"stock-order-created-event-queue";
        public const string Order_OrderComletedEventQueue = $"order-order-comleted-event-queue";
        public const string Order_OrderFailedEventQueue = $"order-order-failed-event-queue";
        public const string Stock_RollbackMessageQueue = $"stock-rollback-failed-message-queue";
        public const string Payment_StartedEventQueue = $"payment-started-event-queue";
        public const string Payment_ComletedEventQueue = $"payment-comleted-event-queue";
        public const string Payment_FaiedEventQueue = $"payment-faied-event-queue";



    }
}
