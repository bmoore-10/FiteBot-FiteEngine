using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FiteBot.Commands.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace FiteBot.Commands
{
    public class CommandHandler
    {
        // Discord client
        private DiscordSocketClient client;

        // Discord service stuff
        public CommandService commandService { get; private set; }
        public IServiceProvider serviceProvider { get; private set; }

        // Command prefix for bot
        public static char commandPrefix = '!';

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandHandler(DiscordSocketClient clientIn, CommandService commandServiceIn, IServiceProvider serviceProviderIn)
        {
            // Get client reference
            client = clientIn;

            // Initialize the command service
            commandService = commandServiceIn;
            serviceProvider = serviceProviderIn;
            commandService.AddModulesAsync(Assembly.GetEntryAssembly(), services: serviceProvider);
            client.MessageReceived += HandleCommandsASync;
        }
        
        /// <summary>
        /// Handls commands asynchronynously 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private async Task HandleCommandsASync(SocketMessage s)
        {
            var message = s as SocketUserMessage;
            if(message == null) return;

            // Create a numbner to track where specific prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if(!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a Websocket-based command contxt based on the message
            var context = new SocketCommandContext(client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.

            // Keep in mind that result does not indicate a return value
            // rather an object stating if the command executed successfully.
            var result = await commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: serviceProvider);
        }
    }
}
