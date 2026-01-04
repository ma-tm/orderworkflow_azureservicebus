using Newtonsoft.Json;

namespace OrderService.Models
{
    public enum OrderStatus
    {
        Placed,
        PendingApproval,
        Rejected,
        Accepted,
        InPreparation,
        ReadyForPickup,
        AssignedToRider,
        PickedUp,
        OutForDelivery,
        Delivered,
        Cancelled
    }    

    public sealed class OrderItem
    {
        [JsonProperty("sku")]
        public string Sku { get; init; } = default!;
        [JsonProperty("name")]
        public string Name { get; init; } = default!;
        [JsonProperty("quantity")]
        public int Quantity { get; init; }
        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; init; }
    }

    public class Order
    {
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("customerId")]
        public string CustomerId { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        private string _orderStatus;
        [JsonProperty("status")]
        public string OrderStatus
        {
            get => _orderStatus;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("OrderStatus cannot be null or whitespace.", nameof(value));
                }

                if (!Enum.TryParse<OrderStatus>(value, ignoreCase: true, out var parsed))
                {
                    var valid = string.Join(", ", Enum.GetNames(typeof(OrderStatus)));
                    throw new ArgumentException($"Invalid OrderStatus '{value}'. Valid values: {valid}.", nameof(value));
                }

                // Normalize stored value to the enum name casing
                _orderStatus = parsed.ToString();
            }
        }

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("assignedRiderId")]
        public int? AssignedRiderId { get; set; }

        [JsonProperty("deliveryAddress")]
        public string DeliveryAddress { get; set; }

        [JsonProperty("items")]
        public List<OrderItem> Items;
    }

    public sealed record CreateOrderRequest(
        string TenantId,
        string CustomerId,
        string DeliveryAddress,
        List<OrderItem> Items
    );

    public sealed record CreateOrderResponse(
        string OrderId,
        string TenantId,
        string CustomerId,
        string Status,
        decimal TotalAmount,
        DateTimeOffset CreatedAt
    );
}
