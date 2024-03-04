using MongoDB.Driver;

namespace Stock.API.Services
{
    public class MongoDBService
    {
        readonly IMongoDatabase mongoDatabase;
        public MongoDBService(IConfiguration configuration) //Bir servis ya da sınıf içerisinden appsettings.json daki bir veriyi getirebilmek için IConfiguration dan yararlarınız.
        {
            MongoClient client = new(configuration.GetConnectionString("MongoDB"));
            mongoDatabase = client.GetDatabase("StockDB2");
        }

        public IMongoCollection<T> GetCollection<T>() => mongoDatabase.GetCollection<T>(typeof(T).Name.ToLowerInvariant());
    }
}
