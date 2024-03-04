using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateDBContext
{
    public class OrderStateDBContext : SagaDbContext // Bir sınıfın StateDBContext olabilmesi için SagaDbContext ten imp edilmsi gerekir
    {
        public OrderStateDBContext(DbContextOptions options) : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get
            {
                yield return new OrderStateMap();
            }
        }
    }
}
