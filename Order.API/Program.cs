
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Order.API.Context;
using Order.API.ViewModels;

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

            app.MapPost("/create-order", async (CreateOrderVM model, OrderDbContext context) =>
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
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
