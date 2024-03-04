
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateDBContext;
using SagaStateMachine.Service.StateInstances;
using SagaStateMachine.Service.StateMachine;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>()
    .EntityFrameworkRepository(options =>
    {
        options.AddDbContext<DbContext, OrderStateDBContext>((provider, _builder) =>
        {
            _builder.UseSqlServer(builder.Configuration.GetConnectionString("MSSQLServer"));
        });
    });

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

    });
});

var host = builder.Build();
host.Run();
