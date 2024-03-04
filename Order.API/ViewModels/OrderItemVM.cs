namespace Order.API.ViewModels
{
    public class OrderItemVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; } //Normalde ürün fiyatını kullanıcı belirlemez ama biz burda mış gibi yaptık.

    }
}
