using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace AceSearch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var baseDirectory = AppContext.BaseDirectory;
                var configName = "appsettings.json";
                if (args != null && args.Any())
                {
                    baseDirectory = Path.GetDirectoryName(args[0]);
                    if (string.IsNullOrEmpty(baseDirectory)) baseDirectory = AppContext.BaseDirectory;
                    configName = Path.GetFileName(args[0]);
                }

                var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDirectory)
                    .AddJsonFile(configName, optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

                var configuration = builder.Build();
                var settings = new Settings();
                configuration.Bind(settings);

                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                using var client = new HttpClient(handler);
                var json = await client.GetStringAsync(
                    "https://api.acestream.me/all?api_version=1.0&api_key=test_api_key");


                var channels = JsonSerializer.Deserialize<Channels[]>(json);

                var allChannels = channels.Where(ch =>
                        ch.Availability >= settings.Availability && ch.AvailabilityUpdatedAt >
                        DateTime.Now.AddHours(-settings.AvailabilityUpdatedAtHours))
                    .GroupBy(ch => ch.Name).Select(gr => gr.First()).OrderBy(ch => ch.Name).ToList();

                await SaveToFile(settings.OutputFolder, settings.PlayListAllFilename, allChannels, settings.CreateJson,
                    settings.UrlTemplate);

                if (settings.CreateFavorite)
                {
                    var fChannelsList = string.Join(", ", settings.FavoriteChannels).Split(",")
                        .Select(fch => fch.Trim()).ToList();
                    // var favoriteChannels = allChannels.Where(ch => fChannelsList.Any(fch => ch.Name.Contains(fch))).OrderBy(ch => ch.Name).ToList();
                    var favoriteChannels = allChannels.Where(ch => fChannelsList.Any(fch => ch.Name == fch))
                        .OrderBy(ch => ch.Name).ToList();
                    await SaveToFile(settings.OutputFolder, settings.PlayListFavoriteFileName, favoriteChannels,
                        settings.CreateJson, settings.UrlTemplate);

                    if (settings.CreateAceStreamFormatPlayList)
                        await SaveToAceFormatFile(settings.OutputFolder, settings.PlayListFavoriteFileNameAsFormat,
                            favoriteChannels, settings.AceStreamEngineUrl);
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

        private static async Task SaveToFile(string path, string fileName, List<Channels> channels, bool createJson,
            string urlTemplate)
        {
            var filePath = Path.Combine(path, fileName);
            await using var writer = File.CreateText(filePath);
            await writer.WriteLineAsync("#EXTM3U");

            channels.ForEach(ch =>
            {
                var url = string.Format(urlTemplate, ch.InfoHash);
                writer.WriteLine($"#EXTINF:-1,{ch.Name}");
                writer.WriteLine(url);
            });

            if (createJson)
                await SaveToJsonFile(path, fileName, channels);
        }

        private static async Task SaveToJsonFile(string path, string fileName, List<Channels> channels)
        {
            var chs = channels.Select(ch =>
            {
                var cat = ch.Categories != null && ch.Categories.Any() ? ch.Categories.First() : "none";
                return new
                {
                    name = ch.Name,
                    url = ch.InfoHash,
                    cat = !string.IsNullOrEmpty(cat) ? cat : "none"
                };
            }).ToArray();

            var jsonFileName = Path.Combine(path, Path.GetFileNameWithoutExtension(fileName) + ".json");

            await using var jsonWriter = File.Create(jsonFileName);
            await JsonSerializer.SerializeAsync(jsonWriter, new { channels = chs },
                new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
        }

        private static async Task SaveToAceFormatFile(string path, string fileName, List<Channels> channels,
            string urlEngine)
        {
            var content = (await Task.WhenAll(channels.Select(async ch =>
            {
                var chanelName = ch.Name;
                var chanelId = await GetChanelId(urlEngine, ch.InfoHash);
                var aceUrl = $"acestream://{chanelId}";
                return new { Name = chanelName, AceUrl = aceUrl };
            }))).ToList();

            var filePath = Path.Combine(path, fileName);
            await using var writer = File.CreateText(filePath);
            await writer.WriteLineAsync("#EXTM3U");

            content.ForEach(c =>
            {
                writer.WriteLine($"#EXTINF:-1,{c.Name}");
                writer.WriteLine(c.AceUrl);
            });

            // foreach (var ch in channels)
            // {
            //     var chanelId = await GetChanelId(urlEngine, ch.InfoHash);
            //     var aceUrl = $"acestream://{chanelId}";
            //     await writer.WriteLineAsync($"#EXTINF:-1,{ch.Name}");
            //     await writer.WriteLineAsync(aceUrl);
            // }
            return;
        }

        private static async Task<string> GetChanelId(string urlEngine, string infoHash)
        {
            var url = $"{urlEngine}/server/api?method=get_content_id&infohash={infoHash}";

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler);
            var json = await client.GetStringAsync(url);

            var jsonObj = JObject.Parse(json);
            var chanelId = Convert.ToString(jsonObj["result"]?["content_id"]);
            return chanelId;
        }
    }
}