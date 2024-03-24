using System;
using System.Text.Json.Serialization;

namespace AceSearch
{
    public class Channel
    {
        [JsonPropertyName("infohash")] public string InfoHash { get; set; }

        [JsonPropertyName("name")]
        [JsonConverter(typeof(TrimmingJsonConverter))]
        public string Name { get; set; }

        [JsonPropertyName("availability")] public decimal Availability { get; set; }

        [JsonPropertyName("categories")] public string[] Categories { get; set; }

        [JsonPropertyName("availability_updated_at")]
        [JsonConverter(typeof(ToDateTimeConverter))]
        public DateTime AvailabilityUpdatedAt { get; set; }

        [JsonIgnore] public string Url { get; set; }

        [JsonIgnore] public string ChannelId { get; set; }

        public string IconUrl { get; set; }
    }
}