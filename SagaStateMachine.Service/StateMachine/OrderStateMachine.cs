using MassTransit;
using SagaStateMachine.Service.StateInstances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateMachine
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance> // Bir sınıfın StateMachine olabilmesi için MassTransitStateMachine'den inherit edilmesi gerekiyor. 
    {
        public OrderStateMachine()
        {
            InstanceState(instance => instance.CurrentSate);
        }
    }
}
