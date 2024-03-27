using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AceSearch;

public class PlayListEngine(Settings settings)
{
    private static readonly SemaphoreSlim Semaphore = new(10);

    public async Task<List<Channel>> GetChannelsFromExternalSource()
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var json = await client.GetStringAsync(settings.ExternalAceSearchUrl);
        return JsonSerializer.Deserialize<Channel[]>(json).ToList();
    }

    public async Task<List<Channel>> GetChannelsFromInternalSource()
    {
        var url = $"http://127.0.0.1:{settings.AceStreamEnginePort}/search?&page_size=200000&page=0&query=";
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var json = await client.GetStringAsync(url);
        var jsonObj = JObject.Parse(json);
        var jsonChannels =
            $"[\n{string.Join(",\n", jsonObj["result"]?["results"]?.Select(r => {
                var jChannel = r["items"]?[0];
                var iconUrl = r["icons"]?[0]?["url"];
                if (iconUrl != null && jChannel != null) jChannel["IconUrl"] = iconUrl;
                return jChannel?.ToString();
            }) ?? Array.Empty<string>())}\n]";
        var channels = JsonSerializer.Deserialize<Channel[]>(jsonChannels).ToList();


        return channels;
    }

    public void CreateUrlsByInfoHash(List<Channel> channels)
    {
        channels.AsParallel().ForAll(ch => ch.Url = $"infohash://{ch.InfoHash}");
    }

    public Task CreateUrlsById(List<Channel> channels)
    {
        return Task.WhenAll(channels.Select(async ch =>
        {
            var chanelId = await GetChanelId(ch.InfoHash);
            ch.ChannelId = chanelId;
            ch.Url = $"acestream://{chanelId}";
        }));
    }

    public async Task SaveToFile(string fileName, List<Channel> channels)
    {
        var filePath = Path.Combine(settings.OutputFolder, fileName);
        await using var writer = File.CreateText(filePath);
        await writer.WriteLineAsync("#EXTM3U");

        channels.ForEach(ch =>
        {
            var headers = new List<string>();
            headers.Add("#EXTINF:-1");
            if (settings.AddCategories && ch.Categories != null && ch.Categories.Any())
                ch.Categories.Where(c => !string.IsNullOrEmpty(c?.Trim())).ToList()
                    .ForEach(c => headers.Add($"group-title={c.Trim()}"));


            if (settings.AddIcons && !string.IsNullOrEmpty(ch.IconUrl?.Trim()))
                headers.Add($"tvg-logo={ch.IconUrl.Trim()}");

            var header = string.Join(" ", headers);
            writer.WriteLine($"{header}, {ch.Name}");
            writer.WriteLine(ch.Url);
        });

        if (settings.CreateJson)
            await SaveToJsonFile(fileName, channels);
    }

    public List<Channel> GetFavoriteChannels(List<Channel> channels)
    {
        var fChannelsList = string.Join(", ", settings.FavoriteChannels).Split(",")
            .Select(fch => fch.Trim()).ToList();
        // var favoriteChannels = allChannels.Where(ch => fChannelsList.Any(fch => ch.Name.Contains(fch))).OrderBy(ch => ch.Name).ToList();
        var favoriteChannels = channels.Where(ch => fChannelsList.Any(fch => ch.Name == fch))
            .OrderBy(ch => ch.Name).ToList();
        return favoriteChannels;
    }

    private async Task SaveToJsonFile(string fileName, List<Channel> channels)
    {
        var chs = channels.Select(ch =>
        {
            var cat = ch.Categories != null && ch.Categories.Any() ? ch.Categories.First() : "none";
            return new
            {
                name = ch.Name,
                url = settings.LinkToBroadcastById ? ch.ChannelId : ch.InfoHash,
                cat = !string.IsNullOrEmpty(cat) ? cat : "none"
            };
        }).ToArray();

        var jsonFileName = Path.Combine(settings.OutputFolder,
            Path.GetFileNameWithoutExtension(fileName) + ".json");

        await using var jsonWriter = File.Create(jsonFileName);
        await JsonSerializer.SerializeAsync(jsonWriter, new { channels = chs },
            new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
    }

    private async Task<string> GetChanelId(string infoHash)
    {
        string chanelId = string.Empty;
        await Semaphore.WaitAsync();
        try
        {
            var url =
                $"http://127.0.0.1:{settings.AceStreamEnginePort}/server/api?method=get_content_id&infohash={infoHash}";

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler);
            var json = await client.GetStringAsync(url);

            var jsonObj = JObject.Parse(json);
            chanelId = Convert.ToString(jsonObj["result"]?["content_id"]);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            Semaphore.Release();
        }

        return chanelId;
    }
}