namespace Order.API.ViewModels
{
    public class CreateOrderVM
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public List<OrderItemVM> OrderItems { get; set; }
    }
}
