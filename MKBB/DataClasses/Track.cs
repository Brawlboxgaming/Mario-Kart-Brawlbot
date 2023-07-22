using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class TrackData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string Authors { get; set; }
        public string Version { get; set; }
        public string TrackSlot { get; set; }
        public string MusicSlot { get; set; }
        public decimal SpeedMultiplier { get; set; }
        public int LapCount { get; set; }
        public string SHA1 { get; set; }
        public DateTime LastChanged { get; set; }
        public int TimeTrialPopularity { get; set; }
        public int WWM1 { get; set; }
        public int WWM2 { get; set; }
        public int WWM3 { get; set; }
        public int WWM6 { get; set; }
        public int WWM9 { get; set; }
        public int WWM12 { get; set; }
        public int FM1 { get; set; }
        public int FM2 { get; set; }
        public int FM3 { get; set; }
        public int FM6 { get; set; }
        public int FM9 { get; set; }
        public int FM12 { get; set; }
        public string LeaderboardLink { get; set; }
        public string CategoryName { get; set; }
        public string SlotID { get; set; }
        public string? EasyStaffSHA1 { get; set; }
        public string? ExpertStaffSHA1 { get; set; }
        public bool CustomTrack { get; set; }
        public bool Is200cc { get; set; }

        public OldTrackData ConvertToOld()
        {
            return new OldTrackData()
            {
                ID = ID,
                Name = Name,
                Authors = Authors,
                Version = Version,
                TrackSlot = TrackSlot,
                MusicSlot = MusicSlot,
                SpeedMultiplier = SpeedMultiplier,
                LapCount = LapCount,
                SHA1 = SHA1,
                LastChanged = LastChanged,
                TimeTrialPopularity = TimeTrialPopularity,
                WWM1 = WWM1,
                WWM2 = WWM2,
                WWM3 = WWM3,
                WWM6 = WWM6,
                WWM9 = WWM9,
                WWM12 = WWM12,
                FM1 = FM1,
                FM2 = FM2,
                FM3 = FM3,
                FM6 = FM6,
                FM9 = FM9,
                FM12 = FM12,
                LeaderboardLink = LeaderboardLink,
                CategoryName = CategoryName,
                SlotID = SlotID,
                EasyStaffSHA1 = EasyStaffSHA1,
                ExpertStaffSHA1 = ExpertStaffSHA1,
                CustomTrack = CustomTrack,
                Is200cc = Is200cc
            };
        }

        public int ReturnOnlinePopularity(string month, bool wws, bool frooms)
        {
            if (wws && frooms)
            {
                return month switch
                {
                    "m1" => WWM1 + FM1,
                    "m2" => WWM2 + FM2,
                    "m3" => WWM3 + FM3,
                    "m6" => WWM6 + FM6,
                    "m9" => WWM9 + FM9,
                    "m12" => WWM12 + FM12,
                    _ => -1,
                };
            }
            else if (wws)
            {
                return month switch
                {
                    "m1" => WWM1,
                    "m2" => WWM2,
                    "m3" => WWM3,
                    "m6" => WWM6,
                    "m9" => WWM9,
                    "m12" => WWM12,
                    _ => -1,
                };
            }
            return month switch
            {
                "m1" => FM1,
                "m2" => FM2,
                "m3" => FM3,
                "m6" => FM6,
                "m9" => FM9,
                "m12" => FM12,
                _ => -1,
            };
        }
        public int ReturnOnlineFroomPopularity(string month)
        {
            return month switch
            {
                "m1" => FM1,
                "m2" => FM2,
                "m3" => FM3,
                "m6" => FM6,
                "m9" => FM9,
                "m12" => FM12,
                _ => -1,
            };
        }
    }

    public class OldTrackData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string Authors { get; set; }
        public string Version { get; set; }
        public string TrackSlot { get; set; }
        public string MusicSlot { get; set; }
        public decimal SpeedMultiplier { get; set; }
        public int LapCount { get; set; }
        public string SHA1 { get; set; }
        public DateTime LastChanged { get; set; }
        public int TimeTrialPopularity { get; set; }
        public int WWM1 { get; set; }
        public int WWM2 { get; set; }
        public int WWM3 { get; set; }
        public int WWM6 { get; set; }
        public int WWM9 { get; set; }
        public int WWM12 { get; set; }
        public int FM1 { get; set; }
        public int FM2 { get; set; }
        public int FM3 { get; set; }
        public int FM6 { get; set; }
        public int FM9 { get; set; }
        public int FM12 { get; set; }
        public string LeaderboardLink { get; set; }
        public string CategoryName { get; set; }
        public string SlotID { get; set; }
        public string? EasyStaffSHA1 { get; set; }
        public string? ExpertStaffSHA1 { get; set; }
        public bool CustomTrack { get; set; }
        public bool Is200cc { get; set; }
    }
}