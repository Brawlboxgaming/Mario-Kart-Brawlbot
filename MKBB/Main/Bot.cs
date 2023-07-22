using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using MKBB.Class;
using MKBB.Commands;
using MKBB.Data;
using Newtonsoft.Json;
using System.Text;

namespace MKBB
{
    public class Bot
    {
        public Interactions interactions = new();
        public Events events = new();
        public static DiscordClient Client { get; private set; }
        public static SlashCommandsExtension SlashCommands { get; private set; }

        public async Task RunAsync()
        {
            string json = string.Empty;

            using (FileStream fs = File.OpenRead("config.json"))
            using (StreamReader sr = new(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            DiscordConfiguration config = new()
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };

            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                AckPaginationButtons = true,
                Timeout = TimeSpan.FromSeconds(60)
            });

            CommandsNextConfiguration commandsConfig = new()
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableDms = false,
                EnableDefaultHelp = false,
                DmHelp = true
            };

            SlashCommands = Client.UseSlashCommands();
#if DEBUG
            SlashCommands.RegisterCommands<Testing>(180306609233330176);
#endif
            SlashCommands.RegisterCommands<Config>();
            SlashCommands.RegisterCommands<TimeTrialManagement>();
            SlashCommands.RegisterCommands<TextCommands>();
            SlashCommands.RegisterCommands<Info>();

            SlashCommands.RegisterCommands<Update>(180306609233330176);
            SlashCommands.RegisterCommands<Council>(180306609233330176);
            SlashCommands.RegisterCommands<Misc>(180306609233330176);
            SlashCommands.RegisterCommands<Ghostbusters>(180306609233330176);

            await events.AssignAllEvents();

            await interactions.AssignAllInteractions();

            DiscordActivity activity = new()
            {
                Name = $"Bot is currently under maintenance. Please be patient :)"
            };

            await Client.ConnectAsync(activity);

            Update update = new();

            await Update.StartTimers(null);

            using MKBBContext dbCtx = new();

            //await dbCtx.Database.EnsureCreatedAsync();

            //await dbCtx.SaveChangesAsync();

            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
