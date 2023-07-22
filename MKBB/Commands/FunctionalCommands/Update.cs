using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using FluentScheduler;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HtmlAgilityPack;
using Microsoft.Scripting.Utils;
using MKBB.Class;
using MKBB.Data;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace MKBB.Commands
{
    public class Update : ApplicationCommandModule
    {
        [SlashCommand("starttimer", "Starts the timer for updating data for the bot.")]
        [SlashRequireOwner]
        public static async Task StartTimers(InteractionContext ctx)
        {
            if (ctx != null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }

            JobManager.Stop();
            JobManager.RemoveAllJobs();

            Util.ScheduleRegister = new Registry();
            Util.ScheduleRegister.Schedule(async () => await ExecuteTimer(ctx)).ToRunEvery(1).Days().At(12, 0);

            JobManager.Initialize(Util.ScheduleRegister);

            if (ctx != null)
            {
                DiscordEmbedBuilder embed = new()
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
        }

        public static async Task ExecuteTimer(InteractionContext ctx)
        {
            Util.PendingPageInteractions = new List<PendingPagesInteraction>();
            Util.PendingChannelConfigInteractions = new List<PendingChannelConfigInteraction>();
            try
            {
                await CheckStrikesInit(ctx);
                await UpdateInit(ctx);
                //await RefreshButtons(ctx);
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }

        [SlashCommand("update", "Updates the database with information from Chadsoft and Wiimmfi.")]
        [SlashRequireOwner]
        public static async Task UpdateInit(InteractionContext ctx)
        {
            if (ctx != null && ctx.CommandName == "update")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }

            try
            {
                await UpdateDatabase();
                if (ctx != null && ctx.CommandName == "update")
                {
                    DiscordEmbedBuilder embed = new()
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
                if (ctx != null && ctx.CommandName == "update")
                {
                    await Util.ThrowError(ctx, ex);
                }
                else
                {
                    await Util.ThrowInteractionlessError(ex);
                }
            }
        }

        public static async Task UpdateDatabase()
        {
            string rtttUrl = "http://tt.chadsoft.co.uk/original-track-leaderboards.json";
            string ctttUrl = "http://tt.chadsoft.co.uk/ctgp-leaderboards.json";
            string rttt200Url = "http://tt.chadsoft.co.uk/original-track-leaderboards-200cc.json";
            string cttt200Url = "http://tt.chadsoft.co.uk/ctgp-leaderboards-200cc.json";
            string ctwwUrl1 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0";
            string ctwwUrl2 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0,100";
            string ctwwUrl3 = "https://wiimmfi.de/stats/track/mv/ctgp?m=json&p=std,c2,0,200";
            string wwUrl = "https://wiimmfi.de/stats/track/mv/ww?m=json&p=std,c2,0";
            string froomUrl1 = "https://wiimmfi.de/stats/track/mv/priv?m=json&p=std,c2,0";
            string froomUrl2 = "https://wiimmfi.de/stats/track/mv/priv?m=json&p=std,c2,0,100";
            string froomUrl3 = "https://wiimmfi.de/stats/track/mv/priv?m=json&p=std,c2,0,200";

            using MKBBContext dbCtx = new();

            // Leaderboards

            WebClient webClient = new()
            {
                Encoding = Encoding.UTF8
            };

            List<TrackData> currentTracks = dbCtx.Tracks.ToList();

            // Get most up to date data from Chadsoft

            var newChadsoftData = JsonConvert.DeserializeObject<List<Track>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(rtttUrl)).Leaderboard));
            foreach (var track in JsonConvert.DeserializeObject<List<Track>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(ctttUrl)).Leaderboard)))
            {
                newChadsoftData.Add(track);
            }
            foreach (var track in JsonConvert.DeserializeObject<List<Track>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(rttt200Url)).Leaderboard)))
            {
                newChadsoftData.Add(track);
            }
            foreach (var track in JsonConvert.DeserializeObject<List<Track>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<LeaderboardInfo>(await webClient.DownloadStringTaskAsync(cttt200Url)).Leaderboard)))
            {
                newChadsoftData.Add(track);
            }

            foreach (var track in newChadsoftData)
            {
                if (currentTracks.Where(x => x.SHA1 == track.SHA1).Any()) // If the track already exists in the database
                {
                    foreach (var t in dbCtx.Tracks.Where(x => x.SHA1 == track.SHA1 && x.LeaderboardLink == track.LinkContainer.Href.URL))
                    {
                        t.LastChanged = track.ConvertData().LastChanged;
                        t.TimeTrialPopularity = track.ConvertData().TimeTrialPopularity;
                        t.Is200cc = track.ConvertData().Is200cc;
                    }
                }
                else // If the track is a new track or an update
                {
                    var newTrack = track.ConvertData();
                    newTrack.LeaderboardLink = track.LinkContainer.Href.URL;
                    newTrack.CustomTrack = true;
                    HtmlWeb web = new()
                    {
                        UserAgent = Util.GetUserAgent()
                    };
                    HtmlDocument wikiPage = await web.LoadFromWebAsync("https://wiki.tockdom.com/wiki/CTGP_Revolution");
                    var tds = wikiPage.DocumentNode.SelectNodes("//table/tbody/tr/td");
                    for (int i = 0; i < tds.Count; i++)
                    {
                        if (tds[i].InnerText.Replace("\n", "") == newTrack.Name)
                        {
                            newTrack.SlotID = tds[i + 6].InnerText.Replace("\n", string.Empty);
                            Console.WriteLine(newTrack.Name + ": " + newTrack.SlotID);
                            break;
                        }
                    }
                    string serviceAccountEmail = "brawlbox@custom-track-testing-bot.iam.gserviceaccount.com";

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
                    foreach (var c in response.Values)
                    {
                        while (c.Count < 11)
                        {
                            c.Add("");
                        }
                    }
                    foreach (var v in response.Values)
                    {
                        if (Util.Convert3DSTrackName(v[2].ToString()) == newTrack.Name)
                        {
                            newTrack.Authors = v[3].ToString();
                            newTrack.Version = v[4].ToString();
                            newTrack.TrackSlot = v[6].ToString().Split('/')[0].Trim(' ');
                            newTrack.MusicSlot = Regex.Replace(v[6].ToString().Split('/')[1], @"\s+", " ").TrimStart();
                            newTrack.SpeedMultiplier = decimal.Parse(v[7].ToString().Split('/')[0].Trim(' '));
                            newTrack.LapCount = int.Parse(v[7].ToString().Split('/')[1].Trim(' '));
                            if (dbCtx.Tracks.ToList().FindIndex(x => x.SHA1 == newTrack.SHA1) == -1)
                            {
                                newTrack.EasyStaffSHA1 = v[9].ToString();
                                newTrack.ExpertStaffSHA1 = v[11].ToString();
                            }
                        }
                    }
                    HtmlDocument leaderboardPage = new();
                    leaderboardPage.LoadHtml(await webClient.DownloadStringTaskAsync($"https://chadsoft.co.uk/time-trials{newTrack.LeaderboardLink.Replace("json", "html")}"));
                    Console.WriteLine($"Downloading leaderboard for {newTrack.Name}");
                    var h1s = leaderboardPage.DocumentNode.SelectNodes("//h1");
                    foreach (var h1 in h1s)
                    {
                        if (!h1.InnerText.Contains("No-shortcut") && !h1.InnerText.Contains("Shortcut") && !h1.InnerText.Contains("Glitch"))
                        {
                            newTrack.CategoryName = "Normal";
                        }
                        else if (h1.InnerText.Contains("No-shortcut"))
                        {
                            newTrack.CategoryName = "No-shortcut";
                        }
                        else if (h1.InnerText.Contains("Shortcut"))
                        {
                            newTrack.CategoryName = "Shortcut";
                        }
                        else if (h1.InnerText.Contains("Glitch"))
                        {
                            newTrack.CategoryName = "Glitch";
                        }
                        else
                        {
                            newTrack.CategoryName = h1.InnerText.Split(' ')[^1];
                        }
                    }
                    foreach (var ct in dbCtx.Tracks.Where(x => x.SlotID == newTrack.SlotID && x.SHA1 != newTrack.SHA1))
                    {
                        dbCtx.OldTracks.Add(ct.ConvertToOld());
                        dbCtx.Tracks.Remove(ct);

                        request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'CTGP Track Issues'!A2:H219");
                        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
                        response = await request.ExecuteAsync();
                        foreach (var c in response.Values)
                        {
                            while (c.Count < 7)
                            {
                                c.Add("");
                            }
                        }
                        var ix = response.Values.FindIndex(x => x[0].ToString() == ct.Name);
                        if (ix != -1)
                        {
                            response.Values[ix][0] = newTrack.Name;
                            response.Values[ix][1] = newTrack.Authors;
                            response.Values[ix][2] = newTrack.Version;
                            response.Values[ix][3] = $"{newTrack.TrackSlot} / {newTrack.MusicSlot}";
                            response.Values[ix][4] = $"{newTrack.SpeedMultiplier} / {newTrack.LapCount}";
                            response.Values[ix][5] = "";
                            response.Values[ix][6] = "";
                            response.Values = response.Values.OrderBy(x => x[0]).ToList();
                            var updateRequest = service.Spreadsheets.Values.Update(response, "1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'CTGP Track Issues'!A2:H219");
                            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                            var update = await updateRequest.ExecuteAsync();
                        }
                    }
                    dbCtx.Tracks.Add(newTrack);
                    dbCtx.SaveChanges();
                }
            }
            dbCtx.SaveChanges();

            // Get most up to date data from Wiimmfi
            // // Nintendo Tracks

            HtmlDocument wwHtml = new();
            wwHtml.LoadHtml(await webClient.DownloadStringTaskAsync(wwUrl));
            var innerText1 = wwHtml.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-')]/td");
            List<string> m1s = new();
            List<string> m2s = new();
            List<string> m3s = new();
            List<string> m6s = new();
            List<string> m9s = new();
            List<string> m12s = new();
            List<string> names = new();
            for (int i = 0; i < 32 * 11; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText1[i].InnerText == "–" ? "0" : innerText1[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
            }
            for (int i = 0; i < names.Count; i++)
            {
                foreach (var t in dbCtx.Tracks)
                {
                    if (names[i].Split('(')[0].Replace("Wii", "").Trim(' ') == t.Name && !t.CustomTrack)
                    {
                        t.WWM1 = int.Parse(m1s[i]);
                        t.WWM2 = int.Parse(m2s[i]);
                        t.WWM3 = int.Parse(m3s[i]);
                        t.WWM6 = int.Parse(m6s[i]);
                        t.WWM9 = int.Parse(m9s[i]);
                        t.WWM12 = int.Parse(m12s[i]);
                        break;
                    }
                }
                dbCtx.SaveChanges();
            }

            HtmlDocument ctwwHtml1 = new();
            HtmlDocument ctwwHtml2 = new();
            HtmlDocument ctwwHtml3 = new();
            ctwwHtml1.LoadHtml(await webClient.DownloadStringTaskAsync(ctwwUrl1));
            var bodyNodes = ctwwHtml1.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]");
            innerText1 = ctwwHtml1.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-')]/td");
            names = new List<string>();
            m1s = new List<string>();
            m2s = new List<string>();
            m3s = new List<string>();
            m6s = new List<string>();
            m9s = new List<string>();
            m12s = new List<string>();
            for (int i = 0; i < innerText1.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText1[i].InnerText == "–" ? "0" : innerText1[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 1st Wiimmfi Page");
            ctwwHtml2.LoadHtml(await webClient.DownloadStringTaskAsync(ctwwUrl2));
            foreach (var n in ctwwHtml2.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]"))
            {
                bodyNodes.Add(n);
            }
            var innerText2 = ctwwHtml2.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-')]/td");
            for (int i = 0; i < innerText2.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText1[i].InnerText == "–" ? "0" : innerText1[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 2nd Wiimmfi Page");
            ctwwHtml3.LoadHtml(await webClient.DownloadStringTaskAsync(ctwwUrl3));
            foreach (var n in ctwwHtml3.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]"))
            {
                bodyNodes.Add(n);
            }
            var innerText3 = ctwwHtml3.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-')]/td");
            for (int i = 0; i < innerText3.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText1[i].InnerText == "–" ? "0" : innerText1[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 3rd Wiimmfi Page");
            for (int i = 0; i < bodyNodes.Count; i++)
            {
                if (bodyNodes[i].InnerHtml.Contains("SHA1"))
                {
                    foreach (var track in dbCtx.Tracks)
                    {
                        if (bodyNodes[i].InnerText.Replace("SHA1: ", "").ToLowerInvariant() == track.SHA1.ToLowerInvariant() && track.CustomTrack)
                        {
                            Console.WriteLine($"Checking SHA1s for {track.Name} from {bodyNodes[i].InnerHtml}");
                            track.WWM1 = int.Parse(m1s[i]);
                            track.WWM2 = int.Parse(m2s[i]);
                            track.WWM3 = int.Parse(m3s[i]);
                            track.WWM6 = int.Parse(m6s[i]);
                            track.WWM9 = int.Parse(m9s[i]);
                            track.WWM12 = int.Parse(m12s[i]);
                        }
                    }
                    dbCtx.SaveChanges();
                }
                else
                {
                    var dl = await webClient.DownloadStringTaskAsync($"{bodyNodes[i].InnerHtml.Split('"')[1]}?m=json");
                    HtmlDocument trackHtml = new();
                    trackHtml.LoadHtml(dl);
                    var tts = trackHtml.DocumentNode.SelectNodes("//tr/td/tt");
                    foreach (var tt in tts)
                    {
                        foreach (var track in dbCtx.Tracks)
                        {
                            if (tt.InnerText.ToLowerInvariant().Contains(track.SHA1.ToLowerInvariant()) && track.CustomTrack)
                            {
                                Console.WriteLine($"Checking SHA1s for {track.Name} from {bodyNodes[i].InnerHtml.Split('"')[1]}");
                                track.WWM1 = int.Parse(m1s[i]);
                                track.WWM2 = int.Parse(m2s[i]);
                                track.WWM3 = int.Parse(m3s[i]);
                                track.WWM6 = int.Parse(m6s[i]);
                                track.WWM9 = int.Parse(m9s[i]);
                                track.WWM12 = int.Parse(m12s[i]);
                            }
                        }
                        dbCtx.SaveChanges();
                    }
                }
            }

            // // Friend Rooms

            HtmlDocument froomHtml1 = new();
            HtmlDocument froomHtml2 = new();
            HtmlDocument froomHtml3 = new();
            froomHtml1.LoadHtml(await webClient.DownloadStringTaskAsync(froomUrl1));
            bodyNodes = froomHtml1.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]");
            innerText1 = froomHtml1.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-')]/td");
            names = new List<string>();
            m1s = new List<string>();
            m2s = new List<string>();
            m3s = new List<string>();
            m6s = new List<string>();
            m9s = new List<string>();
            m12s = new List<string>();
            for (int i = 0; i < innerText1.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText1[i].InnerText == "–" ? "0" : innerText1[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText1[i].InnerHtml == "–" ? "0" : innerText1[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 1st Froom Wiimmfi Page");
            froomHtml2.LoadHtml(await webClient.DownloadStringTaskAsync(froomUrl2));
            foreach (var n in froomHtml2.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]"))
            {
                bodyNodes.Add(n);
            }
            innerText2 = froomHtml2.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-1')]/td");
            for (int i = 0; i < innerText2.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText2[i].InnerText == "–" ? "0" : innerText2[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText2[i].InnerHtml == "–" ? "0" : innerText2[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 2nd Froom Wiimmfi Page");
            froomHtml3.LoadHtml(await webClient.DownloadStringTaskAsync(froomUrl3));
            foreach (var n in froomHtml3.DocumentNode.SelectNodes("//td[contains(@class, 'LL')]"))
            {
                bodyNodes.Add(n);
            }
            innerText3 = froomHtml3.DocumentNode.SelectNodes("//tr[contains(@id, 'p0-2')]/td");
            for (int i = 0; i < innerText3.Count; i++)
            {
                if (i % 11 - 2 == 0)
                {
                    names.Add(innerText3[i].InnerText == "–" ? "0" : innerText3[i].InnerText);
                }
                if (i % 11 - 3 == 0)
                {
                    m1s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 4 == 0)
                {
                    m2s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 5 == 0)
                {
                    m3s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 6 == 0)
                {
                    m6s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 7 == 0)
                {
                    m9s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
                if (i % 11 - 8 == 0)
                {
                    m12s.Add(innerText3[i].InnerHtml == "–" ? "0" : innerText3[i].InnerHtml);
                }
            }
            Console.WriteLine("Downloaded 3rd Froom Wiimmfi Page");
            for (int i = 0; i < bodyNodes.Count; i++)
            {
                if (bodyNodes[i].InnerHtml.Contains("SHA1"))
                {
                    foreach (var track in dbCtx.Tracks)
                    {
                        if (bodyNodes[i].InnerText.Replace("SHA1: ", "").ToLowerInvariant() == track.SHA1.ToLowerInvariant())
                        {
                            Console.WriteLine($"Checking SHA1s for {track.Name} from {bodyNodes[i].InnerHtml}");
                            track.FM1 = int.Parse(m1s[i]);
                            track.FM2 = int.Parse(m2s[i]);
                            track.FM3 = int.Parse(m3s[i]);
                            track.FM6 = int.Parse(m6s[i]);
                            track.FM9 = int.Parse(m9s[i]);
                            track.FM12 = int.Parse(m12s[i]);
                        }
                    }
                    dbCtx.SaveChanges();
                }
                else
                {
                    var dl = await webClient.DownloadStringTaskAsync($"{bodyNodes[i].InnerHtml.Split('"')[1]}?m=json");
                    HtmlDocument trackHtml = new();
                    trackHtml.LoadHtml(dl);
                    var tts = trackHtml.DocumentNode.SelectNodes("//tr/td/tt");
                    foreach (var tt in tts)
                    {
                        foreach (var track in dbCtx.Tracks)
                        {
                            if (tt.InnerText.ToLowerInvariant().Contains(track.SHA1.ToLowerInvariant()))
                            {
                                Console.WriteLine($"Checking SHA1s for {track.Name} from {bodyNodes[i].InnerHtml.Split('"')[1]}");
                                track.FM1 = int.Parse(m1s[i]);
                                track.FM2 = int.Parse(m2s[i]);
                                track.FM3 = int.Parse(m3s[i]);
                                track.FM6 = int.Parse(m6s[i]);
                                track.FM9 = int.Parse(m9s[i]);
                                track.FM12 = int.Parse(m12s[i]);
                            }
                        }
                        dbCtx.SaveChanges();
                    }
                }
            }

#if DEBUG == false
            File.WriteAllText(@"C:\WebApps\MKBB\wwwroot\api\rts.json", JsonConvert.SerializeObject(dbCtx.Tracks.Where(x => !x.CustomTrack && !x.Is200cc).ToList()));
            File.WriteAllText(@"C:\WebApps\MKBB\wwwroot\api\rts200.json", JsonConvert.SerializeObject(dbCtx.Tracks.Where(x => !x.CustomTrack && x.Is200cc).ToList()));
            File.WriteAllText(@"C:\WebApps\MKBB\wwwroot\api\cts.json", JsonConvert.SerializeObject(dbCtx.Tracks.Where(x => x.CustomTrack && !x.Is200cc).ToList()));
            File.WriteAllText(@"C:\WebApps\MKBB\wwwroot\api\cts200.json", JsonConvert.SerializeObject(dbCtx.Tracks.Where(x => x.CustomTrack && x.Is200cc).ToList()));
#endif

            var today = DateTime.Now;
            File.WriteAllText("lastUpdated.txt", today.ToString("dd/MM/yyyy HH:mm:ss"));

            DiscordActivity activity = new()
            {
                Name = $"Last Updated: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}" +
                $" | /help"
            };
            await Bot.Client.UpdateStatusAsync(activity);

        }

        [SlashCommand("checkstrikes", "Checks missed homework and notifies if there is homework due.")]
        [SlashRequireOwner]
        public static async Task CheckStrikesInit(InteractionContext ctx)
        {
            if (ctx != null && ctx.CommandName == "checkstrikes")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }
            try
            {
                await CheckStrikes(ctx);

                if (ctx != null && ctx.CommandName == "checkstrikes")
                {
                    DiscordEmbedBuilder embed = new()
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
                if (ctx != null && ctx.CommandName == "checkstrikes")
                {
                    await Util.ThrowError(ctx, ex);
                }
                else
                {
                    await Util.ThrowInteractionlessError(ex);
                }
            }
        }

        public static async Task CheckStrikes(InteractionContext ctx)
        {
            string json;

            string serviceAccountEmail = "brawlbox@custom-track-testing-bot.iam.gserviceaccount.com";
            X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);
            ServiceAccountCredential credential = new(
               new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));
            SheetsService service = new(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mario Kart Brawlbot",
            });

            var temp = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Track Evaluation Log'");
            temp.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
            var tempResponse = await temp.ExecuteAsync();
            var today = int.Parse(tempResponse.Values[tempResponse.Values.Count - 1][tempResponse.Values[tempResponse.Values.Count - 1].Count - 1].ToString());

            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Track Evaluating'");
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
            ValueRange response = await request.ExecuteAsync();

            var tRequest = service.Spreadsheets.Values.Get("1I9yFsomTcvFT4hp6eN2azsfv6MsIy1897tBFX_gmtss", "'Thread Homework'");
            tRequest.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
            var tResponse = await tRequest.ExecuteAsync();
            foreach (var t in response.Values)
            {
                while (t.Count < response.Values[0].Count)
                {
                    t.Add("");
                }
            }
            foreach (var t in tResponse.Values)
            {
                while (t.Count < tResponse.Values[0].Count)
                {
                    t.Add("");
                }
            }

            using MKBBContext dbCtx = new();
            List<CouncilMemberData> councilJson = dbCtx.Council.ToList();
            var hwCompleted = new bool?[councilJson.Count];
            var threadHwCompleted = new bool?[councilJson.Count];

            List<string> tracks = new();
            List<string> threadTracks = new();
            List<string> dueTracks = new();
            List<string> dueThreadTracks = new();

            List<string> inconsistentMembers = new();
            List<string> inconsistentMembersThreads = new();
            for (int i = 1; i < response.Values.Count; i++)
            {
                if (response.Values[i][1].ToString() != "")
                {
                    if (today == int.Parse(response.Values[i][1].ToString()))
                    {
                        tracks.Add(response.Values[i][0].ToString());
                    }
                    int lastChecked = Convert.ToInt32(DateTime.Parse(File.ReadAllText("lastUpdated.txt")).Subtract(DateTime.ParseExact("31/12/1899", "dd/MM/yyyy", CultureInfo.CurrentCulture)).TotalDays);
                    if (lastChecked < today && today == int.Parse(response.Values[i][1].ToString()) + 1)
                    {
                        dueTracks.Add(response.Values[i][0].ToString());
                        for (int j = 12; j < response.Values[0].Count; j++)
                        {
                            int ix = councilJson.FindIndex(x => x.Name == response.Values[0][j].ToString());
                            bool isAuthor = councilJson[ix].Name.Contains(response.Values[i][2].ToString());
                            if (response.Values[i][j].ToString() == "" ||
                                    response.Values[i][j].ToString().ToLowerInvariant() == "yes" ||
                                    response.Values[i][j].ToString().ToLowerInvariant() == "no" ||
                                    response.Values[i][j].ToString().ToLowerInvariant() == "neutral" ||
                                    response.Values[i][j].ToString().ToLowerInvariant() == "fixes" ||
                                    !response.Values[i][j].ToString().ToLowerInvariant().Contains("yes") &&
                                    !response.Values[i][j].ToString().ToLowerInvariant().Contains("no") &&
                                    !response.Values[i][j].ToString().ToLowerInvariant().Contains("neutral") &&
                                    !response.Values[i][j].ToString().ToLowerInvariant().Contains("fixes") &&
                                    isAuthor == false)
                            {
                                inconsistentMembers.Add(councilJson[ix].DiscordID);
                            }
                        }
                    }
                }
            }
            for (int i = 1; i < tResponse.Values.Count; i++)
            {
                if (tResponse.Values[i][1].ToString() != "")
                {
                    if (today == int.Parse(tResponse.Values[i][1].ToString()))
                    {
                        threadTracks.Add(tResponse.Values[i][3].ToString().Split('"')[1].Split('/')[5]);
                    }
                    int lastChecked = Convert.ToInt32(DateTime.Parse(File.ReadAllText("lastUpdated.txt")).Subtract(DateTime.ParseExact("31/12/1899", "dd/MM/yyyy", CultureInfo.CurrentCulture)).TotalDays);
                    if (lastChecked < today && today == int.Parse(tResponse.Values[i][1].ToString()) + 1)
                    {
                        dueThreadTracks.Add(tResponse.Values[i][0].ToString());
                        for (int j = 7; j < tResponse.Values[0].Count; j++)
                        {
                            int ix = councilJson.FindIndex(x => x.Name == tResponse.Values[0][j].ToString());
                            bool isAuthor = councilJson[ix].Name.Contains(response.Values[i][2].ToString());
                            if (tResponse.Values[i][j].ToString() == "" &&
                                    isAuthor == false)
                            {
                                inconsistentMembersThreads.Add(councilJson[ix].DiscordID);
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < councilJson.Count; i++)
            {
                if (inconsistentMembers.Contains(councilJson[i].DiscordID))
                {
                    hwCompleted[i] = false;
                }
                else if (dueTracks.Count > 0)
                {
                    hwCompleted[i] = true;
                }
                if (inconsistentMembersThreads.Contains(councilJson[i].DiscordID))
                {
                    threadHwCompleted[i] = false;
                }
                else if (dueThreadTracks.Count > 0)
                {
                    threadHwCompleted[i] = true;
                }
            }
            for (int i = 0; i < councilJson.Count; i++)
            {
                bool send = false;
                if (hwCompleted[i] == true)
                {
                    councilJson[i].CompletedHW++;
                }
                else if (hwCompleted[i] == false)
                {
                    councilJson[i].Strikes++;
                    councilJson[i].CompletedHW = 0;
                    if (councilJson[i].Strikes > 2)
                    {
                        send = true;
                    }
                }
                if (threadHwCompleted[i] == true && councilJson[i].ThreadStrikes > -4)
                {
                    councilJson[i].ThreadStrikes--;
                }
                else if (threadHwCompleted[i] == false)
                {
                    councilJson[i].ThreadStrikes += 2;
                    councilJson[i].CompletedHW = 0;
                    if (councilJson[i].Strikes > 4)
                    {
                        send = true;
                    }
                }

                if (send)
                {
                    string message = $"Hello {councilJson[i].Name}. Just to let you know, you appear to have not completed council homework in a while, have been inconsistent with your homework, or are not completing it sufficiently enough. Just to remind you, if you miss homework too many times, admins might have to remove you from council. If you have an issue which stops you from doing homework, please let an admin know.";

                    var members = Bot.Client.GetGuildAsync(180306609233330176).Result.GetAllMembersAsync();
                    foreach (var member in members.Result)
                    {
                        if (member.Id.ToString() == councilJson[i].DiscordID)
                        {
                            try
                            {
                                Console.WriteLine($"DM'd Member: {councilJson[i].Name}");

#if RELEASE
    await member.SendMessageAsync(message);
#endif
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

            dbCtx.SaveChanges();

            DiscordChannel councilAnnouncements = Bot.Client.GetGuildAsync(180306609233330176).Result.GetChannel(635313521487511554);

            string listOfTracks = "";
            if (tracks.Count > 0)
            {
                listOfTracks = tracks[0];
                for (int i = 1; i < tracks.Count; i++)
                {
                    listOfTracks += $", {tracks[i]}";
                }
                if (tracks.Count == 1)
                {
                    listOfTracks += " is";
                }
                else
                {
                    listOfTracks += " are";
                }
                var ping = "";
#if RELEASE
    ping = "<@&608386209655554058> ";
#endif
                await councilAnnouncements.SendMessageAsync($"__**Submissions**__\n{ping}{listOfTracks} due for today.");
            }
            if (threadTracks.Count > 0)
            {
                listOfTracks = $"<#{threadTracks[0]}>";
                for (int i = 1; i < threadTracks.Count; i++)
                {
                    listOfTracks += $", <#{threadTracks[i]}>";
                }
                if (threadTracks.Count == 1)
                {
                    listOfTracks += " is";
                }
                else
                {
                    listOfTracks += " are";
                }
                var ping = "";
#if RELEASE
    ping = "<@&608386209655554058> ";
#endif
                await councilAnnouncements.SendMessageAsync($"__**Threads**__\n{ping}{listOfTracks} due for today.");
            }

            DiscordChannel strikeAnnouncements = Bot.Client.GetGuildAsync(180306609233330176).Result.GetChannel(1019149329556062278);
            DiscordChannel strikeLogs = Bot.Client.GetGuildAsync(1095401690120851558).Result.GetChannel(1095402229491581061);

            string description = string.Empty;
            for (int i = 0; i < hwCompleted.Length; i++)
            {
                if (hwCompleted[i] == false)
                {
                    description += $"{councilJson[i].Name}\n";
                }
            }

            if (hwCompleted.Select(x => x == false).ToArray().Length > 0 && dueTracks.Count > 0 && description != string.Empty)
            {
                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Members who missed homework:**__",
                    Description = description,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await strikeAnnouncements.SendMessageAsync(embed);
                await strikeLogs.SendMessageAsync(embed);
            }

            description = string.Empty;
            for (int i = 0; i < threadHwCompleted.Length; i++)
            {
                if (threadHwCompleted[i] == false)
                {
                    description += $"{councilJson[i].Name}\n";
                }
            }

            if (threadHwCompleted.Select(x => x == false).ToArray().Length > 0 && dueThreadTracks.Count > 0 && description != string.Empty)
            {
                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Members who missed thread homework:**__",
                    Description = description,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await strikeAnnouncements.SendMessageAsync(embed);
                await strikeLogs.SendMessageAsync(embed);
            }

            var now = DateTime.Now;
            File.WriteAllText("lastUpdated.txt", now.ToString("dd/MM/yyyy HH:mm:ss"));

            DiscordActivity activity = new()
            {
                Name = $"Last Updated: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}" +
                $" | /help"
            };
            await Bot.Client.UpdateStatusAsync(activity);
        }

        [SlashCommand("refreshbuttons", "Refreshes the buttons in the CTGP server for submitted information.")]
        [SlashRequireOwner]
        public static async Task RefreshButtons(InteractionContext ctx)
        {
            if (ctx != null && ctx.CommandName == "refreshbuttons")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = true });
            }
            try
            {
                // Ghostbusters
                DiscordChannel channel = Bot.Client.GetGuildAsync(180306609233330176).Result.GetChannel(1118995806754721853);
                DiscordMessage message = channel.GetMessagesAsync().Result[0];
                await channel.DeleteMessageAsync(message);
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(message.Embeds[0]).AddComponents(message.Components));

                // Bug Reports
                channel = Bot.Client.GetGuildAsync(180306609233330176).Result.GetChannel(1122585223306162296);
                message = channel.GetMessagesAsync().Result[0];
                await channel.DeleteMessageAsync(message);
                await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(message.Embeds[0]).AddComponents(message.Components));

                if (ctx != null && ctx.CommandName == "refreshbuttons")
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Success:**__",
                        Description = $"*All buttons have been refreshed successfully.*",
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
                await Util.ThrowInteractionlessError(ex);
            }
        }
    }
}