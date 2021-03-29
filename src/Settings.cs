namespace AceSearch
{
    public class Settings
    {
        public int AvailabilityUpdatedAtHours { get; set; }
        public decimal Availability { get; set; }
        public bool CreateFavorite { get; set; }
        public bool CreateJson { get; set; }
        public string OutputFolder { get; set; }
        public string PlayListAllFilename { get; set; }
        public string PlayListFavoriteFileName { get; set; }
        public string UrlTemplate { get; set; }
        public string FavoriteChannels { get; set; }
    }
}