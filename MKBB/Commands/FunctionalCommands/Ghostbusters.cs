using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MKBB.Class;
using MKBB.Data;
using Newtonsoft.Json;
using System.Net;

namespace MKBB.Commands
{
    public class Ghostbusters : ApplicationCommandModule
    {
        [SlashCommand("gbaddtrack", "Adds a new track for Ghostbusters to set times on.")]
        public static async Task AddNewGBTrack(InteractionContext ctx,
            [Option("track-name", "The name of the track to add.")] string track,
            [Option("track-id", "The id of the track (also known as the SHA1).")] string trackId)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();

                foreach (GBTrackData gbTrack in dbCtx.GBTracks.ToList())
                {
                    if (Util.CompareStrings(gbTrack.Name, track))
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = "*The track is already added.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }));
                        return;
                    }
                }
                dbCtx.GBTracks.Add(new GBTrackData()
                {
                    Name = track,
                    SHA1s = trackId.ToUpperInvariant()
                });
                await dbCtx.SaveChangesAsync();

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Success:**__",
                    Description = $"*{track} has been added successfully.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                DiscordMessage message = await ctx.Client
                    .GetGuildAsync(180306609233330176).Result
                    .GetChannel(1118995806754721853)
                    .GetMessageAsync(1119015755736961164);

                string description = "";
                foreach (GBTrackData t in dbCtx.GBTracks)
                {
                    description += $"* *{t.Name}*\n";
                }
                description = description == "" ? "*No tracks currently assigned.*" : description.Trim('\n');

                embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "__**Current Ghostbusters Tracks:**__",
                    Description = description,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await message.ModifyAsync(new DiscordMessageBuilder().AddEmbed(embed));

            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("gbaddsha1", "Adds an additional SHA1 for multiple versions of the same track.")]
        public static async Task EditNewGBTrack(InteractionContext ctx,
            [Option("track-name", "The name of the track you want to edit.")] string track,
            [Option("track-id", "The new id of the track (also known as the SHA1).")] string newTrackId)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();

                foreach (GBTrackData gbTrack in dbCtx.GBTracks.ToList())
                {
                    if (Util.CompareStrings(gbTrack.Name, track))
                    {
                        if (gbTrack.SHA1s != "")
                        {
                            gbTrack.SHA1s += ",,";
                        }
                        gbTrack.SHA1s += newTrackId.ToUpperInvariant();
                        await dbCtx.SaveChangesAsync();
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Success:**__",
                            Description = $"*The new track ID has been added successfully.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__*Error:*__",
                    Description = $"*{track} could not be found. Please make sure to specify the exact name.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("gbremovesha1", "Removes a SHA1 from a Ghostbusters track.")]
        public static async Task RemoveGBSHA1(InteractionContext ctx,
            [Option("track-name", "The name of the track.")] string track,
            [Option("track-id", "The id you want to remove (also known as the SHA1).")] string trackId)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();

                foreach (GBTrackData gbTrack in dbCtx.GBTracks.ToList())
                {
                    if (Util.CompareStrings(gbTrack.Name, track))
                    {
                        gbTrack.SHA1s = gbTrack.SHA1s.Replace($"{trackId.ToUpperInvariant()}", "").Replace(",,,,", ",,");
                        if (gbTrack.SHA1s == ",,")
                        {
                            gbTrack.SHA1s = "";
                        }
                        await dbCtx.SaveChangesAsync();
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Success:**__",
                            Description = $"*The SHA1 for {track} has been removed successfully.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }
                }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Error:**__",
                    Description = $"*{track} could not be found. Please make sure to specify the exact name.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("gbremovetrack", "Removes a track for Ghostbusters.")]
        public static async Task RemoveGBTrack(InteractionContext ctx,
            [Option("track-name", "The name of the track you want to remove.")] string track)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();

                foreach (GBTrackData gbTrack in dbCtx.GBTracks.ToList())
                {
                    if (Util.CompareStrings(gbTrack.Name, track))
                    {
                        dbCtx.GBTracks.Remove(gbTrack);
                        foreach (GBTimeData time in dbCtx.GBTimes.Where(x=>gbTrack.SHA1s.Contains(x.TrackSHA1)))
                        {
                            dbCtx.GBTimes.Remove(time);
                        }
                        await dbCtx.SaveChangesAsync();
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__*Success:*__",
                            Description = $"*{track} has been removed successfully.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                        DiscordMessage message = await ctx.Client
                            .GetGuildAsync(180306609233330176).Result
                            .GetChannel(1118995806754721853)
                            .GetMessageAsync(1119015755736961164);

                        string description = "";
                        foreach (GBTrackData t in dbCtx.GBTracks)
                        {
                            description += $"* *{t.Name}*\n";
                        }
                        description = description == "" ? "*No tracks currently assigned.*" : description.Trim('\n');

                        embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Current Ghostbusters Tracks:**__",
                            Description = description,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await message.ModifyAsync(new DiscordMessageBuilder().AddEmbed(embed));
                        return;
                    }
                }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__*Error:*__",
                    Description = $"*{track} could not be found. Please make sure to specify the exact name.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("gbtimes", "Gets a time of a player on a track as part of a Ghostbuster submission.")]
        public static async Task GetGBTimes(InteractionContext ctx,
            [Option("player", "The user of the player you want to find a time of.")] DiscordUser player)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });

                using MKBBContext dbCtx = new();

                List<GBTimeData> gbTimes = dbCtx.GBTimes.Where(x => x.Player == player.Id.ToString() && x.URL != null).ToList();
                if (gbTimes.Count == 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Notice:**__",
                        Description = $"*No times could be found for {player.Mention}.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));
                    return;
                }
                List<DiscordEmbed> embeds = new();
                foreach (GBTimeData time in gbTimes)
                {
                    WebClient webClient = new();
                    Ghost ghost = JsonConvert.DeserializeObject<Ghost>(await webClient.DownloadStringTaskAsync(time.URL.Replace("html", "json")));
                    ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(await webClient.DownloadStringTaskAsync(time.URL.Replace("html", "json")));
                    string controllerId = (ghost.ControllerID != 0 && ghost.ControllerID != 1 && ghost.ControllerID != 2 && ghost.ControllerID != 3) ? "???" : Util.Controllers[ghost.ControllerID];

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Ghostbusters Times:**__",
                        Description = $"<@{player.Id}>'s time on {ghost.TrackName}:\n\n" +
                        $"**Time:** {ghost.FinishTimeSimple}\n\n" +
                        $"**Splits:** {string.Join(" - ", ghost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                        $"**Combo:** {Util.Characters[ghost.DriverID]} on {Util.Vehicles[ghost.VehicleID]}\n\n" +
                        $"**Date Set:** {ghost.DateSet.Split('T')[0]}\n\n" +
                        $"**Controller:**\n{controllerId}\n\n" +
                        $"**Extra Details:**\n" +
                        $"*Exact Finish Time: {ghost.FinishTime}*\n\n" +
                        $"*Exact Splits: {string.Join(" - ", ghost.ExtraInfo.Splits.ToArray())}*\n\n" +
                        $"*Comments: {(time.Comments == null ? "No comments" : time.Comments)}*",
                        Url = time.URL.Replace("json", "html"),
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    embeds.Add(embed);
                }

                DiscordWebhookBuilder builder = new DiscordWebhookBuilder().AddEmbed(embeds[0]);

                if (embeds.Count > 1)
                {
                    builder.AddComponents(Util.GeneratePageArrows());
                }

                var message = await ctx.EditResponseAsync(builder);

                if (embeds.Count > 1)
                {
                    PendingPagesInteraction pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = embeds };

                    Util.PendingPageInteractions.Add(pending);
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }
    }
}