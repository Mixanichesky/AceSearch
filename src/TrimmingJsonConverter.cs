using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AceSearch
{
    public class TrimmingJsonConverter : JsonConverter<string>
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetString()?.Trim();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value?.Trim());
    }
}