using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FiteBot.Commands;
using FiteEngine;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FiteBot
{
    public class FiteBot
    {
        // Config file stuff
        public static string dataFolderName = "/Data";
        public static string configFileName = "/config";
        public static string configFileExtension = ".txt";

        // Variables for running
        public string clientKey { get; private set; } // Bot's client key
        public IServiceProvider services { get; private set; }
        public CommandService commandService { get; private set; }
        private FiteEngine.FiteEngine fiteEngine;

        // Discord stuff
        private DiscordSocketClient client;
        private CommandHandler commandHandler;


        /// <summary>
        /// Launches the asynchronous launcher for the discord bot
        /// </summary>
        public static void Main(string[] args)
            => new FiteBot().StartAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Creates a new FiteBot and populates the variables needed for running from the config
        /// </summary>
        public FiteBot()
        {
            this.LoadConfig();
            this.fiteEngine = new FiteEngine.FiteEngine();
            this.commandService = new CommandService();
            this.services = ConfigureServices();
        }

        /// <summary>
        /// Asynchronous launcher for the discord bot
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            // Create our client, log in, and start up the bot
            client = new DiscordSocketClient();
            commandHandler = new CommandHandler(client, commandService, services);
            client.Log += Log;
            await client.LoginAsync(TokenType.Bot, clientKey);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection()
                .AddSingleton(this.fiteEngine)
                .AddSingleton(this.commandService);

            return map.BuildServiceProvider();
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        #region Config stuff
        /// <summary>
        /// Loads (or creates) the config and attempts to populate the variables needed for running
        /// </summary>
        private void LoadConfig()
        {
            //Create the data folder if it doesn't exist
            if(!Directory.Exists(GetDataFolderPath()))
            {
                Directory.CreateDirectory(GetDataFolderPath());
            }

            // Create the data file if it doesn't exist
            if(!File.Exists(GetConfigFilePath()))
            {
                CreateDefaultConfigFile();
            }

            // Read the config
            string[] configLines = File.ReadAllLines(GetConfigFilePath());

            // Populate the bot's client ID
            if(configLines[0] != null) {
                string[] clientKeyLineSplit = configLines[0].Split(':');
                if(!clientKeyLineSplit[0].Equals("ClientKey") ||
                    clientKeyLineSplit.Length != 2 ||
                    clientKeyLineSplit[1].Equals(""))
                {
                    Console.WriteLine("Client key line incorrectly formatted. Correct format: ClientKey:KEY");
                    throw new System.ArgumentException("IncorrectConfigFormat");
                }
                else
                {
                    clientKey = clientKeyLineSplit[1].Trim();
                }
            }
            else
            {
                Console.WriteLine("Empty config file! Please delete it and allow it to be automatically regenerated.");
                throw new System.ArgumentException("IncorrectConfigFormat");
            }

        }

        /// <summary>
        /// Creates the default config for the fitebot
        /// </summary>
        private void CreateDefaultConfigFile()
        {
            File.WriteAllLines(GetConfigFilePath(), defaultConfig);
        }

        /// <summary>
        /// Constructs the path to the config file
        /// </summary>
        /// <returns></returns>
        private string GetConfigFilePath()
        {
            return GetDataFolderPath() + configFileName + configFileExtension;
        }

        /// <summary>
        /// Constructs the path to the data folder
        /// </summary>
        /// <returns></returns>
        private string GetDataFolderPath()
        {
            return Directory.GetCurrentDirectory() + dataFolderName;
        }

        /// <summary>
        /// Default config file setup
        /// Need the bot's client key
        /// </summary>
        private string[] defaultConfig =
        {
            "ClientKey:"
        };
        #endregion
    }
}
