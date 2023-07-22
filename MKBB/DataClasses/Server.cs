using System.ComponentModel.DataAnnotations;

namespace MKBB.Data
{
    public class ServerData
    {
        [Key] public int ID { get; set; }
        public ulong ServerID { get; set; }
        public string Name { get; set; }
        public string? BotChannelIDs { get; set; }
    }
}
