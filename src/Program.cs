using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AceSearch
{

    class Program
    {
        static async Task Main()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

                var configuration = builder.Build();
                var settings = new Settings();
                configuration.GetSection("SearchSettings").Bind(settings);

                using (var client = new HttpClient())
                {
                    var json = await client.GetStringAsync(
                        "https://search.acestream.net/all?api_version=1.0&api_key=test_api_key");

                    var channels = JsonSerializer.Deserialize<Channels[]>(json);
                    var allChannels = channels.Where(ch =>
                        ch.Availability >= settings.Availability && ch.AvailabilityUpdatedAt > DateTime.Now.AddHours(-settings.AvailabilityUpdatedAtHours)).ToList();
                    SaveToFile(settings.OutputFolder, settings.PlayListAllFilename, allChannels);

                    if (settings.CreateFavorite)
                    {
                        var favoriteChannels = channels.Where(ch => settings.FavoriteChannels.Any(fch => ch.Name.Contains(fch))).ToList();
                        SaveToFile(settings.OutputFolder, settings.PlayListFavoriteFileName, favoriteChannels);
                    }
                }


                Console.WriteLine("Playlist generation completed!");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException!");
                Console.WriteLine("Message :{0} ", e.Message);
                Console.WriteLine(e.ToString());
            }
        }

        private static void SaveToFile(string path, string fileName, List<Channels> channels)
        {
            var filePath = Path.Combine(path, fileName);
            using var writer = File.CreateText(filePath);
            writer.WriteLine("#EXTM3U");
            channels.ForEach(ch =>
            {
                writer.WriteLine($"#EXTINF:-1,{ch.Name}");
                writer.WriteLine($"infohash://{ch.Infohash}");
            });
        }
    }

    public class Channels
    {
        [JsonPropertyName("infohash")] public string Infohash { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("availability")] public decimal Availability { get; set; }

        [JsonPropertyName("categories")] public string[] Categories { get; set; }

        [JsonPropertyName("availability_updated_at")]
        [JsonConverter(typeof(ToDateTimeConverter))]
        public DateTime AvailabilityUpdatedAt { get; set; }

    }

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

    public class Settings
    {
        public int AvailabilityUpdatedAtHours { get; set; }
        public decimal Availability { get; set; }
        public bool CreateFavorite { get; set; }
        public string OutputFolder { get; set; }
        public string PlayListAllFilename { get; set; }
        public string PlayListFavoriteFileName { get; set; }
        public string[] FavoriteChannels { get; set; }
    }
}