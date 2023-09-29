using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using IronPython.Runtime.Operations;
using Microsoft.Identity.Client;
using MKBB.Class;
using MKBB.Data;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.Scripting.PerfTrack;

namespace MKBB.Commands
{
    public class Info : ApplicationCommandModule
    {
        [SlashCommand("tools", "Gives a list of useful tools or the ability to search for one.")]
        public static async Task ListTools(InteractionContext ctx,
            [Option("tool-name", "The name of the tool you wish to search for.")] string toolName = "")
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });

                using MKBBContext dbCtx = new();
                List<ToolData> toolList = dbCtx.Tools.ToList();

                if (toolName == "")
                {
                    List<DiscordEmbed> embeds = new();
                    foreach (var tool in toolList)
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Useful Tools:**__",
                            Description = $"**Name:**\n" +
                            $"{tool.Name}\n" +
                            $"**Creators:**\n" +
                            $"{tool.Creators}\n" +
                            $"**Description:**\n" +
                            $"{tool.Description}\n" +
                            $"**Download:**\n" +
                            $"{tool.Download}",
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
                else
                {
                    int index = Util.ListNameCheck(toolList, toolName);

                    DiscordEmbedBuilder embed = new();
                    if (index > -1)
                    {
                        embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Tool Search Result for {toolName}:**__",
                            Description = $"**Name:**\n" +
                            $"{toolList[index].Name}\n" +
                            $"**Creators:**\n" +
                            $"{toolList[index].Creators}\n" +
                            $"**Description:**\n" +
                            $"{toolList[index].Description}\n" +
                            $"**Download:**\n" +
                            $"{toolList[index].Download}",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                    }
                    else
                    {
                        embed = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Error:**__",
                            Description = $"*{toolName} could not be found. If you think a tool is missing, contact <@105742694730457088>.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                    }

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("nextupdate", "Displays the next update(s) coming to CTGP.")]
        public static async Task GetNextUpdate(InteractionContext ctx)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
                string serviceAccountEmail = "sheetbox@sonic-fiber-399810.iam.gserviceaccount.com";

                X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));

                SheetsService service = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Mario Kart Brawlbot",
                });

                SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'Update Queue'");
                request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
                ValueRange response = await request.ExecuteAsync();
                response.Values.Add(new List<object>());
                foreach (var t in response.Values)
                {
                    while (t.Count < 11)
                    {
                        t.Add("");
                    }
                }

                int k = 1;

                string title = $"__**{response.Values[k][1]}:**__";
                k += 2;

                List<DiscordEmbed> embeds = new();

            NewEmbed:
                string description = $"**{response.Values[k][1]}:**";
            NewSection:
                string header = response.Values[k][1].ToString();

                while (response.Values[k][1].ToString() == header || response.Values[k][1].ToString() == "")
                {
                    if (response.Values[k][2].ToString() == "")
                    {
                        break;
                    }
                    string dl = response.Values[k][5].ToString().Contains("=HYPERLINK") ? $"[{response.Values[k][5].ToString().Split('"')[3]}]({response.Values[k][5].ToString().Split('"')[1]})" : "-";
                    description += response.Values[k][2].ToString() == "TBD" ? "\n*TBD*" : $"\n{response.Values[k][2]} {response.Values[k][4]} | {response.Values[k][3]} | {dl}";
                    k++;
                }

                if (response.Values[k][2].ToString() == "")
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = title,
                        Description = description,
                        Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1751905284",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    embeds.Add(embed);
                    k++;
                    if (k >= response.Values.Count)
                    {
                        goto EndOfUpdates;
                    }
                    title = $"__**{response.Values[k][1]}:**__";
                    k += 2;
                    goto NewEmbed;
                }
                description += $"\n**{response.Values[k][1]}:**";
                goto NewSection;

            EndOfUpdates:
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

        [SlashCommand("summary", "Gets the summary review from submission of the track specified.")]
        public static async Task GetSummary(InteractionContext ctx,
            [Option("track-name", "The track name of the summary requested.")] string track)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });

                string serviceAccountEmail = "sheetbox@sonic-fiber-399810.iam.gserviceaccount.com";

                X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));

                SheetsService service = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Mario Kart Brawlbot",
                });

                SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Track Evaluation Log'");
                ValueRange response = await request.ExecuteAsync();
                foreach (var t in response.Values)
                {
                    while (t.Count < 7)
                    {
                        t.Add("");
                    }
                }

                List<int> ixs = new();
                for (int i = 0; i < response.Values.Count; i++)
                {
                    if (Util.CompareIncompleteStrings(response.Values[i][2].ToString(), track))
                    {
                        ixs.Add(i);
                    }
                }

                if (ixs.Count == 0)
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = $"*{track} could not be found.*",
                        Url = "https://wiki.tockdom.com/wiki/CTGP_Revolution/Track_Wishlist",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }

                List<DiscordEmbed> embeds = new();

                foreach (var ix in ixs)
                {
                    string trackName = response.Values[ix][2].ToString();

                    int titleIx = ix;

                    while (response.Values[titleIx][0].ToString() != "delimiter")
                    {
                        titleIx--;
                    }
                    if (response.Values[ix][2].ToString() == trackName)
                    {
                        string description = string.Empty;

                        string dateString = response.Values[titleIx][1].ToString();

                        var tally = response.Values[ix][1].ToString().split("\n");
                        if (tally[0].ToString() == "✘")
                        {
                            tally[0] = DiscordEmoji.FromName(ctx.Client, ":x:");
                        }
                        else if (tally[0].ToString() == "✔")
                        {
                            tally[0] = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                        }
                        if (tally.Count == 1)
                        {
                            tally.Add("");
                        }
                        if (response.Values[ix][6].ToString().ToCharArray().Length > 3499)
                        {
                            description = $"__**{dateString}**__\n**{response.Values[ix][2]} {response.Values[ix][4]} - {response.Values[ix][3]}**\n{tally[1]} {tally[0]}\n\n{response.Values[ix][6].ToString().Remove(3499)}...\n\n*For full summary go to the [Track Evaluation Log](https://docs.google.com/spreadsheets/d/1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss/edit#gid=798417105).*";
                        }
                        else
                        {
                            description = $"__**{dateString}**__\n**{response.Values[ix][2]} {response.Values[ix][4]} - {response.Values[ix][3]}**\n{tally[1]} {tally[0]}\n\n{response.Values[ix][6]}";
                        }

                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Summary for {trackName}:**__",
                            Description = description,
                            Url = "https://wiki.tockdom.com/wiki/CTGP_Revolution/Track_Wishlist",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };

                        embeds.Add(embed);
                    }
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

        [SlashCommand("info", "Gets the information for the track specified.")]
        public static async Task GetTrackInfo(InteractionContext ctx,
            [Option("track-name", "The track the information is being requested for.")] string track)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);
                using MKBBContext dbCtx = new();
                List<TrackData> trackList = dbCtx.Tracks.ToList();

                int ix = Util.ListNameCheck(trackList, track);

                string description = $"**Author:**\n*{trackList[ix].Authors}*\n**Version:**\n*{trackList[ix].Version}*\n**Track/Music Slots:**\n*{trackList[ix].TrackSlot} / {trackList[ix].MusicSlot}*\n**Speed/Lap Count:**\n*{trackList[ix].SpeedMultiplier} / {trackList[ix].LapCount}*";

                if (ix < 0)
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = $"*{track} could not be found.\nThe track does not exist, or is not in CTGP.*",
                        Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1971102004",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**{trackList[ix].Name} *(First result)*:**__",
                        Description = description,
                        Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1971102004",
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

        [SlashCommand("pop", "Gets the amount of times a track has been played on Wiimmfi for the last 3 months")]
        public static async Task PopularityRequest(InteractionContext ctx,
            [Option("search", "Can use rts/cts to get a leaderboard, or input a track name to get the popularity for it.")] string arg,
            [Choice("M1", "m1")]
            [Choice("M2", "m2")]
            [Choice("M3", "m3")]
            [Choice("M6", "m6")]
            [Choice("M9", "m9")]
            [Choice("M12", "m12")]
            [Option("stat-duration", "How many months to check for plays in.")] string month = "m3",
            [Choice("Online", "online")]
            [Choice("Time Trials", "tts")]
            [Option("metric-category", "Can specify either online or time trial popularity.")] string metric = "online")
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
                arg = Util.Convert3DSTrackName(arg);

                List<List<DiscordEmbed>> categories = new();
                List<string> categoryNames = new List<string> { "All", "Worldwides", "Friend Rooms" };


                for (int c = 0; c < categoryNames.Count(); c++)
                {
                    List<DiscordEmbed> embeds = GeneratePopularityEmbeds(arg, month, metric, c < 2, c != 1);
                    if (embeds[0].Description == "")
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Error:**__",
                            Description = $"*{arg} could not be found.*",
                            Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }
                    categories.Add(embeds);
                }

                if (arg.ToLowerInvariant().Contains("rts") || arg.ToLowerInvariant().Contains("cts"))
                {
                    var messageBuilder = new DiscordWebhookBuilder().AddEmbed(categories[0][0]);

                    if (metric == "online") messageBuilder.AddComponents(Util.GenerateCategorySelectMenu(categoryNames, 0));

                    messageBuilder.AddComponents(Util.GeneratePageArrows());

                    var message = await ctx.EditResponseAsync(messageBuilder);

                    PendingPagesInteraction pending = new();

                    if (metric == "online")
                    {
                        pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = categories[0], Categories = categories, CategoryNames = categoryNames };
                    }
                    else
                    {
                        pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = categories[0] };
                    }

                    Util.PendingPageInteractions.Add(pending);
                }
                else
                {
                    var messageBuilder = new DiscordWebhookBuilder().AddEmbed(categories[0][0]);

                    if (metric == "online") messageBuilder.AddComponents(Util.GenerateCategorySelectMenu(categoryNames, 0));

                    var message = await ctx.EditResponseAsync(messageBuilder);

                    PendingPagesInteraction pending = new();

                    if (metric == "online")
                    {
                        pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = categories[0], Categories = categories, CategoryNames = categoryNames };
                    }
                    else
                    {
                        pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = categories[0] };
                    }

                    Util.PendingPageInteractions.Add(pending);
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        public static List<DiscordEmbed> GeneratePopularityEmbeds(string arg, string month, string metric, bool wws, bool frooms)
        {
            string description1 = string.Empty;
            string description2 = string.Empty;
            string description3 = string.Empty;
            string description4 = string.Empty;
            string description5 = string.Empty;
            string description6 = string.Empty;
            string description7 = string.Empty;
            string description8 = string.Empty;
            string description9 = string.Empty;
            string description10 = string.Empty;
            string description11 = string.Empty;

            using MKBBContext dbCtx = new();
            List<TrackData> trackListCts = dbCtx.Tracks.AsEnumerable()
                .Where(x => (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut") && x.CustomTrack && !x.Is200cc)
                .DistinctBy(x => x.Name)
                .OrderByDescending(x => metric == "online" ? x.ReturnOnlinePopularity(month, wws, frooms) : x.TimeTrialPopularity)
                .ToList();

            List<TrackData> trackListRts = dbCtx.Tracks.AsEnumerable()
                .Where(x => (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut") && !x.CustomTrack && !x.Is200cc)
                .DistinctBy(x => x.Name)
                .OrderByDescending(x => metric == "online" ? x.ReturnOnlinePopularity(month, wws, frooms) : x.TimeTrialPopularity)
                .ToList();

            List<DiscordEmbed> embeds = new();

            if (arg.ToLowerInvariant().Contains("rts"))
            {
                for (int i = 0; i < 21; i++)
                {
                    description1 += $"**{i + 1})** {trackListRts[i].Name} *({(metric == "online" ? trackListRts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListRts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 21; i < 32; i++)
                {
                    description2 += $"**{i + 1})** {trackListRts[i].Name} *({(metric == "online" ? trackListRts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListRts[i].TimeTrialPopularity)})*\n";
                }
                embeds = new(){
                    new DiscordEmbedBuilder
                    {
                            Color = new DiscordColor("#FF0000"),
                            Title = metric == "online" ? $"__**Displaying 1-21 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 1-21 [Time Trials]:**__",
                            Description = description1,
                            Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = metric == "online" ? $"__**Displaying 22-32 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 22-32 [Time Trials]:**__",
                            Description = description2,
                            Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }
                    };
            }

            else if (arg.ToLowerInvariant().Contains("cts"))
            {
                for (int i = 0; i < 21; i++)
                {
                    description1 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 21; i < 42; i++)
                {
                    description2 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 42; i < 63; i++)
                {
                    description3 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 63; i < 84; i++)
                {
                    description4 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 84; i < 105; i++)
                {
                    description5 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 105; i < 126; i++)
                {
                    description6 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 126; i < 147; i++)
                {
                    description7 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 147; i < 168; i++)
                {
                    description8 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 168; i < 189; i++)
                {
                    description9 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 189; i < 210; i++)
                {
                    description10 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                for (int i = 210; i < 218; i++)
                {
                    description11 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                }
                embeds = new(){
                new DiscordEmbedBuilder
                {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 1-21 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 1-21 [Time Trials]:**__",
                        Description = description1,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                        new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 22-42 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 22-42 [Time Trials]:**__",
                        Description = description2,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 43-63 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 43-63 [Time Trials]:**__",
                        Description = description3,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 64-84 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 64-84 [Time Trials]:**__",
                        Description = description4,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 85-105 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 85-105 [Time Trials]:**__",
                        Description = description5,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 106-126 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 106-126 [Time Trials]:**__",
                        Description = description6,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 127-147 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 127-147 [Time Trials]:**__",
                        Description = description7,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 148-168 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 148-168 [Time Trials]:**__",
                        Description = description8,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 169-189 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 169-189 [Time Trials]:**__",
                        Description = description9,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 190-210 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 190-210 [Time Trials]:**__",
                        Description = description10,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    },
                    new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = metric == "online" ? $"__**Displaying 211-218 [Online] *({month.ToUpper()})*:**__" : $"__**Displaying 211-218 [Time Trials]:**__",
                        Description = description11,
                        Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }
                };
            }
            else
            {
                int c = 0;
                int d = 0;
                description1 = $"__**Nintendo Tracks**__:\n";
                for (int i = 0; i < trackListRts.Count; i++)
                {
                    if (Util.CompareStrings(trackListRts[i].Name, arg) || Util.CompareIncompleteStrings(trackListRts[i].Name, arg) || Util.CompareStringAbbreviation(arg, trackListRts[i].Name) || Util.CompareStringsLevenshteinDistance(arg, trackListRts[i].Name))
                    {
                        description1 += $"**{i + 1})** {trackListRts[i].Name} *({(metric == "online" ? trackListRts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListRts[i].TimeTrialPopularity)})*\n";
                    }
                }
                if (description1 == $"__**Nintendo Tracks**__:\n")
                {
                    description1 = $"__**Custom Tracks**__:\n";
                    d = description1.ToCharArray().Length;
                }
                else
                {
                    d = description1.ToCharArray().Length;
                    description1 += $"__**Custom Tracks**__:\n";
                }
                for (int i = 0; i < trackListCts.Count; i++)
                {
                    if (Util.CompareStrings(trackListCts[i].Name, arg) || Util.CompareIncompleteStrings(trackListCts[i].Name, arg) || Util.CompareStringAbbreviation(arg, trackListCts[i].Name) || Util.CompareStringsLevenshteinDistance(arg, trackListCts[i].Name))
                    {
                        description1 += $"**{i + 1})** {trackListCts[i].Name} *({(metric == "online" ? trackListCts[i].ReturnOnlinePopularity(month, wws, frooms) : trackListCts[i].TimeTrialPopularity)})*\n";
                        c++;
                    }
                }
                if (c == 0)
                {
                    if (description1.Contains($"__**Nintendo Tracks**__:\n"))
                    {
                        description1 = description1.Remove(d - 1, 23);
                    }
                    else
                    {
                        description1 = description1.Remove(0, d);
                    }
                }
                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = metric == "online" ? $"__**Displaying tracks containing *{arg} ({month.ToUpper()})* [Online]:**__" : $"__**Displaying tracks containing *{arg}* [Time Trials]:**__",
                    Description = description1,
                    Url = metric == "online" ? "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c2,0" : "https://chadsoft.co.uk/time-trials/",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                embeds.Add(embed);
            }
            return embeds;
        }

        [SlashCommand("rating", "Gets the ranking from the most recent track rating forms.")]
        public static async Task GetTrackRating(InteractionContext ctx,
            [Option("track-name", "The track that the best-known times are set on.")] string track = "")
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = Util.CheckEphemeral(ctx) });
                track = Util.Convert3DSTrackName(track);

                string description = "";
                string serviceAccountEmail = "sheetbox@sonic-fiber-399810.iam.gserviceaccount.com";

                X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));

                SheetsService service = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Mario Kart Brawlbot",
                });

                SpreadsheetsResource.ValuesResource.GetRequest request = null;
                ValueRange response = null;
                List<string> earlyTrackDisplay = new();
                List<string> midTrackDisplay = new();

                bool thisYear = false;
                bool halfLastYear = false;
                bool lastYear = false;

                try
                {
                    if (DateTime.Now.Month > 6)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        earlyTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                earlyTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }

                        midTrackDisplay = new List<string>();
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                midTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        thisYear = true;
                    }
                    else
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        earlyTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                earlyTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }

                        midTrackDisplay = new List<string>();
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                midTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        halfLastYear = true;
                    }
                }
                catch
                {
                    if (DateTime.Now.Month > 6)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        earlyTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                earlyTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        midTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                midTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        halfLastYear = true;
                    }
                    else
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        earlyTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                earlyTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                        response = await request.ExecuteAsync();

                        midTrackDisplay = new List<string>();

                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareStrings(response.Values[i][0].ToString(), track) || Util.CompareIncompleteStrings(response.Values[i][0].ToString(), track) || Util.CompareStringAbbreviation(track, response.Values[i][0].ToString()) || Util.CompareStringsLevenshteinDistance(track, response.Values[i][0].ToString()))
                            {
                                midTrackDisplay.Add(response.Values[i][0].ToString());
                            }
                        }
                        lastYear = true;
                    }
                }
                if (!thisYear && !halfLastYear && !lastYear)
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Error:**__",
                        Description = $"*{track} was not found in the latest track rating polls.*",
                        Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=595190106",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else if (track == "")
                {
                    response.Values = response.Values.OrderBy(t => int.Parse(t[6].ToString())).ToList();
                    string description1 = "";
                    string description2 = "";
                    string description3 = "";
                    string description4 = "";
                    string description5 = "";
                    string description6 = "";
                    string description7 = "";
                    string description8 = "";
                    string description9 = "";
                    string description10 = "";
                    string description11 = "";
                    for (int i = 0; i < 21; i++)
                    {
                        description1 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 21; i < 42; i++)
                    {
                        description2 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 42; i < 63; i++)
                    {
                        description3 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 63; i < 84; i++)
                    {
                        description4 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 84; i < 105; i++)
                    {
                        description5 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 105; i < 126; i++)
                    {
                        description6 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 126; i < 147; i++)
                    {
                        description7 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 147; i < 168; i++)
                    {
                        description8 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 168; i < 189; i++)
                    {
                        description9 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 189; i < 210; i++)
                    {
                        description10 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    for (int i = 210; i < 218; i++)
                    {
                        description11 += $"**{i + 1})** {response.Values[i][0]} *({Util.RankNumber(response.Values[i][12].ToString())} / {Util.RankNumber(response.Values[i][18].ToString())} / {Util.RankNumber(response.Values[i][24].ToString())})*\n";
                    }
                    List<DiscordEmbed> embeds = new()
                    {
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 1-21 (Comp/Non-comp/Creators):**__",
                            Description = description1,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                            new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 22-42 (Comp/Non-comp/Creators):**__",
                            Description = description2,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 43-63 (Comp/Non-comp/Creators):**__",
                            Description = description3,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 64-84 (Comp/Non-comp/Creators):**__",
                            Description = description4,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 85-105 (Comp/Non-comp/Creators):**__",
                            Description = description5,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 106-126 (Comp/Non-comp/Creators):**__",
                            Description = description6,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 127-147 (Comp/Non-comp/Creators):**__",
                            Description = description7,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 148-168 (Comp/Non-comp/Creators):**__",
                            Description = description8,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 169-189 (Comp/Non-comp/Creators):**__",
                            Description = description9,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 190-210 (Comp/Non-comp/Creators):**__",
                            Description = description10,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        },
                        new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Displaying 211-218 (Comp/Non-comp/Creators):**__",
                            Description = description11,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }
                    };
                    var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embeds[0]).AddComponents(Util.GeneratePageArrows()));

                    PendingPagesInteraction pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = embeds };

                    Util.PendingPageInteractions.Add(pending);
                }
                else
                {

                    if (thisYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year} Track Rating Graphs'");
                    }
                    else if (halfLastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Graphs'");
                    }
                    else if (lastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Graphs'");
                    }
                    response = await request.ExecuteAsync();
                    foreach (var t in response.Values)
                    {
                        while (t.Count < 15)
                        {
                            t.Add("");
                        }
                    }

                    int ix = -1;

                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        if (response.Values[i][12].ToString().Contains("Average Track"))
                        {
                            ix = i + 2;
                            break;
                        }
                    }

                    string secondAverage = $"{Math.Round((double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}/{Math.Round((double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}/{Math.Round((double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}";



                    if (thisYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                    }
                    else if (halfLastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                    }
                    else if (lastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                    }
                    response = await request.ExecuteAsync();

                    if (midTrackDisplay.Count > 0)
                    {
                        if (thisYear)
                        {
                            description += $"__**Mid {DateTime.Now.Year} Track Rating Data (Average: {secondAverage}%):**__\n";
                        }
                        else if (halfLastYear)
                        {
                            description += $"__**Early {DateTime.Now.Year} Track Rating Data (Average: {secondAverage}%):**__\n";
                        }
                        else if (lastYear)
                        {
                            description += $"__**Mid {DateTime.Now.Year - 1} Track Rating Data (Average: {secondAverage}%):**__\n";
                        }
                        for (int i = 0; i < midTrackDisplay.Count; i++)
                        {
                            foreach (var t in response.Values)
                            {
                                if (midTrackDisplay[i] == t[0].ToString())
                                {
                                    description += $"__{t[0]}__:\nAll - {Math.Round((double.Parse(t[2].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}/{Math.Round((double.Parse(t[3].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}/{Math.Round((double.Parse(t[4].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}% - {Util.RankNumber(t[6].ToString())}\n";
                                    description += $"Comp - {Math.Round((double.Parse(t[8].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}/{Math.Round((double.Parse(t[9].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}/{Math.Round((double.Parse(t[10].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}% - {Util.RankNumber(t[12].ToString())}\n";
                                    description += $"Non-Comp - {Math.Round((double.Parse(t[14].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}/{Math.Round((double.Parse(t[15].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}/{Math.Round((double.Parse(t[16].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}% - {Util.RankNumber(t[18].ToString())}\n";
                                    description += $"Creators - {Math.Round((double.Parse(t[20].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}/{Math.Round((double.Parse(t[21].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}/{Math.Round((double.Parse(t[22].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}% - {Util.RankNumber(t[24].ToString())}\n";
                                }
                            }
                        }
                    }

                    if (thisYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Graphs'");
                    }
                    else if (halfLastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Graphs'");
                    }
                    else if (lastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year - 1} Track Rating Graphs'");
                    }
                    response = await request.ExecuteAsync();
                    foreach (var t in response.Values)
                    {
                        while (t.Count < 15)
                        {
                            t.Add("");
                        }
                    }

                    ix = -1;

                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        if (response.Values[i][12].ToString().Contains("Average Track"))
                        {
                            ix = i + 2;
                            break;
                        }
                    }

                    string firstAverage = $"{Math.Round((double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}/{Math.Round((double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}/{Math.Round((double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)) / (double.Parse(response.Values[ix][12].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][13].ToString().Replace("%", string.Empty)) + double.Parse(response.Values[ix][14].ToString().Replace("%", string.Empty)))) * 100)}";

                    if (thisYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year} Track Rating Data'!A226:Y443");
                    }
                    else if (halfLastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Mid {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                    }
                    else if (lastYear)
                    {
                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", $"'Early {DateTime.Now.Year - 1} Track Rating Data'!A226:Y443");
                    }
                    response = await request.ExecuteAsync();

                    if (earlyTrackDisplay.Count > 0)
                    {
                        if (thisYear)
                        {
                            description += $"__**Early {DateTime.Now.Year} Track Rating Data (Average: {firstAverage}%):**__\n";
                        }
                        else if (halfLastYear)
                        {
                            description += $"__**Mid {DateTime.Now.Year - 1} Track Rating Data (Average: {firstAverage}%):**__\n";
                        }
                        else if (lastYear)
                        {
                            description += $"__**Early {DateTime.Now.Year - 1} Track Rating Data (Average: {firstAverage}%):**__\n";
                        }
                        for (int i = 0; i < earlyTrackDisplay.Count; i++)
                        {
                            foreach (var t in response.Values)
                            {
                                if (earlyTrackDisplay[i] == t[0].ToString())
                                {
                                    description += $"__{t[0]}__:\nAll - {Math.Round((double.Parse(t[2].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}/{Math.Round((double.Parse(t[3].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}/{Math.Round((double.Parse(t[4].ToString()) / (double.Parse(t[2].ToString()) + double.Parse(t[3].ToString()) + double.Parse(t[4].ToString()))) * 100)}% - {Util.RankNumber(t[6].ToString())}\n";
                                    description += $"Comp - {Math.Round((double.Parse(t[8].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}/{Math.Round((double.Parse(t[9].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}/{Math.Round((double.Parse(t[10].ToString()) / (double.Parse(t[8].ToString()) + double.Parse(t[9].ToString()) + double.Parse(t[10].ToString()))) * 100)}% - {Util.RankNumber(t[12].ToString())}\n";
                                    description += $"Non-Comp - {Math.Round((double.Parse(t[14].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}/{Math.Round((double.Parse(t[15].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}/{Math.Round((double.Parse(t[16].ToString()) / (double.Parse(t[14].ToString()) + double.Parse(t[15].ToString()) + double.Parse(t[16].ToString()))) * 100)}% - {Util.RankNumber(t[18].ToString())}\n";
                                    description += $"Creators - {Math.Round((double.Parse(t[20].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}/{Math.Round((double.Parse(t[21].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}/{Math.Round((double.Parse(t[22].ToString()) / (double.Parse(t[20].ToString()) + double.Parse(t[21].ToString()) + double.Parse(t[22].ToString()))) * 100)}% - {Util.RankNumber(t[24].ToString())}\n";
                                }
                            }
                        }
                    }

                    if (description.ToCharArray().Length == 0)
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Error:**__",
                            Description = $"*{track} was not found in the latest track rating polls.*",
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=595190106",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Track Ratings for {track} (Remove/Indifferent/Keep - Rank):**__",
                            Description = description,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=595190106",
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

        [SlashCommand("issues", "Displays a list of issues in order of issue count.")]
        public static async Task GetTrackIssues(InteractionContext ctx,
            [Option("track-name", "The track that the issues were found on.")] string track = "")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id == 180306609233330176 ? !(ctx.Channel.Id == 842035247734587453 || !Util.CheckEphemeral(ctx)) : Util.CheckEphemeral(ctx) });
            track = Util.Convert3DSTrackName(track);

            var json = string.Empty;
            var description = string.Empty;

            string serviceAccountEmail = "sheetbox@sonic-fiber-399810.iam.gserviceaccount.com";

            X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new(
               new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));

            SheetsService service = new(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mario Kart Brawlbot",
            });

            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'CTGP Track Issues'");
            ValueRange response = await request.ExecuteAsync();
            foreach (var t in response.Values)
            {
                while (t.Count < 7)
                {
                    t.Add("");
                }
            }

            try
            {
                string maj = string.Empty;
                string min = string.Empty;

                DiscordButtonComponent sendLinksButton = new(
                    ButtonStyle.Primary,
                    "issuesEmbedding",
                    "Embed Images/Videos");

                if (track == "")
                {
                    int j = 0;
                    Dictionary<string, int> issueCount = new();

                    foreach (var v in response.Values)
                    {
                        if (v[0].ToString() != "Track")
                        {
                            int count = v[5].ToString().Count(c => c == '\n') + v[6].ToString().Count(c => c == '\n');
                            if (v[5].ToString().ToCharArray().Length != 0 && v[5].ToString().ToCharArray()[0] == '-')
                            {
                                count++;
                            }
                            if (v[6].ToString().ToCharArray().Length != 0 && v[6].ToString().ToCharArray()[0] == '-')
                            {
                                count++;
                            }
                            issueCount.Add(v[0].ToString(), count);
                        }
                    }
                    issueCount = issueCount.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                    List<DiscordEmbed> embeds = new();

                    foreach (var t in issueCount.Keys.ToList())
                    {
                        for (int i = 0; i < response.Values.Count; i++)
                        {
                            if (Util.CompareIncompleteStrings(t, response.Values[i][0].ToString()))
                            {
                                if (response.Values[i][5].ToString() == "")
                                {
                                    maj = "- No reported bugs";
                                }
                                else
                                {
                                    maj = response.Values[i][5].ToString().Trim();
                                }
                                if (response.Values[i][6].ToString() == "")
                                {
                                    min = "- No reported bugs";
                                }
                                else
                                {
                                    min = response.Values[i][6].ToString().Trim();
                                }
                                description = $"**Major:**\n*{maj}*\n**Minor:**\n*{min}*";
                                j++;

                                DiscordEmbedBuilder embed = new()
                                {
                                    Color = new DiscordColor("#FF0000"),
                                    Title = $"__**Known issues on {response.Values[i][0]}:**__",
                                    Description = description,
                                    Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1971102004",
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                                    }
                                };
                                embeds.Add(embed);
                            }
                        }
                    }

                    DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embeds[0]).AddComponents(Util.GeneratePageArrows()).AddComponents(sendLinksButton));

                    PendingPagesInteraction pending = new() { CurrentPage = 0, MessageId = message.Id, Context = ctx, Pages = embeds };

                    Util.PendingPageInteractions.Add(pending);
                }
                else
                {
                    int ix = Util.ListNameCheck(response.Values, track, ix2: 0);

                    if (ix > -1)
                    {
                        if (response.Values[ix][5].ToString() == "")
                        {
                            maj = "- No reported bugs";
                        }
                        else
                        {
                            maj = response.Values[ix][5].ToString().Trim();
                        }
                        if (response.Values[ix][6].ToString() == "")
                        {
                            min = "- No reported bugs";
                        }
                        else
                        {
                            min = response.Values[ix][6].ToString().Trim();
                        }
                        description = $"## Major:\n*{maj}*\n## Minor:\n*{min}*";
                    }

                    if (ix < 0)
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "__**Error:**__",
                            Description = $"*{track} could not be found.\nThe track does not exist, or is not in CTGP.*",
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1971102004",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    }
                    else
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Known issues on {response.Values[ix][0]} *(First result)*:**__",
                            Description = description,
                            Url = "https://docs.google.com/spreadsheets/d/1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM/edit#gid=1971102004",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(sendLinksButton));
                    }
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }
    }
}