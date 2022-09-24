﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using System.Globalization;
using System.Linq;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using DSharpPlus.SlashCommands.Attributes;

namespace MKBB.Commands
{
    public class Update : ApplicationCommandModule
    {
        [SlashCommand("starttimer", "Starts the timer for updating data for the bot.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task StartTimers(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });

            JobManager.Stop();
            JobManager.RemoveAllJobs();

            Util.ScheduleRegister = new Registry();
            Util.ScheduleRegister.Schedule(async () => await ExecuteTimer(ctx)).ToRunEvery(1).Days().At(12, 0);

            JobManager.Initialize(Util.ScheduleRegister);

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#FF0000"),
                Title = $"__**Notice:**__",
                Description = "Timer has been started.",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Server Time: {DateTime.Now}"
                }
            };
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        public async Task ExecuteTimer(InteractionContext ctx)
        {
            Util.PendingInteractions = new List<PendingPaginator>();
            try
            {
                await CheckStrikes(ctx);
                await UpdateInit(ctx, "all");
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ctx, ex);
            }
        }

        [SlashCommand("update", "Updates the database with information from Chadsoft and Wiimmfi.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task UpdateInit(InteractionContext ctx,
            [Choice("All", "all")]
            [Choice("Wiimmfi", "wiimmfi")]
            [Choice("BKTs", "bkts")]
            [Option("Quantity", "Determines how much data to retrieve.")] string arg = "")
        {
            if (ctx.CommandName == "update")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }

            try
            {
                await UpdateJsons(ctx, arg);
                if (ctx.CommandName == "update")
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Notice:**__",
                        Description = "*Database has been updated.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Server Time: {DateTime.Now}"
                        }
                    };

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            catch (Exception ex)
            {
                if (ctx.CommandName == "update")
                {
                    await Util.ThrowError(ctx, ex);
                }
                else
                {
                    await Util.ThrowInteractionlessError(ctx, ex);
                }
            }
        }

        public async Task UpdateJsons(InteractionContext ctx, string arg)
        {
            string rtttUrl = "http://tt.chadsoft.co.uk/original-track-leaderboards.json";
            string ctttUrl = "http://tt.chadsoft.co.uk/ctgp-leaderboards.json";
            string rttt200Url = "http://tt.chadsoft.co.uk/original-track-leaderboards-200cc.json";
            string cttt200Url = "http://tt.chadsoft.co.uk/ctgp-leaderboards-200cc.json";
            string ctwwUrl1 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0";
            string ctwwUrl2 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0,100";
            string ctwwUrl3 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0,200";
            string wwUrl = "https://wiimmfi.de/stats/track/mv/ww?m=json&p=std,c2,0";

            // Leaderboards

            var webClient = new WebClient();

            var rtRawJson = JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(rtttUrl));
            var ctRawJson = JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(ctttUrl));
            var rtRaw200Json = JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(rttt200Url));
            var ctRaw200Json = JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(cttt200Url));

            foreach (var track in rtRawJson.Leaderboard)
            {
                track.LeaderboardLink = track.LinkContainer.Href.URL;
                track.LinkContainer = null;
            }
            foreach (var track in ctRawJson.Leaderboard)
            {
                track.LeaderboardLink = track.LinkContainer.Href.URL;
                track.LinkContainer = null;
            }
            foreach (var track in rtRaw200Json.Leaderboard)
            {
                track.LeaderboardLink = track.LinkContainer.Href.URL;
                track.LinkContainer = null;
            }
            foreach (var track in ctRaw200Json.Leaderboard)
            {
                track.LeaderboardLink = track.LinkContainer.Href.URL;
                track.LinkContainer = null;
            }

            var rtJson = JsonConvert.SerializeObject(rtRawJson.Leaderboard);
            var ctJson = JsonConvert.SerializeObject(ctRawJson.Leaderboard);
            var rt200Json = JsonConvert.SerializeObject(rtRaw200Json.Leaderboard);
            var ct200Json = JsonConvert.SerializeObject(ctRaw200Json.Leaderboard);

            string ctwwDl1 = await webClient.DownloadStringTaskAsync(ctwwUrl1);
            string ctwwDl2 = await webClient.DownloadStringTaskAsync(ctwwUrl2);
            string ctwwDl3 = await webClient.DownloadStringTaskAsync(ctwwUrl3);
            string wwDl = await webClient.DownloadStringTaskAsync(wwUrl);

            List<Track> trackListRt = JsonConvert.DeserializeObject<List<Track>>(rtJson);
            List<Track> trackListRt200 = JsonConvert.DeserializeObject<List<Track>>(rt200Json);
            List<Track> trackList = JsonConvert.DeserializeObject<List<Track>>(ctJson);
            List<Track> trackList200 = JsonConvert.DeserializeObject<List<Track>>(ct200Json);
            List<Track> trackListNc = JsonConvert.DeserializeObject<List<Track>>(ctJson);
            List<Track> trackList200Nc = JsonConvert.DeserializeObject<List<Track>>(ct200Json);
            for (int i = 0; i < trackListNc.Count; i++)
            {
                if (trackListNc[i].Category % 16 != 0)
                {
                    trackListNc.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < trackList200Nc.Count; i++)
            {
                if (trackList200Nc[i].Category % 16 != 0 || trackList200Nc[i].Category != 4)
                {
                    trackList200Nc.RemoveAt(i);
                    i--;
                }
            }

            string oldRtJson = File.ReadAllText("rts.json");
            List<Track> oldRtTrackList = JsonConvert.DeserializeObject<List<Track>>(oldRtJson);

            for (int i = 0; i < trackListRt.Count; i++)
            {
                int ix = oldRtTrackList.FindIndex(t => t.Name == trackListRt[i].Name && t.Category == trackListRt[i].Category);
                if (ix != -1)
                {
                    trackListRt[i].WiimmfiName = oldRtTrackList[ix].WiimmfiName;
                    trackListRt[i].WiimmfiScore = oldRtTrackList[ix].WiimmfiScore;
                    trackListRt[i].BKTLink = oldRtTrackList[ix].BKTLink;
                    trackListRt[i].BKTHolder = oldRtTrackList[ix].BKTHolder;
                    trackListRt[i].CategoryName = oldRtTrackList[ix].CategoryName;
                }
            }

            oldRtJson = File.ReadAllText("rts200.json");
            oldRtTrackList = JsonConvert.DeserializeObject<List<Track>>(oldRtJson);

            for (int i = 0; i < trackListRt200.Count; i++)
            {
                int ix = oldRtTrackList.FindIndex(t => t.Name == trackListRt200[i].Name && t.Category == trackListRt200[i].Category);
                if (ix != -1)
                {
                    trackListRt200[i].WiimmfiName = oldRtTrackList[ix].WiimmfiName;
                    trackListRt200[i].WiimmfiScore = oldRtTrackList[ix].WiimmfiScore;
                    trackListRt200[i].BKTLink = oldRtTrackList[ix].BKTLink;
                    trackListRt200[i].BKTHolder = oldRtTrackList[ix].BKTHolder;
                    trackListRt200[i].CategoryName = oldRtTrackList[ix].CategoryName;
                }
            }

            string oldJson = File.ReadAllText("cts.json");
            List<Track> oldTrackList = JsonConvert.DeserializeObject<List<Track>>(oldJson);

            for (int i = 0; i < trackList.Count; i++)
            {
                int ix = oldTrackList.FindIndex(t => t.Name == trackList[i].Name && t.Category == trackList[i].Category);
                if (ix != -1)
                {
                    trackList[i].WiimmfiName = oldTrackList[ix].WiimmfiName;
                    trackList[i].WiimmfiScore = oldTrackList[ix].WiimmfiScore;
                    trackList[i].BKTLink = oldTrackList[ix].BKTLink;
                    trackList[i].BKTHolder = oldTrackList[ix].BKTHolder;
                    trackList[i].CategoryName = oldTrackList[ix].CategoryName;
                }
            }

            oldJson = File.ReadAllText("cts200.json");
            oldTrackList = JsonConvert.DeserializeObject<List<Track>>(oldJson);

            for (int i = 0; i < trackList200.Count; i++)
            {
                int ix = oldTrackList.FindIndex(t => t.Name == trackList200[i].Name && t.Category == trackList200[i].Category);
                if (ix != -1)
                {
                    trackList200[i].WiimmfiName = oldTrackList[ix].WiimmfiName;
                    trackList200[i].WiimmfiScore = oldTrackList[ix].WiimmfiScore;
                    trackList200[i].BKTLink = oldTrackList[ix].BKTLink;
                    trackList200[i].BKTHolder = oldTrackList[ix].BKTHolder;
                    trackList200[i].CategoryName = oldTrackList[ix].CategoryName;
                }
            }

            try
            {
                if (Util.CompareIncompleteStrings(arg, "wiimmfi") || Util.CompareIncompleteStrings(arg, "all"))
                {
                    await Util.Scraper.WiimmfiScrape(rtJson,
                        rt200Json,
                        ctwwDl1,
                        ctwwDl2,
                        ctwwDl3,
                        wwDl,
                        trackListRt,
                        trackListRt200,
                        trackList,
                        trackList200,
                        trackListNc,
                        trackList200Nc);
                }
            }
            catch
            {
                Thread.Sleep(300000);
                await Util.Scraper.WiimmfiScrape(rtJson,
                         rt200Json,
                         ctwwDl1,
                         ctwwDl2,
                         ctwwDl3,
                         wwDl,
                         trackListRt,
                         trackListRt200,
                         trackList,
                         trackList200,
                         trackListNc,
                         trackList200Nc);
            }

            try
            {
                if (Util.CompareIncompleteStrings(arg, "bkts") || Util.CompareIncompleteStrings(arg, "all"))
                {
                    await Util.Scraper.GetBKTLeaderboards(trackListRt, trackListRt200, trackList, trackList200);
                }
            }
            catch
            {
                Thread.Sleep(300000);
                await Util.Scraper.GetBKTLeaderboards(trackListRt, trackListRt200, trackList, trackList200);
            }

            try
            {
                await Util.Scraper.GetSlotIds(trackListRt, trackListRt200, trackList, trackList200);
            }
            catch
            {
                Thread.Sleep(300000);
                await Util.Scraper.GetSlotIds(trackListRt, trackListRt200, trackList, trackList200);
            }

            string playerListJson = File.ReadAllText("players.json");
            List<Player> playerList = JsonConvert.DeserializeObject<List<Player>>(playerListJson);

            foreach (var player in playerList)
            {
                try
                {
                    var playerJson = JsonConvert.DeserializeObject<Player>(await webClient.DownloadStringTaskAsync(player.PlayerLink));
                    player.MiiName = playerJson.MiiName;
                    Console.WriteLine($"Updating data for {player.MiiName}.");
                }
                catch
                {
                    Console.WriteLine($"Download failed for {player.MiiName}.");
                }
            }

            playerListJson = JsonConvert.SerializeObject(playerList);
            File.WriteAllText("players.json", playerListJson);

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            ctJson = JsonConvert.SerializeObject(trackList, settings);
            ct200Json = JsonConvert.SerializeObject(trackList200, settings);

            rtJson = JsonConvert.SerializeObject(trackListRt, settings);
            rt200Json = JsonConvert.SerializeObject(trackListRt200, settings);

            File.WriteAllText("rts.json", rtJson);
            File.WriteAllText("cts.json", ctJson);
            File.WriteAllText("rts200.json", rt200Json);
            File.WriteAllText("cts200.json", ct200Json);

            var processInfo = new ProcessStartInfo();
            processInfo.FileName = @"sudo";
            processInfo.Arguments = $"cp rts.json /var/www/brawlbox/MKBB/";
            processInfo.CreateNoWindow = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            var process = new Process();
            process.StartInfo = processInfo;
            process.Start();
            process.WaitForExit();

            processInfo = new ProcessStartInfo();
            processInfo.FileName = @"sudo";
            processInfo.Arguments = $"cp rts200.json /var/www/brawlbox/MKBB/";
            processInfo.CreateNoWindow = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            process = new Process();
            process.StartInfo = processInfo;
            process.Start();
            process.WaitForExit();

            processInfo = new ProcessStartInfo();
            processInfo.FileName = @"sudo";
            processInfo.Arguments = $"cp cts.json /var/www/brawlbox/MKBB/";
            processInfo.CreateNoWindow = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            process = new Process();
            process.StartInfo = processInfo;
            process.Start();
            process.WaitForExit();

            processInfo = new ProcessStartInfo();
            processInfo.FileName = @"sudo";
            processInfo.Arguments = $"cp cts200.json /var/www/brawlbox/MKBB/";
            processInfo.CreateNoWindow = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            process = new Process();
            process.StartInfo = processInfo;
            process.Start();
            process.WaitForExit();

            var today = DateTime.Now;
            File.WriteAllText("lastUpdated.txt", today.ToString());

            DiscordActivity activity = new DiscordActivity();
            activity.Name = $"Last Updated: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}" +
                $" | /help";
            await ctx.Client.UpdateStatusAsync(activity);

        }

        [SlashCommand("checkstrikes", "Checks council spreadsheet for strikes, and also notifies if there is homework due.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task CheckStrikesInit(InteractionContext ctx)
        {
            if (ctx.CommandName == "checkstrikes")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }
            try
            {
                await CheckStrikes(ctx);

                if (ctx.CommandName == "checkstrikes")
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Notice:**__",
                        Description = $"*Any strikes have been recorded.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Server Time: {DateTime.Now}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            catch (Exception ex)
            {
                if (ctx.CommandName == "checkstrikes")
                {
                    await Util.ThrowError(ctx, ex);
                }
                else
                {
                    await Util.ThrowInteractionlessError(ctx, ex);
                }
            }
        }

        public async Task CheckStrikes(InteractionContext ctx)
        {
            List<string> trackDisplay = new List<string>();
            string json;

            string serviceAccountEmail = "brawlbox@custom-track-testing-bot.iam.gserviceaccount.com";
            var certificate = new X509Certificate2(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mario Kart Brawlbot",
            });

            var temp = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Track Evaluation Log'");
            var tempResponse = await temp.ExecuteAsync();
            var today = int.Parse(tempResponse.Values[tempResponse.Values.Count - 1][tempResponse.Values[tempResponse.Values.Count - 1].Count - 1].ToString());

            var request = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Track Evaluating'");
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
            var response = await request.ExecuteAsync();
            foreach (var t in response.Values)
            {
                while (t.Count < response.Values[0].Count)
                {
                    t.Add("");
                }
            }

            using (var fs = File.OpenRead("council.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            List<CouncilMember> councilJson = JsonConvert.DeserializeObject<List<CouncilMember>>(json);
            var hwCompleted = new bool?[councilJson.Count];

            for (int i = 1; i < response.Values.Count; i++)
            {
                if (today == int.Parse(response.Values[i][1].ToString()))
                {
                    trackDisplay.Add(response.Values[i][0].ToString());
                }
                int lastChecked = Convert.ToInt32(DateTime.Parse(File.ReadAllText("lastUpdated.txt")).Subtract(DateTime.ParseExact("31/12/1899", "dd/MM/yyyy", CultureInfo.InvariantCulture)).TotalDays);
                if (lastChecked != int.Parse(response.Values[i][1].ToString()) && today == int.Parse(response.Values[i][1].ToString()) + 1)
                {
                    for (int h = 0; h < hwCompleted.Length; h++)
                    {
                        hwCompleted[h] = true;
                    }
                    for (int j = 12; j < response.Values[0].Count; j++)
                    {
                        int ix = councilJson.FindIndex(x => x.SheetName == response.Values[0][j].ToString());
                        if ((response.Values[i][j].ToString() == "" ||
                                response.Values[i][j].ToString().ToLowerInvariant() == "yes" ||
                                response.Values[i][j].ToString().ToLowerInvariant() == "no" ||
                                response.Values[i][j].ToString().ToLowerInvariant() == "neutral" ||
                                response.Values[i][j].ToString().ToLowerInvariant() == "fixes" ||
                                !response.Values[i][j].ToString().ToLowerInvariant().Contains("yes") &&
                                !response.Values[i][j].ToString().ToLowerInvariant().Contains("no") &&
                                !response.Values[i][j].ToString().ToLowerInvariant().Contains("neutral") &&
                                !response.Values[i][j].ToString().ToLowerInvariant().Contains("fixes")) &&
                                response.Values[i][j].ToString() != "This member is the author thus cannot vote")
                        {
                            hwCompleted[ix] = false;
                        }
                    }
                }
            }
            List<string> inconsistentMembers = new List<string>();
            for (int i = 0; i < hwCompleted.Length; i++)
            {
                if (hwCompleted[i] == true)
                {
                    councilJson[i].HwInARow++;
                    if (councilJson[i].HwInARow > 4)
                    {
                        councilJson[i].TimesMissedHw = 0;
                    }
                }
                else if (hwCompleted[i] == false)
                {
                    councilJson[i].TimesMissedHw++;
                    councilJson[i].HwInARow = 0;
                    inconsistentMembers.Add(councilJson[i].SheetName);
                    if (councilJson[i].TimesMissedHw > 0 && councilJson[i].TimesMissedHw % 3 == 0)
                    {
                        string message = $"Hello {councilJson[i].SheetName}. Just to let you know, you appear to have not completed council homework in a while, have been inconsistent with your homework, or are not completing it sufficiently enough. Just to remind you, if you miss homework too many times, admins might have to remove you from council. If you have an issue which stops you from doing homework, please let an admin know.";

                        var members = ctx.Guild.GetAllMembersAsync();
                        foreach (var member in members.Result)
                        {
                            if (member.Id == councilJson[i].DiscordId)
                            {
                                try
                                {
                                    Console.WriteLine($"DM'd Member: {councilJson[i].SheetName}");
                                    await member.SendMessageAsync(message).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine("DMs are likely closed.");
                                }
                            }
                        }
                    }
                }
            }

            string council = JsonConvert.SerializeObject(councilJson);
            File.WriteAllText("council.json", council);

            DiscordChannel channel = ctx.Channel;

            foreach (var c in ctx.Guild.Channels)
            {
                if (c.Value.Id == 635313521487511554)
                {
                    channel = c.Value;
                }
            }

            string listOfTracks = "";
            if (trackDisplay.Count > 0)
            {
                listOfTracks = trackDisplay[0];
                for (int i = 1; i < trackDisplay.Count; i++)
                {
                    listOfTracks += $", {trackDisplay[i]}";
                }
                if (trackDisplay.Count == 1)
                {
                    listOfTracks += " is";
                }
                else
                {
                    listOfTracks += " are";
                }

                await channel.SendMessageAsync($"<@&608386209655554058> {listOfTracks} due for today.");
            }

            foreach (var c in ctx.Guild.Channels)
            {
                if (c.Value.Id == 935200150710808626)
                {
                    channel = c.Value;
                }
            }

            string description = string.Empty;
            foreach (var member in inconsistentMembers)
            {
                description += $"{member}\n";
            }

            if (inconsistentMembers.Count > 0)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Members who missed homework:**__",
                    Description = description,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                foreach (var c in ctx.Guild.Channels)
                {
                    if (c.Value.Id == 1019149329556062278)
                    {
                        channel = c.Value;
                    }
                }
                await channel.SendMessageAsync(embed);
            }


            var now = DateTime.Now;
            File.WriteAllText("lastUpdated.txt", now.ToString());

            DiscordActivity activity = new DiscordActivity();
            activity.Name = $"Last Updated: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}" +
                $" | /help";
            await ctx.Client.UpdateStatusAsync(activity);
        }
    }
}