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
        return Json
