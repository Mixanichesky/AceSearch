using System;
using System.Text.Json.Serialization;

namespace AceSearch
{
    public class Channels
    {
        [JsonPropertyName("infohash")] 
        public string Infohash { get; set; }

        [JsonPropertyName("name")]
        [JsonConverter(typeof(TrimmingJsonConverter))]
        public string Name { get; set; }

        [JsonPropertyName("availability")] 
        public decimal Availability { get; set; }

        [JsonPropertyName("categories")] 
        public string[] Categories { get; set; }

        [JsonPropertyName("availability_updated_at")]
        [JsonConverter(typeof(ToDateTimeConverter))]
        public DateTime AvailabilityUpdatedAt { get; set; }
    }
}