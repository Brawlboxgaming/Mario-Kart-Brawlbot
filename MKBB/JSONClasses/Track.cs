using MKBB.Data;
using Newtonsoft.Json;

namespace MKBB.Class
{
    public class Track
    {
        [JsonProperty("_links")]
        public Link LinkContainer { get; set; }
        [JsonProperty("categoryId")]
        public int? Category { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("trackId")]
        public string SHA1 { get; set; }
        [JsonProperty("fastestTimeSimple")]
        public string BestTime { get; set; }
        [JsonProperty("lastChanged")]
        public DateTime TrackAdded { get; set; }
        public string WiimmfiName { get; set; }
        [JsonProperty("popularity")]
        public int TimeTrialScore { get; set; }
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int M3 { get; set; }
        public int M6 { get; set; }
        public int M9 { get; set; }
        public int M12 { get; set; }
        public string LeaderboardLink { get; set; }
        public string CategoryName { get; set; }
        public string SlotID { get; set; }
        public bool CustomTrack { get; set; }
        [JsonProperty("200cc")]
        public bool Is200cc { get; set; }

        public TrackData ConvertData()
        {
            return new TrackData()
            {
                Name = Name,
                SHA1 = SHA1,
                LastChanged = TrackAdded,
                TimeTrialPopularity = TimeTrialScore,
                WWM1 = M1,
                WWM2 = M2,
                WWM3 = M3,
                WWM6 = M6,
                WWM9 = M9,
                WWM12 = M12,
                LeaderboardLink = LeaderboardLink,
                CategoryName = CategoryName,
                SlotID = SlotID,
                CustomTrack = CustomTrack,
                Is200cc = Is200cc
            };
        }

        public int ReturnOnlinePopularity(string month)
        {
            switch (month)
            {
                case "m1":
                    return M1;
                case "m2":
                    return M2;
                case "m3":
                    return M3;
                case "m6":
                    return M6;
                case "m9":
                    return M9;
                case "m12":
                    return M12;
            }
            return -1;
        }
    }

    public class LeaderboardInfo
    {
        [JsonProperty("leaderboards")]
        public List<Track> Leaderboard { get; set; }
    }

    public class Link
    {
        [JsonProperty("item")]
        public Href Href { get; set; }
    }

    public class Href
    {
        [JsonProperty("href")]
        public string URL { get; set; }
    }
}