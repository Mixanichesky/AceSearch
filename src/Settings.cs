using System.Text.Json.Serialization;

namespace AceSearch
{
    public class Settings
    {
        public int AvailabilityUpdatedAtHours { get; set; }
        public decimal Availability { get; set; }
        public bool CreateFavorite { get; set; }
        public bool CreateJson { get; set; }

        public bool UseExternalAceSearch { get; set; }

        public bool LinkToBroadcastById { get; set; }
        public string ExternalAceSearchUrl { get; set; }
        public string OutputFolder { get; set; }
        public string PlayListAllFilename { get; set; }
        public string PlayListFavoriteFileName { get; set; }

        public long AceStreamEnginePort { get; set; }
        public string[] FavoriteChannels { get; set; }

        public bool AddCategories { get; set; }

        public bool AddIcons { get; set; }
    }
}