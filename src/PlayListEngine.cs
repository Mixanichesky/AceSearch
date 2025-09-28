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

namespace AceSearch{;

public class PlayListEngine
{
    private static readonly SemaphoreSlim Semaphore = new(10);
    private readonly Settings _settings;

    public PlayListEngine(Settings settings)
    {
        _settings = settings;
    }

    public async Task<List<Channel>> GetChannelsFromExternalSource()
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var json = await client.GetStringAsync(_settings.ExternalAceSearchUrl);
        return JsonSerializer.Deserialize<Channel[]>(json).ToList();
    }

    public async Task<List<Channel>> GetChannelsFromInternalSource()
    {
        var url = $"http://127.0.0.1:{_settings.AceStreamEnginePort}/search?&page_size=200000&page=0&query=";
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        var json = await client.GetStringAsync(url);
        var jsonObj = JObject.Parse(json);

        var results = jsonObj["result"]?["results"] ?? new JArray();

        var channelStrings = results
            .Select(r =>
            {
                var jChannel = r["items"]?[0];
                var iconUrl = r["icons"]?[0]?["url"];
                if (iconUrl != null && jChannel != null) jChannel["IconUrl"] = iconUrl;
                return jChannel?.ToString() ?? string.Empty;
            })
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var jsonChannels = "[\n" + string.Join(",\n", channelStrings) + "\n]";
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
        var filePath = Path.Combine(_settings.OutputFolder, fileName);
        await using var writer = File.CreateText(filePath);
        await writer.WriteLineAsync("#EXTM3U");

        channels.ForEach(ch =>
        {
            var headers = new List<string> { "#EXTINF:-1" };
            if (_settings.AddCategories && ch.Categories != null && ch.Categories.Any())
                ch.Categories.Where(c => !string.IsNullOrEmpty(c?.Trim())).ToList()
                    .ForEach(c => headers.Add($"group-title={c.Trim()}"));

            if (_settings.AddIcons && !string.IsNullOrEmpty(ch.IconUrl?.Trim()))
                headers.Add($"tvg-logo={ch.IconUrl.Trim()}");

            var header = string.Join(" ", headers);
            writer.WriteLine($"{header}, {ch.Name}");
            writer.WriteLine(ch.Url);
        });

        if (_settings.CreateJson)
            await SaveToJsonFile(fileName, channels);
    }

    public List<Channel> GetFavoriteChannels(List<Channel> channels)
    {
        var fChannelsList = string.Join(", ", _settings.FavoriteChannels).Split(",")
            .Select(fch => fch.Trim()).ToList();
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
                url = _settings.LinkToBroadcastById ? ch.ChannelId : ch.InfoHash,
                cat = !string.IsNullOrEmpty(cat) ? cat : "none"
            };
        }).ToArray();

        var jsonFileName = Path.Combine(_settings.OutputFolder,
            Path.GetFileNameWithoutExtension(fileName) + ".json");

        await using var jsonWriter = File.Create(jsonFileName);
        await JsonSerializer.SerializeAsync(jsonWriter, new { channels = chs },
            new JsonSerializerOptions
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
                $"http://127.0.0.1:{_settings.AceStreamEnginePort}/server/api?method=get_content_id&infohash={infoHash}";

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
}}
