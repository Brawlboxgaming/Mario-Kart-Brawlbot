using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class CouncilMemberData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string DiscordID { get; set; }
        public int ThreadStrikes { get; set; }
        public int Strikes { get; set; }
        public int CompletedHW { get; set; }
        public int MissedHW { get; set; }
    }
}