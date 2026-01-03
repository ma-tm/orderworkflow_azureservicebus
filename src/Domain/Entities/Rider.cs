using Newtonsoft.Json;

namespace Domain.Entities
{
    public class Rider
    {
        [JsonProperty("riderId")]
        public string RiderId { get; set; }

        [JsonProperty("isAvailable")]
        public string IsAvailable { get; set; }

        [JsonProperty("lastKnownLocation")]
        public string LastKnownLocation { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
