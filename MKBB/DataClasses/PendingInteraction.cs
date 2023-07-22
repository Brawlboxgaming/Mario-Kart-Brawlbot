using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace MKBB.Data
{
    public class PendingPagesInteraction
    {
        public ulong MessageId { get; set; }
        public InteractionContext Context { get; set; }
        public List<List<DiscordEmbed>> Categories { get; set; }
        public List<DiscordEmbed> Pages { get; set; }
        public List<string> CategoryNames { get; set; }
        public int CurrentCategory { get; set; }
        public int CurrentPage { get; set; }
    }
    public class PendingChannelConfigInteraction
    {
        public ulong MessageId { get; set; }
        public InteractionContext Context { get; set; }
    }
}