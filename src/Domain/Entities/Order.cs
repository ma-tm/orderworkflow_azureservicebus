using Newtonsoft.Json;

namespace Domain.Entities
{
    public class Order
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("customerId")]
        public string CustomerId { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

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
    }
}
