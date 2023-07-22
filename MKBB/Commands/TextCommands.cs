using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MKBB.Class;

namespace MKBB.Commands
{
    public class TextCommands : ApplicationCommandModule
    {
        [SlashCommand("help", "Gives a list of commands available.")]
        public async Task Help(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
            string description = "__**Standard Commands:**__" +
                "\n/cttp" +
                "\n/help" +
                "\n/info track" +
                "\n/issues track" +
                "\n/nextupdate" +
                "\n/pb track player (engine-class)" +
                "\n/pop rts/cts/track (stat-duration) (online/tts)" +
                "\n/rating track" +
                "\n/register chadsoft-link" +
                "\n/source" +
                "\n/staff track (engine-class)" +
                "\n/stars" +
                "\n/summary track" +
                "\n/tools name" +
                "\n/top10 track (vehicle) (engine-class)" +
                "\n/servertop10 track (vehicle) (engine-class)";

            if (ctx.Guild.Id == 180306609233330176 && Util.CheckEphemeral(ctx))
            {
                foreach (var role in ctx.Member.Roles)
                {
                    if (role.Name == "Ghostbusters" || ctx.Member.Id == 105742694730457088)
                    {
                        description += "\n\n__**Ghostbusters Commands:**__" +
                            "\n/gbtimes player";
                        break;
                    }
                }
                /*foreach (var role in ctx.Member.Roles)
                {
                    if (role.Name == "CT Creator" || ctx.Member.Id == 105742694730457088)
                    {
                        description += "\n\n__**Creator Commands:**__" +
                            "\n/pinmessage message (file)";
                        break;
                    }
                }*/
                foreach (var role in ctx.Member.Roles)
                {
                    if (role.Name == "Track Council" || ctx.Member.Id == 105742694730457088)
                    {
                        description += "\n\n__**Council Commands:**__" +
                            "\n/assignedthreads" +
                            "\n/gethw track member" +
                            "\n/getthreadhw track member" +
                            "\n/hw" +
                            "\n/strikes member" +
                            "\n/threadstrikes member" +
                            "\n/submithw yes/fixes/neutral/no track feedback" +
                            "\n/submitthreadhw track feedback";
                        break;
                    }
                }
                foreach (var role in ctx.Member.Roles)
                {
                    if (role.Name == "Admin" || ctx.Member.Id == 105742694730457088)
                    {
                        {
                            description += "\n\n__**Admin Commands:**__" +
                                "\n/addhw track author version download-link slot-filename speed/lap-modifiers notes" +
                                "\n/addtool name creators description download" +
                                "\n/allstrikes" +
                                "\n/botchannel no-channels" +
                                "\n/delhw track" +
                                "\n/deltool name" +
                                "\n/edittool oldname name creators description download" +
                                "\n/gbaddsha1 sha1" +
                                "\n/gbaddtrack track-name sha1" +
                                "\n/gbremovesha1 track-name" +
                                "\n/gbremovetrack" +
                                "\n/uploadtestpack test-pack-zip";
                            break;
                        }
                    }
                }
            }
            else if (ctx.Member.Permissions.HasPermission(Permissions.Administrator) && Util.CheckEphemeral(ctx))
            {
                description += "\n\n__**Admin Commands:**__" +
                            "\n/botchannel no-channels";
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "__**Help**__",
                Description = description,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                }
            };

            var trackTestersInvite = new DiscordLinkButtonComponent("https://discord.gg/sjPzuJ7PwD", "CTGP Track Testers Invite");

            var botInvite = new DiscordLinkButtonComponent("https://discord.com/api/oauth2/authorize?client_id=933390786266017884&permissions=294205385728&scope=bot", "Bot Invite");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(trackTestersInvite, botInvite));
        }

        [SlashCommand("cttp", "Gives links relating to the Custom Track Test Pack.")]
        public async Task CTTP(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "__**Custom Track Test Pack Links**__",
                Description = "**Base Pack:** *https://drive.google.com/file/d/1tzhaBxycHEvY5G2k5jTzpONBhmq6g08g/view?usp=sharing*" +
                "\n**Installation Tutorial:** *https://youtu.be/Vh3GXTbxHLU*" +
                "\n**Test Pack Creation Tutorial:** *https://youtu.be/igtfmZAyG3g*",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                }
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("sheets", "Gives the links to useful spreadsheets.")]
        public async Task Sheets(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
            string description = "**Backroom Page:** *This page has all of the information on our finalized updates, and is viewable to the public. The first tab outlines what the next few updates will look like, and the second tab contains all the accepted tracks, with the fixes needed (if any).*" +
                "\n\n**Testing Notes Page:** *This page is what we fill when doing the online tests. It also contains the downloads for all the tracks in the test.*";
            var message = new DiscordWebhookBuilder();
            bool admin = false;
            bool councilMember = false;
            if (Util.CheckEphemeral(ctx))
            {
                foreach (var role in ctx.Member.Roles)
                {
                    if (role.Id == 228909597090512896 && ctx.Channel.Id != 908709951411716166)
                    {
                        admin = true;
                    }
                    if ((role.Id == 608386209655554058 || role.Id == 228909597090512896) && ctx.Channel.Id != 908709951411716166)
                    {
                        councilMember = true;
                    }
                    if (ctx.Member.Id == 105742694730457088)
                    {
                        admin = true;
                        councilMember = true;
                    }
                    if (councilMember && admin)
                    {
                        break;
                    }
                }
            }
            List<DiscordLinkButtonComponent> buttons = new List<DiscordLinkButtonComponent> {
                Util.GetBackroomLinkButton(),
                Util.GetTestingNotesLinkButton()
            };
            if (councilMember)
            {
                description += $"\n\n**Council Page:** *This page has multiple important things for the Council to keep track of; this is where members review tracks that are submitted. This is also where we vote for members when they apply.*";
                buttons.Add(Util.GetCouncilLinkButton());
            }
            if (admin)
            {
                description += $"\n\n**Admin Page:** *This page is for admin information.*";
                buttons.Add(Util.GetAdminLinkButton());
            }
            message.AddComponents(buttons);

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "__**Useful Google Sheets Pages**__",
                Description = description,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                }
            };

            await ctx.EditResponseAsync(message.AddEmbed(embed));
        }

        [SlashCommand("source", "Gives a link to the source code of this bot.")]
        public async Task SourceCode(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "__**Source Code:**__",
                Description = "**Github:** *https://github.com/Brawlboxgaming/Mario-Kart-Brawlbot*",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                }
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
