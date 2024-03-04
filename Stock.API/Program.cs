using MassTransit;
using MongoDB.Driver;
using Stock.API.Services;

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

builder.Services.AddSingleton<MongoDBService>(); //Oluşturduğumuz MongoDBService'i IOC Container a ekledik.

var app = builder.Build();

using IServiceScope scope = builder.Services.BuildServiceProvider().CreateScope(); //Bir defa kullanılıp sonra dispose edileceği için using kullandık.
MongoDBService mongoDBService = scope.ServiceProvider.GetRequiredService<MongoDBService>();

// Bu kontrol Stock.API ayağa kalktığında MongoDB inde hem Stock Collectionunu oluşturacak ve  Stock Collection'unda data yoksa if bloğunun içindeki dataları ekleyecek,
// varsa eklemeyecek  zaten.
if (! await (await mongoDBService.GetCollection<Stock.API.Models.Stock>().FindAsync(x => true)).AnyAsync()) 
{
    mongoDBService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 1,
        ProductName = "Elma",
        Count = 200,
    });
    mongoDBService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 2,
        ProductName = "Armut",
        Count = 300,
    });
    mongoDBService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 3,
        ProductName = "Muz",
        Count = 50,
    });
    mongoDBService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 4,
        ProductName = "Avakado",
        Count = 10,
    });
    mongoDBService.GetCollection<Stock.API.Models.Stock>().InsertOne(new()
    {
        ProductId = 5,
        ProductName = "Çilek",
        Count = 60,
    });
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();

