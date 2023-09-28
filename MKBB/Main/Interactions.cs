using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MKBB.Class;
using MKBB.Data;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace MKBB
{
    public class Interactions
    {
        public async Task AssignAllInteractions()
        {
            Bot.Client.InteractionCreated += LogInteractions;

            Bot.Client.ComponentInteractionCreated += async (c, e) =>
            {
                foreach (PendingPagesInteraction p in Util.PendingPageInteractions)
                {
                    if (e.Id == "leftButton") await PressLeftButton(e, p);
                    if (e.Id == "rightButton") await PressRightButton(e, p);
                    if (e.Id == "category") await OnCategoryChange(e, p);
                }

                foreach (PendingChannelConfigInteraction p in Util.PendingChannelConfigInteractions)
                {
                    await OnBotChannelChange(e, p);
                };

                if (e.Id == "gbSubmission") await GBModal(e);

                if (e.Id == "issuesEmbedding") await DisplayEmbeddedIssues(e);

                if (e.Id == "bugSubmission") await BugModal(e);
                if (e.Id.Contains("bugAcceptMaj")) await BugAdminModal(c, e, "Accept", 5, e.Id.Split('-')[1], e.Id.Split('-')[2]);
                if (e.Id.Contains("bugAcceptMin")) await BugAdminModal(c, e, "Accept", 6, e.Id.Split('-')[1], e.Id.Split('-')[2]);
                if (e.Id.Contains("bugModifyMaj")) await BugAdminModal(c, e, "Modify", 5, e.Id.Split('-')[1], e.Id.Split('-')[2]);
                if (e.Id.Contains("bugModifyMin")) await BugAdminModal(c, e, "Modify", 6, e.Id.Split('-')[1], e.Id.Split('-')[2]);
                if (e.Id.Contains("bugReject")) await BugAdminModal(c, e, "Reject", -1, e.Id.Split('-')[1], e.Id.Split('-')[2]);

                if (e.Id == "pinAccept") await HandlePendingPins(c, e, true);
                if (e.Id == "pinReject") await HandlePendingPins(c, e, false);
            };

            Bot.Client.ModalSubmitted += async (c, e) =>
            {
                if (e.Interaction.Data.CustomId == "gbSubmissionModal") await GBModalSubmit(e);

                if (e.Interaction.Data.CustomId == "bugSubmissionModal") await BugModalSubmit(c, e);
                if (e.Interaction.Data.CustomId.Contains("adminBugSubmissionModalMaj")) await BugAdminModalSubmit(c, e, 5, e.Interaction.Data.CustomId.Split('-')[1], e.Interaction.Data.CustomId.Split('-')[2]);
                if (e.Interaction.Data.CustomId.Contains("adminBugSubmissionModalMin")) await BugAdminModalSubmit(c, e, 6, e.Interaction.Data.CustomId.Split('-')[1], e.Interaction.Data.CustomId.Split('-')[2]);
            };

            await Task.CompletedTask;
        }

        private async Task DisplayEmbeddedIssues(ComponentInteractionCreateEventArgs e)
        {
            try
            {
                await e.Interaction.DeferAsync(true);
                string newMessage = "";
                DiscordMessage message = e.Message;
                List<string> issues = message.Embeds[0].Description.Split('\n').ToList();
                int issueCount = 0;
                foreach (var issue in issues)
                {
                    if (issue.Contains("https://"))
                    {
                        issueCount++;
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        {
                            IsEphemeral = true,
                            Content = issue.Replace("*", "")
                        });
                    }
                }

                if (issueCount == 0)
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = "*There are no issues to embed.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }
        private async Task LogInteractions(DiscordClient c, InteractionCreateEventArgs e)
        {
            DiscordChannel channel = Bot.Client.GetGuildAsync(1095401690120851558).Result.GetChannel(1095402077338996846);

            string options = "";

            if (e.Interaction.Data.Options != null)
            {
                foreach (DiscordInteractionDataOption option in e.Interaction.Data.Options)
                {
                    options += $" {option.Name}: *{option.Value}*";
                }
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = $"__**Notice:**__",
                Description = $"'/{e.Interaction.Data.Name}{options}' was used by <@{e.Interaction.User.Id}> in {e.Interaction.Guild.Name}.",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Server Time: {DateTime.Now}"
                }
            };
            await channel.SendMessageAsync(embed);
        }
        private async Task PressLeftButton(ComponentInteractionCreateEventArgs e, PendingPagesInteraction p)
        {
            if (e.Message.Id == p.MessageId)
            {
                p.CurrentPage = p.CurrentPage - 1 == -1 ? p.Pages.Count - 1 : p.CurrentPage - 1;
                DiscordInteractionResponseBuilder responseBuilder = new();
                if (p.CategoryNames != null)
                {
                    if (p.CategoryNames.Count != 1)
                    {
                        responseBuilder.AddComponents(Util.GenerateCategorySelectMenu(p.CategoryNames, p.CurrentCategory));
                    }
                }
                responseBuilder.AddEmbed(p.Pages[p.CurrentPage]).AddComponents(Util.GeneratePageArrows());
                foreach (DiscordActionRowComponent componentHolder in e.Message.Components)
                {
                    foreach (DiscordComponent component in componentHolder.Components)
                    {
                        if (component.CustomId == "issuesEmbedding")
                        {
                            responseBuilder.AddComponents(component);
                            break;
                        }
                    }
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
            }
        }
        private async Task PressRightButton(ComponentInteractionCreateEventArgs e, PendingPagesInteraction p)
        {
            if (e.Message.Id == p.MessageId)
            {
                p.CurrentPage = (p.CurrentPage + 1) % p.Pages.Count;
                DiscordInteractionResponseBuilder responseBuilder = new();
                if (p.CategoryNames != null)
                {
                    if (p.CategoryNames.Count != 1)
                    {
                        responseBuilder.AddComponents(Util.GenerateCategorySelectMenu(p.CategoryNames, p.CurrentCategory));
                    }
                }
                responseBuilder.AddEmbed(p.Pages[p.CurrentPage]).AddComponents(Util.GeneratePageArrows());
                foreach (DiscordActionRowComponent componentHolder in e.Message.Components)
                {
                    foreach (DiscordComponent component in componentHolder.Components)
                    {
                        if (component.CustomId == "issuesEmbedding")
                        {
                            responseBuilder.AddComponents(component);
                            break;
                        }
                    }
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
            }
        }
        private async Task OnCategoryChange(ComponentInteractionCreateEventArgs e, PendingPagesInteraction p)
        {
            if (e.Message.Id == p.MessageId)
            {
                p.CurrentPage = 0;
                p.CurrentCategory = p.CategoryNames.FindIndex(x => x == e.Values[0]);
                p.Pages = p.Categories[p.CurrentCategory];

                DiscordInteractionResponseBuilder responseBuilder = new();
                responseBuilder.AddComponents(Util.GenerateCategorySelectMenu(p.CategoryNames, p.CurrentCategory));
                responseBuilder.AddEmbed(p.Pages[p.CurrentPage]);
                if (p.Pages.Count() > 1)
                {
                    responseBuilder.AddComponents(Util.GeneratePageArrows());
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
            }
        }
        private async Task OnBotChannelChange(ComponentInteractionCreateEventArgs e, PendingChannelConfigInteraction p)
        {

            if (e.Message.Id == p.MessageId)
            {
                using MKBBContext dbCtx = new();
                List<ServerData> servers = dbCtx.Servers.ToList();
                string ids = "";
                foreach (string value in e.Values)
                {
                    ids += $"{value},,";
                }
                ids = ids.Remove(ids.Length - 2, 2);
                foreach (ServerData server in servers)
                {
                    if (server.ServerID == e.Guild.Id)
                    {
                        server.BotChannelIDs = ids;
                        break;
                    }
                }
                await dbCtx.SaveChangesAsync();
                string channelsDisplay = "";
                foreach (string id in ids.Split(',').ToList())
                {
                    channelsDisplay += $"<#{id}>\n";
                }

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Success:**__",
                    Description = $"*The server's bot channels have been set to the following:*\n" +
                    channelsDisplay,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed));
            }
        }
        private async Task HandlePendingPins(DiscordClient c, ComponentInteractionCreateEventArgs e, bool a)
        {
            string description = e.Message.Embeds[0].Description;
            string jumpLink = description.Split('\n')[1];

            var message = await c
                .GetGuildAsync(ulong.Parse(jumpLink.Split('/')[4])).Result
                .GetChannel(1046936322574655578)
                .Threads.First(x => x.Id == ulong.Parse(jumpLink.Split('/')[5]))
                .GetMessageAsync(ulong.Parse(jumpLink.Split('/')[6]));

            if (a)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Pin Accepted:**__",
                        Description = description,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Server Time: {DateTime.Now}"
                        }
                    })
                );
                await message.PinAsync();
            }
            else
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Pin Rejected:**__",
                        Description = description,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Server Time: {DateTime.Now}"
                        }
                    })
                );
            }
        }
        private async Task GBModal(ComponentInteractionCreateEventArgs e)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, new DiscordInteractionResponseBuilder()
                .WithTitle("Ghostbusters Submission")
                .WithCustomId("gbSubmissionModal")
                .AddComponents(new TextInputComponent(label: "Chadsoft Link", customId: "link",
                placeholder: "https://chadsoft.co.uk/time-trials/rkgd/60/BD/59C361AB44B3542B99349F815D3AA5090758.html"))
                .AddComponents(new TextInputComponent(label: "Comments", customId: "comments", placeholder: "", required: false, style: TextInputStyle.Paragraph))
            );
        }
        private async Task GBModalSubmit(ModalSubmitEventArgs e)
        {
            try
            {
                await e.Interaction.DeferAsync(true);

                string ghostUrl = e.Values.First(x => x.Key == "link").Value;
                DiscordUser user = e.Interaction.User;
                string comments = e.Values.First(x => x.Key == "comments").Value;

                if (ghostUrl.Contains("rkg"))
                {
                    WebClient webClient = new();
                    Ghost ghostData = new();
                    try
                    {
                        ghostData = JsonConvert.DeserializeObject<Ghost>(await webClient.DownloadStringTaskAsync(ghostUrl[..^4] + "json"));
                    }
                    catch
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = "*Invalid ghost link.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }

                    bool found = false;
                    using MKBBContext dbCtx = new();

                    if (dbCtx.GBTimes.Where(x => x.TrackSHA1 == ghostData.TrackID).ToList().Count() > 0)
                    {
                        DiscordEmbedBuilder embed = new()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = "*This time has already been submitted.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        };
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }

                    foreach (GBTrackData track in dbCtx.GBTracks)
                    {
                        if (track.SHA1s.Contains(ghostData.TrackID.ToUpperInvariant()))
                        {
                            found = true;
                        }
                    }

                    if (found)
                    {
                        dbCtx.GBTimes.Add(new()
                        {
                            TrackSHA1 = ghostData.TrackID,
                            Player = user.Id.ToString(),
                            URL = ghostUrl,
                            Comments = comments != null ? comments : null
                        });
                        await dbCtx.SaveChangesAsync();

                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Success:**__",
                            Description = $"*The time was added successfully.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }));

                        DiscordChannel channel = e.Interaction.Guild.GetChannel(1128804056761118881);

                        await channel.SendMessageAsync(new DiscordEmbedBuilder()
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**New Time Submitted:**__",
                            Description = $"**Track**: *{dbCtx.GBTracks.First(x=>x.SHA1s.Contains(ghostData.TrackID)).Name}*" +
                            $"\n**Player**: *{user.Mention}*" +
                            $"\n**URL**: *{ghostUrl}*" +
                            $"{(comments != null && comments != "" ? $"\n**Comments**: *{comments}*" : null)}",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        });
                    }
                    else
                    {
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = $"__**Error:**__",
                            Description = $"*The track SHA1 of the ghost link was not found on any new tracks.*",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                            }
                        }));
                    }
                }
                else
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*This is not a valid ghost link.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }
        private async Task BugModal(ComponentInteractionCreateEventArgs e)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, new DiscordInteractionResponseBuilder()
                .WithTitle("Bug Report Submission")
                .WithCustomId("bugSubmissionModal")
                .AddComponents(new TextInputComponent(label: "Track", customId: "track", placeholder: "Melting Magma Melee"))
                .AddComponents(new TextInputComponent(label: "Video or Image Link", customId: "link", placeholder: "https://gyazo.com/c8f785e6997bef0231723b582ea25f90"))
                .AddComponents(new TextInputComponent(label: "Description of Bug", customId: "description", placeholder: "Bad out-of-bounds plane."))
                .AddComponents(new TextInputComponent(label: "Comments", customId: "comments", required: false, style: TextInputStyle.Paragraph))
            );
        }
        private async Task BugModalSubmit(DiscordClient c, ModalSubmitEventArgs e)
        {
            try
            {
                await e.Interaction.DeferAsync(true);

                using MKBBContext dbCtx = new();

                string submittedTrack = Util.Convert3DSTrackName(e.Values.First(x => x.Key == "track").Value);
                int ix = Util.ListNameCheck(dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList(), submittedTrack);
                if (ix < 0)
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*{submittedTrack} was not found.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));
                }
                else
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Success:**__",
                        Description = $"*Your bug has been reported and is now being reviewed by admins.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));

                    DiscordChannel channel = c.GetGuildAsync(180306609233330176).Result.GetChannel(1122584278895693934); // #bug-report-log

                    string comments = e.Values.First(x => x.Key == "comments").Value;
                    if (comments != "")
                    {
                        comments = $"\n**Comments:** *{comments}*";
                    }

                    var message = await channel.SendMessageAsync("If you can still see this, this means something has gone wrong...");

                    DiscordButtonComponent acceptMajButton = new(
                        ButtonStyle.Success,
                        $"bugAcceptMaj-{message.Id}-{e.Interaction.User.Mention}",
                        "Accept (Major)");
                    DiscordButtonComponent acceptMinButton = new(
                        ButtonStyle.Success,
                        $"bugAcceptMin-{message.Id}-{e.Interaction.User.Mention}",
                        "Accept (Minor)");
                    DiscordButtonComponent modifyMajButton = new(
                        ButtonStyle.Secondary,
                        $"bugModifyMaj-{message.Id}-{e.Interaction.User.Mention}",
                        "Modify (Major)");
                    DiscordButtonComponent modifyMinButton = new(
                        ButtonStyle.Secondary,
                        $"bugModifyMin-{message.Id}-{e.Interaction.User.Mention}",
                        "Modify (Minor)");
                    DiscordButtonComponent rejectButton = new(
                        ButtonStyle.Danger,
                        $"bugReject-{message.Id}-{e.Interaction.User.Mention}",
                        "Reject");

                    await message.ModifyAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Pending Bug Report:**__",
                        Description = $"**Submitted by:** {e.Interaction.User.Mention}\n" +
                        $"**Track:** *{dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList()[ix].Name}*\n" +
                        $"**Evidence:** *{e.Values.First(x => x.Key == "link").Value}*\n" +
                        $"**Description:** *{e.Values.First(x => x.Key == "description").Value}*" +
                        comments,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }).AddComponents(acceptMajButton, acceptMinButton, modifyMajButton, modifyMinButton, rejectButton));

                    await channel.SendMessageAsync(e.Values.First(x => x.Key == "link").Value);
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }
        private async Task BugAdminModal(DiscordClient c, ComponentInteractionCreateEventArgs e, string a, int s, string m, string u)
        {
            DiscordMessage message = e.Message;
            if (a == "Accept")
            {
                await e.Interaction.DeferAsync(true);
                using MKBBContext dbCtx = new();

                TrackData track = new();
                foreach (TrackData t in dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList())
                {
                    if (message.Embeds[0].Description.Contains(t.Name))
                    {
                        track = t;
                    }
                }

                Match match = Regex.Match(message.Embeds[0].Description, @"\*\*Evidence:\*\* \*(.*)\*");
                string evidence = match.Groups[1].Value;
                match = Regex.Match(message.Embeds[0].Description, @"\*\*Description:\*\* \*(.*)\*");
                string description = match.Groups[1].Value;

                string issue = $"- {description}: {evidence}";

                await AddIssueToSheet(s, issue, track);

                DiscordMessage ogMessage = await e.Interaction.Channel.GetMessageAsync(ulong.Parse(m));
                await ogMessage.ModifyAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Accepted Bug Report:**__",
                    Description = $"**Submitted by:** {u}\n" +
                        $"**Track:** *{track.Name}*\n" +
                        $"**Evidence:** *{evidence}*\n" +
                        $"**Description:** *{description}*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));

                DiscordChannel channel = c.GetGuildAsync(180306609233330176).Result.GetChannel(1122584615471829084); //ctgp-bug-log

                await channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Bug Report:**__",
                    Description = $"**Submitted by:** {u}\n" +
                        $"**Track:** *{track.Name}*\n" +
                        $"**Evidence:** *{evidence}*\n" +
                        $"**Description:** *{description}*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                });

                await channel.SendMessageAsync(evidence);

                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Success:**__",
                    Description = $"*Bug reported successfully.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));

                return;
            }
            else if (a == "Reject")
            {
                await e.Interaction.DeferAsync(true);
                using MKBBContext dbCtx = new();

                TrackData track = new();
                foreach (TrackData t in dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList())
                {
                    if (message.Embeds[0].Description.Contains(Util.Convert3DSTrackName(t.Name)))
                    {
                        track = t;
                    }
                }

                Match match = Regex.Match(message.Embeds[0].Description, @"\*\*Evidence:\*\* \*(.*)\*");
                string evidence = match.Groups[1].Value;
                match = Regex.Match(message.Embeds[0].Description, @"\*\*Description:\*\* \*(.*)\*");
                string description = match.Groups[1].Value;
                match = Regex.Match(message.Embeds[0].Description, @"\*\*Submitted by:\*\* (.*)");
                string mention = match.Groups[1].Value;

                string issue = $"- {description}: {evidence}";

                DiscordMessage ogMessage = await e.Interaction.Channel.GetMessageAsync(ulong.Parse(m));
                await ogMessage.ModifyAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Rejected Bug Report:**__",
                    Description = $"**Submitted by:** {u}\n" +
                        $"**Track:** *{track.Name}*\n" +
                        $"**Evidence:** *{evidence}*\n" +
                        $"**Description:** *{description}*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));

                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = $"__**Success:**__",
                    Description = $"*Bug report has been rejected successfully.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                }));

                return;
            }
            await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, new DiscordInteractionResponseBuilder()
                .WithTitle("Bug Report Modified Submission")
                .WithCustomId(s == 5 ? $"adminBugSubmissionModalMaj-{message.Id}-{u}" : $"adminBugSubmissionModalMin-{message.Id}-{u}")
                .AddComponents(new TextInputComponent(label: "Track", customId: "track", placeholder: "Melting Magma Melee"))
                .AddComponents(new TextInputComponent(label: "Video or Image Link", customId: "link", placeholder: "https://gyazo.com/c8f785e6997bef0231723b582ea25f90"))
                .AddComponents(new TextInputComponent(label: "Description of Bug", customId: "description", placeholder: "Bad out-of-bounds plane."))
                );
        }
        private async Task BugAdminModalSubmit(DiscordClient c, ModalSubmitEventArgs e, int s, string m, string u)
        {
            try
            {
                await e.Interaction.DeferAsync(true);

                using MKBBContext dbCtx = new();

                string submittedTrack = Util.Convert3DSTrackName(e.Values.First(x => x.Key == "track").Value);
                int ix = Util.ListNameCheck(dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList(), submittedTrack);
                if (ix < 0)
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Error:**__",
                        Description = $"*{submittedTrack} was not found.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));
                }
                else
                {
                    TrackData track = dbCtx.Tracks.Where(x => !x.Is200cc && x.CustomTrack && (x.CategoryName == "Normal" || x.CategoryName == "No-shortcut")).ToList()[ix];

                    string evidence = e.Values.First(x => x.Key == "link").Value;
                    string description = e.Values.First(x => x.Key == "description").Value;

                    string issue = $"- {description}: {evidence}";

                    await AddIssueToSheet(s, issue, track);

                    DiscordMessage ogMessage = await e.Interaction.Channel.GetMessageAsync(ulong.Parse(m));
                    await ogMessage.ModifyAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Accepted Bug Report:**__",
                        Description = $"**Submitted by:** {u}\n" +
                            $"**Track:** *{track.Name}*\n" +
                            $"**Evidence:** *{evidence}*\n" +
                            $"**Description:** *{description}*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));

                    DiscordChannel channel = c.GetGuildAsync(180306609233330176).Result.GetChannel(1122584615471829084); //ctgp-bug-log

                    await channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Bug Report:**__",
                        Description = $"**Submitted by:** {u}\n" +
                            $"**Track:** *{track.Name}*\n" +
                            $"**Evidence:** *{evidence}*\n" +
                            $"**Description:** *{description}*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    });

                    await channel.SendMessageAsync(evidence);

                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**Success:**__",
                        Description = $"*Bug reported successfully.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    }));

                    return;
                }
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }
        private async Task AddIssueToSheet(int s, string i, TrackData t)
        {
            try
            {
                string serviceAccountEmail = "sheetbox@sonic-fiber-399810.iam.gserviceaccount.com";

                X509Certificate2 certificate = new(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail).FromCertificate(certificate));

                SheetsService service = new(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Mario Kart Brawlbot",
                });

                var request = service.Spreadsheets.Values.Get("1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'CTGP Track Issues'!A1:G219");
                var response = await request.ExecuteAsync();
                foreach (var r in response.Values)
                {
                    while (r.Count < 7)
                    {
                        r.Add("");
                    }
                }

                int ix = Util.ListNameCheck(response.Values, t.Name, ix2: 0);

                response.Values[ix][s] = response.Values[ix][s].ToString().Trim();

                if (response.Values[ix][s].ToString() != "")
                {
                    response.Values[ix][s] = $"{response.Values[ix][s]}\n{i}";
                }
                else
                {
                    response.Values[ix][s] = i;
                }

                var updateRequest = service.Spreadsheets.Values.Update(response, "1xwhKoyypCWq5tCRTI69ijJoDiaoAVsvYAxz-q4UBNqM", "'CTGP Track Issues'!A1:G219");
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                var update = await updateRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                await Util.ThrowInteractionlessError(ex);
            }
        }
    }
}
