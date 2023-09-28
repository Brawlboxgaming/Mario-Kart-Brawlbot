using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MKBB.Class;
using MKBB.Data;
using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace MKBB.Commands
{
    public partial class Misc : ApplicationCommandModule
    {
        [GeneratedRegex("\\d\\d\\d\\d-\\d\\d-\\d\\d\\.zip")]
        private static partial Regex DateTimeZipRegex();

        [SlashCommand("addtool", "Adds a tool to the list of tools.")]
        public static async Task AddTool(InteractionContext ctx,
            [Option("name", "The name of the tool you would like to add.")] string toolName,
            [Option("creators", "The name(s) of the creators of the tool.")] string toolCreators,
            [Option("description", "The description of the tool i.e. what it does.")] string toolDescription,
            [Option("download", "The link to the download for the tool.")] string toolDownload)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();
                List<ToolData> toolList = dbCtx.Tools.ToList();

                toolList.Add(new ToolData
                {
                    Name = toolName,
                    Creators = toolCreators,
                    Description = toolDescription,
                    Download = toolDownload
                });
                toolList = toolList.OrderBy(x => x.Name).ToList();
                string tools = JsonConvert.SerializeObject(toolList);
                await dbCtx.SaveChangesAsync();

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "__**Tool was added:**__",
                    Description = $"**Name:**\n" +
                            $"{toolName}\n" +
                            $"**Creators:**\n" +
                            $"{toolCreators}\n" +
                            $"**Description:**\n" +
                            $"{toolDescription}\n" +
                            $"**Download:**\n" +
                            $"{toolDownload}",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("edittool", "Adds a tool to the list of tools.")]
        public static async Task EditTool(InteractionContext ctx,
            [Option("old-name", "The name of the tool you would like to edit.")] string oldToolName,
            [Option("name", "The new name for the tool you are editing.")] string toolName = "",
            [Option("creators", "The new name(s) of the creators of the tool.")] string toolCreators = "",
            [Option("description", "The new description of the tool i.e. what it does.")] string toolDescription = "",
            [Option("download", "The new link to the download for the tool.")] string toolDownload = "")
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();
                List<ToolData> toolList = dbCtx.Tools.ToList();

                int index = Util.ListNameCheck(toolList, oldToolName);

                DiscordEmbedBuilder embed = new();
                if (index > -1)
                {
                    if (toolName != "")
                    {
                        toolList[index].Name = toolName;
                    }
                    if (toolCreators != "")
                    {
                        toolList[index].Creators = toolCreators;
                    }
                    if (toolDescription != "")
                    {
                        toolList[index].Description = toolDescription;
                    }
                    if (toolDownload != "")
                    {
                        toolList[index].Download = toolDownload;
                    }

                    await dbCtx.SaveChangesAsync();

                    embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = $"__**{oldToolName} has been edited:**__",
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
                        Description = $"*{oldToolName} could not be found. If you think a tool is missing, contact <@105742694730457088>.*",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                        }
                    };
                }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("deltool", "Removes a tool from the list of tools.")]
        public static async Task DeleteTool(InteractionContext ctx,
            [Option("name", "The name of the tool you would like to delete.")] string toolName)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                using MKBBContext dbCtx = new();
                List<ToolData> toolList = dbCtx.Tools.ToList();

                int index = Util.ListNameCheck(toolList, toolName);

                string displayName = toolList[index].Name;
                toolList.RemoveAt(index);

                await dbCtx.SaveChangesAsync();

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "__**Success:**__",
                    Description = $"*{displayName} has been removed.*",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Last Updated: {File.ReadAllText("lastUpdated.txt")}"
                    }
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                await Util.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("uploadtestpack", "Uploads the test pack .zip file (named with the date of the test YYYY-MM-DD)")]
        public static async Task UploadTestPack(InteractionContext ctx,
            [Option("test-pack-zip", "The .zip file for the test pack (named with the date of the test YYYY-MM-DD)")] DiscordAttachment file)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder() { IsEphemeral = ctx.Guild.Id != 1095401690120851558 });

                if (DateTimeZipRegex().Match(file.FileName).Success)
                {
                    WebClient webClient = new();
                    await webClient.DownloadFileTaskAsync(file.Url, $"C:/Files/Tests/{file.FileName}");
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "__**Success:**__",
                        Description = $"*File is now available here: https://files.brawlbox.co.uk/Tests/{file.FileName}.*",
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
                        Title = "__**Error:**__",
                        Description = $"*File was not named in the correct naming convention: YYYY-MM-DD.*",
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
    }
}
