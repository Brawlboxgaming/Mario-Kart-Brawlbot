﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using HtmlAgilityPack;
using MKBB.Class;
using MKBB.Data;
using Newtonsoft.Json;
using System.Net;

namespace MKBB.Commands
{
    public class TimeTrialManagement : ApplicationCommandModule
    {
        [SlashCommand("register", "Register your Chadsoft account with the bot.")]
        public async Task RegisterNewPlayer(InteractionContext ctx,
            [Option("player-id", "The player ID on your Chadsoft player page.")] string playerId)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });

                bool fail = false;
                string playerUrl = $"https://www.chadsoft.co.uk/time-trials/players/{playerId.Substring(0, 2)}/{playerId.Substring(2)}.json";
                var player = new PlayerData();
                try
                {
                    var webClient = new WebClient();
                    player = JsonConvert.DeserializeObject<PlayerData>(await webClient.DownloadStringTaskAsync(playerUrl));
                    player.PlayerLink = playerUrl;
                    player.DiscordID = ctx.Member.Id;
                }
                catch
                {
                    fail = true;
                }
                if (fail)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = "*Player ID was invalid or doesn't exist. To get your player ID, go to [Chadsoft](https://www.chadsoft.co.uk/time-trials/players.html) and search for your player page, which will have your player ID.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    using var dbCtx = new MKBBContext();
                    List<PlayerData> playerList = dbCtx.Players.ToList();

                    int ix = playerList.FindIndex(x => x.PlayerID == player.PlayerID);
                    int ix2 = playerList.FindIndex(x => x.DiscordID == player.DiscordID);

                    if (ix2 > -1)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Notice:**__",
                            Description = $"*You have already registered a profile. If you think this is a mistake, or would like this amended, please contact <@105742694730457088>.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else if (ix < 0)
                    {
                        dbCtx.Players.Add(player);
                        dbCtx.SaveChanges();

                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Success:**__",
                            Description = $"*Player ID has been registered.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Notice:**__",
                            Description = $"*This player ID has already been registered. If you think this is a mistake, please contact <@105742694730457088>.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("stars", "Gets the stars of the user specified.")]
        public async Task DisplayStars(InteractionContext ctx,
            [Option("player", "Will display the stars of the player requested.")] DiscordUser user)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });

                using var dbCtx = new MKBBContext();
                List<PlayerData> playerList = dbCtx.Players.ToList();

                int ix = playerList.FindIndex(x => x.DiscordID == user.Id);

                if (ix < 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = $"*Player ID has not yet been registered. Please use /register to register your player ID if you haven't already. To get your player ID, go to [Chadsoft](https://www.chadsoft.co.uk/time-trials/players.html) and search for your player page, which will have your player ID.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var webClient = new WebClient();
                    var starsJson = JsonConvert.DeserializeObject<GetStars>(await webClient.DownloadStringTaskAsync(playerList[ix].PlayerLink));

                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Stars of {user.Username}:**__",
                        Description = $"Gold - {starsJson.Stars.Gold}\n" +
                        $"Silver - {starsJson.Stars.Silver}\n" +
                        $"Bronze - {starsJson.Stars.Bronze}",
                        Url = $"{playerList[ix].PlayerLink.Substring(0, playerList[ix].PlayerLink.Length - 5)}.html",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("pb", "Gets the personal best(s) on the track and player specified.")]
        public async Task GetPBs(InteractionContext ctx,
            [Option("track-name", "The track you want to display the personal bests of.")] string track,
            [Option("player", "The Discord user of the player you want to the display the personal bests of.")] DiscordUser user,
            [Choice("Kart", "Kart")]
            [Choice("Bike", "Bike")]
            [Option("vehicle-type", "Gets all ghosts based on a specific vehicle")] string vehicleRestriction = "",
            [Option("200cc", "The engine class of the personal best you want to find .")] bool cc = false)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);
                using var dbCtx = new MKBBContext();

                List<PlayerData> playerList = dbCtx.Players.ToList();
                int playerIx = playerList.FindIndex(x => x.DiscordID == user.Id);

                List<TrackData> trackList = dbCtx.Tracks.Where(x => x.Is200cc == cc).ToList();

                int trackIx = Util.ListNameCheck(trackList, track);
                if (playerIx < 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*{user.Username} has not been registered. To register, use /register with your Chadsoft player ID. To get your player ID, go to [Chadsoft](https://www.chadsoft.co.uk/time-trials/players.html) and search for your player page, which will have your player ID.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else if (trackIx < 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*{track} could not be found.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var webClient = new WebClient();
                    TrackData foundTrack = trackList[trackIx];
                    List<TrackData> allTrackCategories = trackList.Where(x => x.SHA1 == foundTrack.SHA1).ToList();
                    GhostHolder ghostHolder = JsonConvert.DeserializeObject<GhostHolder>(await webClient.DownloadStringTaskAsync(playerList[playerIx].PlayerLink));
                    List<Ghost> applicableGhosts = ghostHolder.Ghosts.Where(x => x.TrackID == foundTrack.SHA1 && (vehicleRestriction == "" ? x.PersonalBest : true) && x.Is200cc == cc).ToList();
                    if (vehicleRestriction == "Kart")
                    {
                        applicableGhosts.RemoveAll(x => x.VehicleID > 17);
                        var orderedGhosts = applicableGhosts.OrderBy(x => ulong.Parse(x.FinishTime.Split(':')[0] + x.FinishTime.Split(':')[1].Split('.')[0] + x.FinishTime.Split(':')[1].Split('.')[1])).ToList();
                        foreach (var ghost in orderedGhosts)
                        {
                            HtmlDocument ghostPage = new HtmlDocument();
                            ghostPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL.Replace("json", "html")}"));
                            var strongs = ghostPage.DocumentNode.SelectNodes("//strong");
                            foreach (var strong in strongs.Where(x => x.InnerText.Contains(foundTrack.Name)))
                            {
                                if (!strong.InnerText.Contains("Normal") && !strong.InnerText.Contains("No-shortcut") && !strong.InnerText.Contains("Shortcut") && !strong.InnerText.Contains("Glitch"))
                                {
                                    ghost.CategoryName = "Normal";
                                }
                                else
                                {
                                    ghost.CategoryName = strong.InnerText.Split(' ')[strong.InnerText.Split(' ').Count() - 1];
                                }
                            }
                        }
                        applicableGhosts = new List<Ghost>();
                        foreach (var category in allTrackCategories)
                        {
                            var ix = orderedGhosts.FindIndex(x => x.CategoryName == category.CategoryName);
                            if (ix > -1)
                            {
                                applicableGhosts.Add(orderedGhosts[ix]);
                            }
                        }
                    }
                    else if (vehicleRestriction == "Bike")
                    {
                        applicableGhosts.RemoveAll(x => x.VehicleID < 18);
                        var orderedGhosts = applicableGhosts.OrderBy(x => ulong.Parse(x.FinishTime.Split(':')[0] + x.FinishTime.Split(':')[1].Split('.')[0] + x.FinishTime.Split(':')[1].Split('.')[1])).ToList();
                        foreach (var ghost in orderedGhosts)
                        {
                            HtmlDocument ghostPage = new HtmlDocument();
                            ghostPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL.Replace("json", "html")}"));
                            var strongs = ghostPage.DocumentNode.SelectNodes("//strong");
                            foreach (var strong in strongs.Where(x => x.InnerText.Contains(foundTrack.Name)))
                            {
                                if (!strong.InnerText.Contains("Normal") && !strong.InnerText.Contains("No-shortcut") && !strong.InnerText.Contains("Shortcut") && !strong.InnerText.Contains("Glitch"))
                                {
                                    ghost.CategoryName = "Normal";
                                }
                                else
                                {
                                    ghost.CategoryName = strong.InnerText.Split(' ')[strong.InnerText.Split(' ').Count() - 1];
                                }
                            }
                        }
                        applicableGhosts = new List<Ghost>();
                        foreach (var category in allTrackCategories)
                        {
                            var ix = orderedGhosts.FindIndex(x => x.CategoryName == category.CategoryName);
                            if (ix > -1)
                            {
                                applicableGhosts.Add(orderedGhosts[ix]);
                            }
                        }
                    }
                    if (applicableGhosts.Count == 0)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = $"*No record was found on {trackList[trackIx].Name}.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
                        allTrackCategories = allTrackCategories.OrderByDescending(x => x.CategoryName).ToList();
                        applicableGhosts = applicableGhosts.OrderByDescending(x => x.CategoryName).ToList();

                        foreach (var ghost in applicableGhosts)
                        {
                            HtmlDocument ghostPage = new HtmlDocument();
                            ghostPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL.Replace("json", "html")}"));
                            var strongs = ghostPage.DocumentNode.SelectNodes("//strong");
                            foreach (var strong in strongs.Where(x => x.InnerText.Contains(foundTrack.Name)))
                            {
                                if (!strong.InnerText.Contains("Normal") && !strong.InnerText.Contains("No-shortcut") && !strong.InnerText.Contains("Shortcut") && !strong.InnerText.Contains("Glitch"))
                                {
                                    ghost.CategoryName = "Normal";
                                }
                                else
                                {
                                    ghost.CategoryName = strong.InnerText.Split(' ')[strong.InnerText.Split(' ').Count() - 1];
                                }
                            }
                            ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL}"));

                            string controllerId = (ghost.ControllerID != 0 && ghost.ControllerID != 1 && ghost.ControllerID != 2 && ghost.ControllerID != 3) ? "???" : Util.Controllers[ghost.ControllerID];

                            var embed = new DiscordEmbedBuilder
                            {
                                Color = new DiscordColor("#FF0000"),
                                Title = $"__**{trackList[trackIx].Name} - {ghost.CategoryName} {(!cc ? "(150cc)" : "(200cc)")}{(vehicleRestriction == "" ? "" : $" [{vehicleRestriction}]")}:**__",
                                Description = $"<@{user.Id}>'s fastest time on {trackList[trackIx].Name}:\n\n" +
                                $"**Time:** {ghost.FinishTimeSimple}\n\n" +
                                $"**Splits:** {string.Join(" - ", ghost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                                $"**Combo:** {Util.Characters[ghost.DriverID]} on {Util.Vehicles[ghost.VehicleID]}\n\n" +
                                $"**Date Set:** {ghost.DateSet.Split('T')[0]}\n\n" +
                                $"**Controller:**\n{controllerId}\n\n" +
                                $"**Extra Details:**\n" +
                                $"*Exact Finish Time: {ghost.FinishTime}*\n\n" +
                                $"*Exact Splits: {string.Join(" - ", ghost.ExtraInfo.Splits.ToArray())}*",
                                Url = $"https://www.chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL.Substring(0, ghost.LinkContainer.Href.URL.Length - 4)}html",
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                                }
                            };

                            embeds.Add(embed);
                        }

                        var messageBuilder = new DiscordWebhookBuilder().AddEmbed(embeds[0]);

                        if (embeds.Count() > 1)
                        {
                            messageBuilder.AddComponents(Util.GeneratePageArrows());
                        }

                        var message = await ctx.EditResponseAsync(messageBuilder);

                        if (embeds.Count() > 1)
                        {
                            PendingPagesInteraction pending = new PendingPagesInteraction() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = embeds };

                            Util.PendingPageInteractions.Add(pending);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("top10", "Gets the top 10 of the track specified.")]
        public async Task GetTop10(InteractionContext ctx,
            [Option("track-name", "The track you want to display the leaderboard of.")] string track,
            [Choice("Kart", "Kart")]
            [Choice("Bike", "Bike")]
            [Option("vehicle-type", "Gets all ghosts based on a specific vehicle")] string vehicleRestriction = "",
            [Option("200cc", "The engine class of the personal best you want to find .")] bool cc = false)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);
                using var dbCtx = new MKBBContext();
                List<TrackData> trackList = dbCtx.Tracks.Where(x => x.Is200cc == cc).ToList();

                int trackIx = Util.ListNameCheck(trackList, track);
                if (trackIx < 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*{track} could not be found.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    var webClient = new WebClient();
                    TrackData foundTrack = trackList[trackIx];
                    List<TrackData> allTrackCategories = trackList.Where(x => x.SHA1 == foundTrack.SHA1).DistinctBy(x => x.CategoryName).ToList();
                    allTrackCategories = allTrackCategories.OrderByDescending(x => x.CategoryName.Length).ToList();

                    List<List<DiscordEmbedBuilder>> categories = new List<List<DiscordEmbedBuilder>>();

                    for (int i = 0; i < allTrackCategories.Count(); i++)
                    {
                        GhostHolder leaderboard = JsonConvert.DeserializeObject<GhostHolder>(await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{allTrackCategories[i].LeaderboardLink}?limit=45000"));

                        if (vehicleRestriction == "Kart")
                        {
                            leaderboard.Ghosts.RemoveAll(x => x.VehicleID > 17);
                        }
                        else if (vehicleRestriction == "Bike")
                        {
                            leaderboard.Ghosts.RemoveAll(x => x.VehicleID < 18);
                        }

                        leaderboard.Ghosts.RemoveAll(x => !x.PersonalBest);
                        try
                        {
                            leaderboard.Ghosts.RemoveRange(10, leaderboard.Ghosts.Count() - 10);
                        }
                        catch
                        {
                            Console.WriteLine("Already <=10.");
                        }

                        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
                        string leaderboardDisplay = "";

                        foreach (var ghost in leaderboard.Ghosts)
                        {
                            var ghostJson = await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL}");
                            ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(ghostJson);

                            leaderboardDisplay += $"**{leaderboard.Ghosts.FindIndex(x => x.LinkContainer.Href.URL == ghost.LinkContainer.Href.URL) + 1})** {ghost.ExtraInfo.MiiName} - {ghost.FinishTimeSimple}\n";
                        }

                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**{allTrackCategories[i].Name} - {allTrackCategories[i].CategoryName} {(!cc ? "(150cc)" : "(200cc)")}{(vehicleRestriction == "" ? "" : $"[ {vehicleRestriction}]")}:**__",
                            Description = leaderboardDisplay,
                            Url = $"https://www.chadsoft.co.uk/time-trials{allTrackCategories[i].LeaderboardLink.Substring(0, allTrackCategories[i].LeaderboardLink.Length - 4)}html",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        if (leaderboardDisplay != "")
                        {
                            embeds.Add(embed);
                        }

                        foreach (var top10Ghost in leaderboard.Ghosts)
                        {
                            var ghostJson = await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL}");
                            HtmlDocument ghostPage = new HtmlDocument();
                            ghostPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL.Replace("json", "html")}"));
                            var strongs = ghostPage.DocumentNode.SelectNodes("//strong");
                            foreach (var strong in strongs.Where(x => x.InnerText.Contains(foundTrack.Name)))
                            {
                                if (!strong.InnerText.Contains("Normal") && !strong.InnerText.Contains("No-shortcut") && !strong.InnerText.Contains("Shortcut") && !strong.InnerText.Contains("Glitch"))
                                {
                                    top10Ghost.CategoryName = "Normal";
                                }
                                else
                                {
                                    top10Ghost.CategoryName = strong.InnerText.Split(' ')[strong.InnerText.Split(' ').Count() - 1];
                                }
                            }
                            top10Ghost.Category = JsonConvert.DeserializeObject<Ghost>(ghostJson).Category;
                            top10Ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(ghostJson);

                            string controllerId = (top10Ghost.ControllerID != 0 && top10Ghost.ControllerID != 1 && top10Ghost.ControllerID != 2 && top10Ghost.ControllerID != 3) ? "???" : Util.Controllers[top10Ghost.ControllerID];

                            embed = new DiscordEmbedBuilder
                            {
                                Color = new DiscordColor("#FF0000"),
                                Title = $"__**{leaderboard.Ghosts.FindIndex(x => x.LinkContainer.Href.URL == top10Ghost.LinkContainer.Href.URL) + 1}) {trackList[trackIx].Name} {(top10Ghost.CategoryName != null ? $"- {top10Ghost.CategoryName}" : "")} {(!cc ? "(150cc)" : "(200cc)")}:**__",
                                Description = $"{top10Ghost.ExtraInfo.MiiName}'s fastest time on {trackList[trackIx].Name}:\n\n" +
                                $"**Time:** {top10Ghost.FinishTimeSimple}\n\n" +
                                $"**Splits:** {string.Join(" - ", top10Ghost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                                $"**Combo:** {Util.Characters[top10Ghost.DriverID]} on {Util.Vehicles[top10Ghost.VehicleID]}\n\n" +
                                $"**Date Set:** {top10Ghost.DateSet.Split('T')[0]}\n\n" +
                                $"**Controller:**\n{controllerId}\n\n" +
                                $"**Extra Details:**\n" +
                                $"*Exact Finish Time: {top10Ghost.FinishTime}*\n\n" +
                                $"*Exact Splits: {string.Join(" - ", top10Ghost.ExtraInfo.Splits.ToArray())}*",
                                Url = $"https://www.chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL.Substring(0, top10Ghost.LinkContainer.Href.URL.Length - 4)}html",
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                                }
                            };
                            embeds.Add(embed);
                        }

                        categories.Add(embeds);
                    }

                    var messageBuilder = new DiscordWebhookBuilder().AddEmbed(categories[0][0]);

                    if (categories.Count() > 1)
                    {
                        messageBuilder.AddComponents(Util.GenerateCategorySelectMenu(allTrackCategories, 0));
                    }
                    if (categories[0].Count() > 1)
                    {
                        messageBuilder.AddComponents(Util.GeneratePageArrows());
                    }

                    var message = await ctx.EditResponseAsync(messageBuilder);

                    if (categories[0].Count() > 1)
                    {
                        PendingPagesInteraction pending = new PendingPagesInteraction() { CurrentPage = 0, CurrentCategory = 0, MessageId = message.Id, Context = ctx, Categories = categories, Pages = categories[0], CategoryNames = allTrackCategories };

                        Util.PendingPageInteractions.Add(pending);
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        //top10 alias
        [SlashCommand("bkt", "Gets the top 10 of the track specified (alias of top10).")]
        public async Task GetTop10Alias(InteractionContext ctx,
            [Option("track-name", "The track you want to display the leaderboard of.")] string track,
            [Choice("Kart", "Kart")]
            [Choice("Bike", "Bike")]
            [Option("vehicle-type", "Gets all ghosts based on a specific vehicle")] string vehicleRestriction = "",
            [Option("200cc", "The engine class of the personal best you want to find .")] bool cc = false)
        {
            await GetTop10(ctx, track, vehicleRestriction, cc);
        }

        [SlashCommand("servertop10", "Gets the top 10 of the track specified for all players in the server.")]
        public async Task GetServerTop10(InteractionContext ctx,
            [Option("track-name", "The track you want to display the leaderboard of.")] string track,
            [Choice("Kart", "Kart")]
            [Choice("Bike", "Bike")]
            [Option("vehicle-type", "Gets all ghosts based on a specific vehicle")] string vehicleRestriction = "",
            [Option("200cc", "The engine class of the personal best you want to find .")] bool cc = false)
        {

            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);
                using var dbCtx = new MKBBContext();
                List<PlayerData> players = dbCtx.Players.ToList();
                var allMembers = ctx.Guild.GetAllMembersAsync().Result;
                List<ulong> allMemberIds = new List<ulong>();
                foreach (var member in allMembers)
                {
                    allMemberIds.Add(member.Id);
                }
                players.RemoveAll(x => !allMemberIds.Contains(x.DiscordID));

                if (players.Count == 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = "*No members in this guild are registered.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    List<TrackData> trackList = dbCtx.Tracks.Where(x => x.Is200cc == cc).ToList();
                    int trackIx = Util.ListNameCheck(trackList, track);
                    if (trackIx < 0)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = $"*{track} could not be found.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        var webClient = new WebClient();
                        TrackData foundTrack = trackList[trackIx];
                        List<TrackData> allTrackCategories = trackList.Where(x => x.SHA1 == foundTrack.SHA1).ToList();
                        allTrackCategories = allTrackCategories.OrderByDescending(x => x.CategoryName.Length).ToList();

                        List<List<DiscordEmbedBuilder>> categories = new List<List<DiscordEmbedBuilder>>();

                        for (int i = 0; i < allTrackCategories.Count(); i++)
                        {
                            GhostHolder leaderboard = JsonConvert.DeserializeObject<GhostHolder>(await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{allTrackCategories[i].LeaderboardLink}?limit=45000"));
                            List<string> playerIds = new List<string>();
                            foreach (var player in players)
                            {
                                playerIds.Add(player.PlayerID);
                            }
                            if (vehicleRestriction == "Kart")
                            {
                                leaderboard.Ghosts.RemoveAll(x => x.VehicleID > 17);
                            }
                            else if (vehicleRestriction == "Bike")
                            {
                                leaderboard.Ghosts.RemoveAll(x => x.VehicleID < 18);
                            }
                            leaderboard.Ghosts.RemoveAll(x => !x.PersonalBest || !playerIds.Contains(x.PlayerId));
                            try
                            {
                                leaderboard.Ghosts.RemoveRange(10, leaderboard.Ghosts.Count() - 10);
                            }
                            catch
                            {
                                Console.WriteLine("Already <=10.");
                            }

                            List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
                            string leaderboardDisplay = "";

                            foreach (var ghost in leaderboard.Ghosts)
                            {
                                var ghostJson = await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{ghost.LinkContainer.Href.URL}");
                                ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(ghostJson);
                                ulong playerId = players.Find(x => x.PlayerID == ghost.PlayerId).DiscordID;
                                leaderboardDisplay += $"**{leaderboard.Ghosts.FindIndex(x => x.LinkContainer.Href.URL == ghost.LinkContainer.Href.URL) + 1})** <@{playerId}> - {ghost.FinishTimeSimple}\n";
                            }

                            var embed = new DiscordEmbedBuilder
                            {
                                Color = new DiscordColor("#FF0000"),
                                Title = $"__**{allTrackCategories[i].Name} - {allTrackCategories[i].CategoryName} {(!cc ? "(150cc)" : "(200cc)")}{(vehicleRestriction == "" ? "" : $" [{vehicleRestriction}]")}:**__",
                                Description = leaderboardDisplay,
                                Url = $"https://www.chadsoft.co.uk/time-trials{allTrackCategories[i].LeaderboardLink.Substring(0, allTrackCategories[i].LeaderboardLink.Length - 4)}html",
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                                }
                            };
                            if (leaderboardDisplay != "")
                            {
                                embeds.Add(embed);
                            }

                            foreach (var top10Ghost in leaderboard.Ghosts)
                            {
                                var ghostJson = await webClient.DownloadStringTaskAsync($"https://www.chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL}");
                                HtmlDocument ghostPage = new HtmlDocument();
                                ghostPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL.Replace("json", "html")}"));
                                var strongs = ghostPage.DocumentNode.SelectNodes("//strong");
                                foreach (var strong in strongs.Where(x => x.InnerText.Contains(foundTrack.Name)))
                                {
                                    if (!strong.InnerText.Contains("Normal") && !strong.InnerText.Contains("No-shortcut") && !strong.InnerText.Contains("Shortcut") && !strong.InnerText.Contains("Glitch"))
                                    {
                                        top10Ghost.CategoryName = "Normal";
                                    }
                                    else
                                    {
                                        top10Ghost.CategoryName = strong.InnerText.Split(' ')[strong.InnerText.Split(' ').Count() - 1];
                                    }
                                }
                                top10Ghost.Category = JsonConvert.DeserializeObject<Ghost>(ghostJson).Category;
                                top10Ghost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(ghostJson);

                                string controllerId = (top10Ghost.ControllerID != 0 && top10Ghost.ControllerID != 1 && top10Ghost.ControllerID != 2 && top10Ghost.ControllerID != 3) ? "???" : Util.Controllers[top10Ghost.ControllerID];

                                embed = new DiscordEmbedBuilder
                                {
                                    Color = new DiscordColor("#FF0000"),
                                    Title = $"__**{leaderboard.Ghosts.FindIndex(x => x.LinkContainer.Href.URL == top10Ghost.LinkContainer.Href.URL) + 1}) {trackList[trackIx].Name} {(top10Ghost.CategoryName != null ? $"- {top10Ghost.CategoryName}" : "")} {(!cc ? "(150cc)" : "(200cc)")}:**__",
                                    Description = $"{top10Ghost.ExtraInfo.MiiName}'s fastest time on {trackList[trackIx].Name}:\n\n" +
                                    $"**Time:** {top10Ghost.FinishTimeSimple}\n\n" +
                                    $"**Splits:** {string.Join(" - ", top10Ghost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                                    $"**Combo:** {Util.Characters[top10Ghost.DriverID]} on {Util.Vehicles[top10Ghost.VehicleID]}\n\n" +
                                    $"**Date Set:** {top10Ghost.DateSet.Split('T')[0]}\n\n" +
                                    $"**Controller:**\n{controllerId}\n\n" +
                                    $"**Extra Details:**\n" +
                                    $"*Exact Finish Time: {top10Ghost.FinishTime}*\n\n" +
                                    $"*Exact Splits: {string.Join(" - ", top10Ghost.ExtraInfo.Splits.ToArray())}*",
                                    Url = $"https://www.chadsoft.co.uk/time-trials{top10Ghost.LinkContainer.Href.URL.Substring(0, top10Ghost.LinkContainer.Href.URL.Length - 4)}html",
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                                    }
                                };

                                embeds.Add(embed);
                            }
                            if (embeds.Count > 0)
                            {
                                categories.Add(embeds);
                            }
                        }

                        var messageBuilder = new DiscordWebhookBuilder().AddEmbed(categories[0][0]);

                        if (categories.Count() > 1)
                        {
                            messageBuilder.AddComponents(Util.GenerateCategorySelectMenu(allTrackCategories, 0));
                        }
                        if (categories[0].Count() > 1)
                        {
                            messageBuilder.AddComponents(Util.GeneratePageArrows());
                        }

                        var message = await ctx.EditResponseAsync(messageBuilder);

                        if (categories[0].Count() > 1)
                        {
                            PendingPagesInteraction pending = new PendingPagesInteraction() { CurrentPage = 0, CurrentCategory = 0, MessageId = message.Id, Context = ctx, Categories = categories, Pages = categories[0], CategoryNames = allTrackCategories };

                            Util.PendingPageInteractions.Add(pending);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("staff", "Gets the staff ghosts for the track specified.")]
        public async Task GetStaffGhosts(InteractionContext ctx,
            [Option("track-name", "The track that the staff ghosts are set on.")] string track)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.ParentId == 755509221394743467 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);
                using var dbCtx = new MKBBContext();
                List<TrackData> trackList = dbCtx.Tracks.Where(x => x.EasyStaffSHA1 != null && x.ExpertStaffSHA1 != null).ToList();

                int ix = Util.ListNameCheck(trackList, track);

                if (ix < 0)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = $"*{track} could not be found.*",
                        Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1188255728",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }

                else
                {
                    WebClient webClient = new WebClient();
                    var easyGhostJson = await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials/rkgd/{trackList[ix].EasyStaffSHA1.Substring(0, 2)}/{trackList[ix].EasyStaffSHA1.Substring(2, 2)}/{trackList[ix].EasyStaffSHA1.Substring(4)}.json");
                    var expertGhostJson = await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials/rkgd/{trackList[ix].ExpertStaffSHA1.Substring(0, 2)}/{trackList[ix].ExpertStaffSHA1.Substring(2, 2)}/{trackList[ix].ExpertStaffSHA1.Substring(4)}.json");
                    Ghost easyGhost = JsonConvert.DeserializeObject<Ghost>(easyGhostJson);
                    easyGhost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(easyGhostJson);
                    Ghost expertGhost = JsonConvert.DeserializeObject<Ghost>(expertGhostJson);
                    expertGhost.ExtraInfo = JsonConvert.DeserializeObject<ExtraInfo>(expertGhostJson);

                    List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>()
                    {
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Staff ghosts for {trackList[ix].Name} *(First result)*:**__",
                            Description = $"Easy Staff Ghost by {easyGhost.ExtraInfo.MiiName}:\n\n" +
                                    $"**Time:** {easyGhost.FinishTimeSimple}\n\n" +
                                    $"**Splits:** {string.Join(" - ", easyGhost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                                    $"**Combo:** {Util.Characters[easyGhost.DriverID]} on {Util.Vehicles[easyGhost.VehicleID]}\n\n" +
                                    $"**Date Set:** {easyGhost.DateSet.Split('T')[0]}\n\n" +
                                    $"**Controller:**\n{Util.Controllers[easyGhost.ControllerID]}\n\n" +
                                    $"**Extra Details:**\n" +
                                    $"*Exact Finish Time: {easyGhost.FinishTime}*\n\n" +
                                    $"*Exact Splits: {string.Join(" - ", easyGhost.ExtraInfo.Splits.ToArray())}*",
                            Url = $"https://chadsoft.co.uk/time-trials/rkgd/{trackList[ix].EasyStaffSHA1.Substring(0, 2)}/{trackList[ix].EasyStaffSHA1.Substring(2, 2)}/{trackList[ix].EasyStaffSHA1.Substring(4)}.html",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Staff ghosts for {trackList[ix].Name} *(First result)*:**__",
                            Description = $"Expert Staff Ghost by {expertGhost.ExtraInfo.MiiName}:\n\n" +
                                    $"**Time:** {expertGhost.FinishTimeSimple}\n\n" +
                                    $"**Splits:** {string.Join(" - ", expertGhost.ExtraInfo.SplitsSimple.ToArray())}\n\n" +
                                    $"**Combo:** {Util.Characters[expertGhost.DriverID]} on {Util.Vehicles[expertGhost.VehicleID]}\n\n" +
                                    $"**Date Set:** {expertGhost.DateSet.Split('T')[0]}\n\n" +
                                    $"**Controller:**\n{Util.Controllers[expertGhost.ControllerID]}\n\n" +
                                    $"**Extra Details:**\n" +
                                    $"*Exact Finish Time: {expertGhost.FinishTime}*\n\n" +
                                    $"*Exact Splits: {string.Join(" - ", expertGhost.ExtraInfo.Splits.ToArray())}*",
                            Url = $"https://chadsoft.co.uk/time-trials/rkgd/{trackList[ix].ExpertStaffSHA1.Substring(0, 2)}/{trackList[ix].ExpertStaffSHA1.Substring(2, 2)}/{trackList[ix].ExpertStaffSHA1.Substring(4)}.html",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }
                    };

                    var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(embeds[0])
                        .AddComponents(Util.GeneratePageArrows()));

                    Util.PendingPageInteractions.Add(new PendingPagesInteraction() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = embeds });
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }
    }
}
