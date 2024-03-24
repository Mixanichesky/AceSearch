using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AceSearch
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var settings = GetSettings(args != null && args.Any() ? args[0] : null);
            var playListEngine = new PlayListEngine(settings);
            List<Channel> channels;

            if (settings.UseExternalAceSearch)
                channels = await playListEngine.GetChannelsFromExternalSource();
            else
                channels = await playListEngine.GetChannelsFromInternalSource();

            var allChannels = channels.Where(ch =>
                    ch.Availability >= settings.Availability && ch.AvailabilityUpdatedAt >
                    DateTime.Now.AddHours(-settings.AvailabilityUpdatedAtHours))
                .GroupBy(ch => ch.Name).Select(gr => gr.First()).OrderBy(ch => ch.Name).ToList();

            if (settings.LinkToBroadcastById) await playListEngine.CreateUrlsById(allChannels);
            else playListEngine.CreateUrlsByInfoHash(allChannels);


            await playListEngine.SaveToFile(settings.PlayListAllFilename, allChannels);

            if (settings.CreateFavorite)
            {
                var favoriteChannels = playListEngine.GetFavoriteChannels(allChannels);
                await playListEngine.SaveToFile(settings.PlayListFavoriteFileName, favoriteChannels);
            }


            Console.WriteLine("Playlist generation completed!");
        }

        private static Settings GetSettings(string extPath)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var configName = "appsettings.json";
            if (!string.IsNullOrEmpty(extPath))
            {
                baseDirectory = Path.GetDirectoryName(extPath);
                if (string.IsNullOrEmpty(baseDirectory)) baseDirectory = AppContext.BaseDirectory;
                configName = Path.GetFileName(extPath);
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(baseDirectory)
                .AddJsonFile(configName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var settings = new Settings();
            configuration.Bind(settings);
            return settings;
        }
    }
}