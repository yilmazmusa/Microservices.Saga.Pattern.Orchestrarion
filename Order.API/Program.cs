
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Order.API.Context;
using Order.API.ViewModels;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.Settings;

namespace Order.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddMassTransit(configurator =>
            {
                configurator.UsingRabbitMq((context, _configure) =>
                {
                    _configure.Host(builder.Configuration["RabbitMQ"]);
                });
            });

            //Aşağıda diyoruz ki sen contex olarak OrderDbContext i kullanacaksın diyoruz.Yani OrderDbContext i ıugulamaya servis olarak ekliyoruz.
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQLServer")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapPost("/create-order", async (CreateOrderVM model, OrderDbContext context, ISendEndpointProvider sendEndpointProvider) =>
            {
                Order.API.Models.Order order = new()
                {
                    BuyerId = model.BuyerId,
                    CreatedDate = DateTime.Now,
                    OrderStatus = Enums.OrderStatus.Suspend,
                    TotalPrice = model.OrderItems.Sum(oi => oi.Price * oi.Count),
                    OrderItems = model.OrderItems.Select(oi => new Order.API.Models.OrderItem
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.ProductName,
                        Price = oi.Price,
                        Count = oi.Count
                    }).ToList(),

                };
                await context.Orders.AddAsync(order);
                await context.SaveChangesAsync();

                OrderStartedEvent orderStartedEvent = new()
                {
                    BuyerId = model.BuyerId,
                    OrderId = order.Id,
                    TotalPrice = model.OrderItems.Sum(oi => oi.Count * oi.Price),
                    OrderItems = model.OrderItems.Select(oi => new OrderItemMessage
                    {
                        ProductId = oi.ProductId,
                        Count = oi.Count,
                        Price = oi.Price
                    }).ToList()

                };

               ISendEndpoint sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}")); //Burda göndereceğimiz kuyruk ismini/endpointini belirliyoruz.
               await sendEndpoint.Send<OrderStartedEvent>(orderStartedEvent); //Burda da o kuyruğa ilgili eventi(orderStartedEvent) gönderiyoruz.Geri kalanını StateMachine devralacak o da Stock işlemleri için OrderCreatedEvent'i yayınlayacak o eventi dinleyen Stock.API de stock işlemlerini yapıcak

            });

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
