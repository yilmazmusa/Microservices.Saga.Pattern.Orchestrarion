using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaStateMachine.Service.StateInstances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaStateMachine.Service.StateMaps
{
    public class OrderStateMap : SagaClassMap<OrderStateInstance> // Bir sınıfın StateMap olabilmesi için SagaClassMap'tan imp. edilmesi gerekmektedir.
    {
        protected override void Configure(EntityTypeBuilder<OrderStateInstance> entity, ModelBuilder model)
        {
            entity.Property(x => x.BuyerId).IsRequired();
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.TotalPrice).HasDefaultValue(0);
        }
    }
}
