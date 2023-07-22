using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class GBTrackData
    {
        [Key] public int ID { get; set; }
        public string Name { get; set; }
        public string SHA1s { get; set; }
    }
    public class GBTimeData
    {
        [Key] public int ID { get; set; }
        public string TrackSHA1 { get; set; }
        public string Player { get; set; }
        public string? URL { get; set; }
        public string? Comments { get; set; }
    }
}
