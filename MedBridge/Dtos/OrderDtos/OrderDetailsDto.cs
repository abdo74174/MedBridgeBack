namespace MedBridge.Dtos.OrderDtos
{
    public class OrderDetailsDto
    {
        public int OrderId { get; set; }
        public string UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderDetailsItemDto> Items { get; set; }
    }

    public class OrderDetailsItemDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
