namespace MedBridge.Dtos.OrderDtos
{
    public class OrderDetailsDto
    {
        public int OrderId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public bool UserConfirmedShipped { get; set; }
        public bool DeliveryPersonConfirmedShipped { get; set; }
        public List<OrderDetailsItemDto> Items { get; set; } = new List<OrderDetailsItemDto>();
    }

    public class OrderDetailsItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
