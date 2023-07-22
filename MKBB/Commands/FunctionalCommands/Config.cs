using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MKBB.Class;
using MKBB.Data;

namespace MKBB.Commands
{
    public class Config : ApplicationCommandModule
    {

        [SlashCommand("botchannel", "Configures the channel(s) in which commands will no longer be ephemeral (requires admin).")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public static async Task ConfigureBotChannel(InteractionContext ctx,
            [Choice("True", 1)]
            [Choice("False", 0)]
            [Option("no-channels", "If you would like no channels configured, set this to true.")] bool noChannels = false)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });

                if (noChannels)
                {
                    using MKBBContext dbCtx = new();
                    List<ServerData> servers = dbCtx.Servers.ToList();
                    foreach (ServerData server in servers)
                    {
                        if (ctx.Guild.Id == server.ServerID)
                        {
                            server.BotChannelIDs = null;
                            break;
                        }
                    }

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Success:**__",
                        Description = $"*The server's bot channels have been set to none.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };

                    DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Choose your channels:**__",
                        Description = $"*Please select one or more channels from the drop down menu below.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };

                    DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(Util.GenerateChannelConfigSelectMenu()));

                    PendingChannelConfigInteraction pending = new() { Context = ctx, MessageId = message.Id };
                    Util.PendingChannelConfigInteractions.Add(pending);
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }
    }
}
