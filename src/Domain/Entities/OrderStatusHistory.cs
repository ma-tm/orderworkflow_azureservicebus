using Newtonsoft.Json;

namespace Domain.Entities
{
    public class OrderStatusHistory
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("fromStatus")]
        public string FromStatus { get; set; }

        [JsonProperty("toStatus")]
        public string ToStatus { get; set; }

        [JsonProperty("changedByRole")]
        public string ChangedByRole { get; set; }

        [JsonProperty("changedByUserId")]
        public string ChangedByUserId { get; set; }
    }
}
