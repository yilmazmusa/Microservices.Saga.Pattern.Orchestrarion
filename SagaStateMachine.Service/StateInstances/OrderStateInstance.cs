using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateInstances
{
    public class OrderStateInstance : SagaStateMachineInstance //Bir sınıfın StateInstance olabilmesi için SagaStateMachineInstance'dan imp. edilmesi gerekir.
    {
        public Guid CorrelationId { get; set; }
        public string CurrentSate { get; set; }
        public int OrderId { get; set; }
        public int BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; }

    }
}
