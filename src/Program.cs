using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
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

                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback += (sender, certificate, chain, errors) => true;

                using (var client = new HttpClient(handler))
                {
                    var json = await client.GetStringAsync(
                        "http://search.acestream.net/all?api_version=1.0&api_key=test_api_key");

                    var channels = JsonSerializer.Deserialize<Channels[]>(json);
                    var allChannels = channels.Where(ch =>
                        ch.Availability >= settings.Availability && ch.AvailabilityUpdatedAt > DateTime.Now.AddHours(-settings.AvailabilityUpdatedAtHours)).ToList();
                    await SaveToFile(settings.OutputFolder, settings.PlayListAllFilename, allChannels, settings.CreateJson);

                    if (settings.CreateFavorite)
                    {
                        var favoriteChannels = allChannels.Where(ch => settings.FavoriteChannels.Any(fch => ch.Name.Contains(fch))).ToList();
                        await SaveToFile(settings.OutputFolder, settings.PlayListFavoriteFileName, favoriteChannels, settings.CreateJson);
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

        private static async Task SaveToFile(string path, string fileName, List<Channels> channels, bool createJson)
        {
            var filePath = Path.Combine(path, fileName);
            await using var writer = File.CreateText(filePath);
            writer.WriteLine("#EXTM3U");
            channels.ForEach(ch =>
            {
                writer.WriteLine($"#EXTINF:-1,{ch.Name}");
                writer.WriteLine($"infohash://{ch.Infohash}");
            });

            if (createJson)
            {
                var chs = channels.Select(ch => new
                {
                    name = ch.Name,
                    url = ch.Infohash,
                    cat = ch.Categories != null && ch.Categories.Any() ? ch.Categories.First() : "none"

                }).ToArray();

                var jsonFileName = Path.Combine(path, Path.GetFileNameWithoutExtension(fileName) + ".json");

                await using var jsonWriter = File.Create(jsonFileName);
                await JsonSerializer.SerializeAsync(jsonWriter, new { channels = chs },
                    new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            }
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
        public bool CreateJson { get; set; }
        public string OutputFolder { get; set; }
        public string PlayListAllFilename { get; set; }
        public string PlayListFavoriteFileName { get; set; }
        public string[] FavoriteChannels { get; set; }
    }
}