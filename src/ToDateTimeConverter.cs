using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AceSearch
{
    public class ToDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DateTimeOffset.FromUnixTimeSeconds(1000);

            if (!reader.TryGetInt64(out long value)) return new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().DateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();

        }
    }
}