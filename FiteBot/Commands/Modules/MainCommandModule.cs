using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using FiteEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FiteBot.Commands.Modules
{
    public class MainCommandModule : ModuleBase<SocketCommandContext>
    {
        private CommandService commandService;
        private FiteEngine.FiteEngine fiteEngine;
        private static Color memberRoleColor = Color.Magenta;

        [Command("Commands")]
        [Summary("Gets and lists the information for each command. Moderators may call '!commands 2' to get a list of moderator commands")]
        public async Task GetCommands(int page = 1)
        {
            List<CommandInfo> commands = commandService.Commands.ToList();

            List<Tuple<string, string>> commonCommandDescriptions = new List<Tuple<string,string>>();
            List<Tuple<string,string>> moderatorCommandDescriptions = new List<Tuple<string,string>>();

            EmbedBuilder embedBuilder = new EmbedBuilder();

            // Search through each command and designate them as either common or moderator commands
            foreach(CommandInfo command in commands)
            {
                // Local tracker to keep tabs on which commands are accounted for
                bool addedToList = false;
                // Get the command summary attribute info
                string embedFieldText = command.Summary ?? "No description available";
                // Check each command to see if it has preconditions. Specifically to populate the moderator command list
                foreach(PreconditionAttribute precondition in command.Preconditions)
                {
                    if(precondition is RequireUserPermissionAttribute)
                    {
                        if(((RequireUserPermissionAttribute)precondition).GuildPermission == GuildPermission.ManageMessages && !addedToList)
                        {
                            // We have a moderator command, add it to the moderator command list
                            moderatorCommandDescriptions.Add(new Tuple<string, string>(command.Name, embedFieldText));
                            addedToList = true;
                        }
                    }
                }
                // If we don't have a moderator command, add it to the common command list
                if(!addedToList)
                {
                    commonCommandDescriptions.Add(new Tuple<string, string>(command.Name, embedFieldText));
                }
            }

            // Get our user for some conditional checks
            IGuildUser user = Context.Message.Author as IGuildUser;
            bool isModerator = user.GuildPermissions.ManageMessages;

            // Add all common commands
            if(isModerator || page == 1)
            foreach(Tuple<string,string> command in commonCommandDescriptions)
            {
                embedBuilder.AddField(command.Item1, command.Item2);
            }

            // Add moderator commands if applicable
            if(isModerator && page == 2)
            {
                embedBuilder.AddField("Moderator commands", "­"); // Not an empty string - an invisible character
                foreach(Tuple<string, string> command in moderatorCommandDescriptions)
                {
                    embedBuilder.AddField(command.Item1, command.Item2);
                }
            }

            await Context.Channel.SendMessageAsync("Here's a list of commands and their descriptions: ", false, embedBuilder.Build());
        }

        #region Match Management Commands
        [Command("Challenge"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Issues a challenge to another player in a game.\nSyntax: !challenge [@PLAYER] [GAME]")]
        public async Task Challenge(IGuildUser otherPerson, string game)
        {
            // Attempt to create the challenge
            int result = fiteEngine.CreateChallenge(Context.Message.Author.Id.ToString(), otherPerson.Id.ToString(), game);
            if(result > 0)
            {
                // Successful match creation
                Embed matchEmbed = CreateMatchEmbed(fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString()));

                // Make the challenge known and alert the other player
                await ReplyAsync(otherPerson.Mention + " you've been challenged!", false, matchEmbed);
            }else if(result == -1)
            {
                // Someone isn't in the database
                await ReplyAsync("Couldn't create match. Either challenger or challenged player is not registered.");
            }else if(result == -2)
            {
                // Game isn't in database
                await ReplyAsync("Couldn't create match. " + game + " is not a valid game.");
            }
            else if(result == -3)
            {
                // Someone's in a match already
                await ReplyAsync("Couldn't create match. Either challenger or challenged player is already associated with a match");
            }
            else if(result == -4)
            {
                // Trying to challenge self
                await ReplyAsync("You can't challenge yourself to a match.");
            }
            else
            {
                // No clue
                await ReplyAsync("Something when wrong. I don't know what, but something.");
            }
        }

        [Command("Accept"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Accepts a challenge from another player.")]
        public async Task Accept()
        {
            int result = fiteEngine.AcceptChallenge(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
                IGuildUser opponent = GetUserFromIdString(currMatch.initiatingPlayer.GetName());

                Embed matchEmbed = CreateMatchEmbed(currMatch);
                await ReplyAsync("Challenge accepted! " + opponent.Mention + ", you're up.", false, matchEmbed);
            }
            else if(result == -1)
            {
                // Accepting player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Accepting player isn't being challenged
                await ReplyAsync("You don't seem to have a match waiting on your response. Get out there, make an enemy or two");
            }
            else if(result == -3)
            {
                // Match is already active
                await ReplyAsync("You're already in an active match. One at a time, bud.");
            }
            else if(result == -4)
            {
                await ReplyAsync("You can't accept your own challenge.");
            }
        }

        [Command("Decline"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Declines a challenge from another player.")]
        public async Task Decline()
        {
            int result = fiteEngine.DeclineChallenge(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                // Success
                Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
                IGuildUser opponent = GetUserFromIdString(currMatch.initiatingPlayer.GetName());

                await ReplyAsync("Challenge Declined! " + opponent.Mention + ", no dice.");
            }
            else if(result == -1)
            {
                // Declining player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Declining player isn't being challenged
                await ReplyAsync("You don't seem to have a match waiting on your response. Get out there, make an enemy or two");
            }
            else if(result == -3)
            {
                // Match is already active
                await ReplyAsync("You're already in an active match. No backing out now.");
            }
            else if(result == -4)
            {
                // Trying to decline their own match
                await ReplyAsync("You can't decline your own challenge. You can, however, !cancel it. Wink.");
            }
        }

        [Command("ReportWin"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Reports that you've won your match. Note that this requires your opponent to !Confirm before it's made official.")]
        public async Task ReportWin()
        {
            int result = fiteEngine.ReportWin(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                // Success
                Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
                // Determine who the opponent is
                IGuildUser opponent = null;
                if(GetUserFromIdString(currMatch.pendingVictor.GetName()).Equals(currMatch.initiatingPlayer.GetName()))
                {
                    opponent = GetUserFromIdString(currMatch.challengedPlayer.GetName());
                }
                else
                {
                    opponent = GetUserFromIdString(currMatch.initiatingPlayer.GetName());
                }

                await ReplyAsync(opponent.Mention + ", " + GetUserLocalName(((IGuildUser)Context.Message.Author)) + " has reported themselves as the winner of your match. Please !Confirm this.");
            }
            else if(result == -1)
            {
                // Reporting player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Reporting player isn't associated with a match
                await ReplyAsync("You don't appear to be in a match. Can't win if you aren't playing.");
            }
            else if(result == -3)
            {
                // Reporting player's match isn't active
                await ReplyAsync("Slow down, your match doesn't seem to be active. A challenge must be !Accepted before it can be won.");
            }
        }

        [Command("ReportLoss"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Reports that you've lost your match. This requires no !Confirm-ation")]
        public async Task ReportLoss()
        {
            // Have to get this first, before match is removed
            Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
            
            int result = fiteEngine.ReportLoss(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                // Success
                IGuildUser opponent = GetUserFromIdString(currMatch.trueVictor.GetName());
                await ReplyAsync(GetUserLocalName(((IGuildUser)Context.Message.Author)) + " graciously admits defeat. Congratulations, " + opponent.Mention + "!");
            }
            else if(result == -1)
            {
                // Reporting player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Reporting player isn't associated with a match
                await ReplyAsync("You don't appear to be in a match. Can't lose if you don't play.");
            }
            else if(result == -3)
            {
                // Reporting player's match isn't active
                await ReplyAsync("Slow down, your match doesn't seem to be active. A challenge must be !Accepted before it can be lost.");
            }
            else if(result == -4)
            {
                // Post-match housekeeping wasn't successfully handled
                await ReplyAsync("Something went wrong while trying to resolve the match. Please try again.");
            }
        }

        [Command("Confirm"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Confirms that the person claiming to have won your match is legitimate.")]
        public async Task ConfirmWin()
        {
            // Have to get this first, before match is removed
            Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
            
            int result = fiteEngine.ConfirmWin(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                // Success
                IGuildUser opponent = GetUserFromIdString(currMatch.pendingVictor.GetName());
                await ReplyAsync(GetUserLocalName(((IGuildUser)Context.Message.Author)) + " graciously admits defeat. Congratulations, " + opponent.Mention + "!");
            }
            else if(result == -1)
            {
                // Confirming player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Confirmer isn't associated with a match
                await ReplyAsync("You don't appear to be in a match.");
            }
            else if(result == -3)
            {
                // There's no pending victor for the match
                await ReplyAsync("It appears that no one has reported victory for your match. Someone must !ReportWin before it can be !Confirm-ed.");
            }
            else if(result == -4)
            {
                // Confirming player is trying to confirm their own win
                await ReplyAsync("You can't confirm your own win.");
            }
            else if(result == -5)
            {
                // Post-match housekeeping wasn't successfully handled
                await ReplyAsync("Something went wrong while trying to resolve the match. Please try again.");
            }
        }

        [Command("Cancel"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Allows for cancellation of challenges you've issued.")]
        public async Task CancelChallenge()
        {
            // Have to get this first, before match is removed
            Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());

            int result = fiteEngine.CancelChallenge(Context.Message.Author.Id.ToString());
            if(result > 0)
            {
                // Success
                IGuildUser opponent = GetUserFromIdString(currMatch.challengedPlayer.GetName());
                await ReplyAsync("Challenge cancelled. " + opponent.Mention + ", you're off the hook.");
            }
            else if(result == -1)
            {
                // Cancelling player isn't registered
                await ReplyAsync("You don't seem to be a registered player. Please !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Canceller isn't associated with a match
                await ReplyAsync("You don't seem to have any issued challenges.");
            }
            else if(result == -3)
            {
                // Canceller isn't the person who issued the challenge
                await ReplyAsync("You can't cancel a challenge you didn't issue. But you can !decline them. wink.");
            }
            else if(result == -4)
            {
                // Match is active and has already been accepted
                await ReplyAsync("This match appears to already be active. Too late to back out now.");
            }
        }

        [Command("ForceWin"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary ("Forces a win for a specific player in their current match.\nSyntax: !forcewin @PLAYER")]
        public async Task ForceWin(IGuildUser player)
        {
            int result = fiteEngine.forceWin(player.Id.ToString());
            if(result > 0)
            {
                // Success
                await ReplyAsync("Operation complete. Congratulations " + player.Mention + ".");
            }
            if(result == -1)
            {
                // Player whose win is being forced isn't registered
                await ReplyAsync(GetUserLocalName(player) + " doesn't seem to be a registered player. Please have them !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Player isn't associated with a match
                await ReplyAsync(GetUserLocalName(player) + " isn't associated with a match.");
            }
            else if(result == -3)
            {
                // Match isn't active
                await ReplyAsync(GetUserLocalName(player) + "'s match isn't active.");
            }
            else if(result == -4)
            {
                // Poast match housekeeping failed
                await ReplyAsync("Couldn't successfully resolve match. Please try again.");
            }
        }

        [Command("CancelMatch"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Cancels a player's match regardless of state.\nSyntax: !cancelmatch @PLAYER")]
        public async Task CancelMatch(IGuildUser player)
        {
            // Have to get this first, before the match is cancelled
            Match currMatch = fiteEngine.GetPlayerMatch(Context.Message.Author.Id.ToString());
            IGuildUser opponent = null;
            if(currMatch != null) { opponent = GetUserFromIdString(currMatch.initiatingPlayer.GetName()); }

            int result = fiteEngine.CancelMatch(player.Id.ToString());
            if(result > 0)
            {
                // Success
                await ReplyAsync("Operation complete." + player.Mention + ", " + opponent.Mention + " - You're free now." );
            }
            if(result == -1)
            {
                // Player whose win is being forced isn't registered
                await ReplyAsync(GetUserLocalName(player) + " doesn't seem to be a registered player. Please have them !register");
                // I'm not sure this should ever happen...but just in case
            }
            else if(result == -2)
            {
                // Player isn't associated with a match
                await ReplyAsync(GetUserLocalName(player) + " isn't associated with a match.");
            }
        }
        #endregion

        #region Player Management Commands
        [Command("Register")]
        [Summary("Registers you within the player database and gives you access to the rest of the server.")]
        public async Task Register()
        {
            var user = Context.User as SocketGuildUser;
            if(user.Roles.Contains(GetRegisteredUserRole(Context)))
            {
                // User already has the registered user role
                if(fiteEngine.AddPlayer(GetMessageAuthorID(Context)) > 0)
                {
                    // Successful addition
                    await ReplyAsync("Registration successful.");
                }
                else
                {
                    // Failure - User already exists
                    await ReplyAsync("You're already registered!");
                }
            }
            else
            {
                // User lacks registered user role
                if(fiteEngine.AddPlayer(GetMessageAuthorID(Context)) > 0)
                {
                    // Successful addition
                    await user.AddRoleAsync(GetRegisteredUserRole(Context));
                    await ReplyAsync("Registration complete. Welcome!");
                }
                else
                {
                    // Failure - User already exists
                    await user.AddRoleAsync(GetRegisteredUserRole(Context));
                    await ReplyAsync("Welcome back!");
                }
            }
        }

        [Command("Rating"), RequireUserPermission(ChannelPermission.ReadMessageHistory)]
        [Summary("Gets the rating of yourself or another player. Either average rating or in a specific game.\nSyntax: !rating [optional GAME] [optional @PLAYER]")]
        public async Task Rating(string game = null, IGuildUser otherPerson = null)
        {
            // Check if there is an optional user passed in and if that user is valid
            IGuildUser user = (IGuildUser)Context.Message.Author;
            if(otherPerson != null){ user = otherPerson; }

            Player player = fiteEngine.GetPlayer(user.Id.ToString());
            if(player == null) { 
                await ReplyAsync("Cannot obtain rating. " + user.Username + " isn't a registered player.");
                return;
            }

            if(game != null)
            {
                FiteEngine.Utilities.Game gameObj = fiteEngine.GetGame(game);
                // We want a game ranking
                if(gameObj != null)
                {
                    GameRanking currRanking = fiteEngine.GetPlayer(user.Id.ToString()).getGameRanking(gameObj.fullTitle);
                    if(currRanking != null)
                    {
                        // Format a ranking and print it
                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.AddField(GetUserLocalName(user) + "'s MMR in " + currRanking.fullGameTitle + ": " + currRanking.rating, "Matches played: " + currRanking.matchesPlayed);
                        await ReplyAsync(null, false, embedBuilder.Build());
                    }
                    else
                    {
                        // Somehow no ranking for this game exists. This shouldn't happen...
                        await ReplyAsync(GetUserLocalName(user) + " somehow has no ranking in " + game + ". This shouldn't happen...please contact a moderator.");
                    }
                }
                else
                {
                    // Invalid game title or shorthand
                    await ReplyAsync(game + " isnt' a valid game title or shorthand.");
                }
            }
            else
            {
                // Format an average rating and print it
                EmbedBuilder embedBuilder = new EmbedBuilder();
                string invisibleCharacter = "­"; // Not an empty string - an invisible character
                embedBuilder.AddField(user.Username + "'s Average MMR: " + player.GetAverageMMR(), invisibleCharacter);
                await ReplyAsync(null, false, embedBuilder.Build());
            }
        }


        #endregion

        #region Game Management Commands
        [Command("AddGame"),RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a new game to the database.\nSyntax: !addgame [FULL TITLE] [SHORTHAND]")]
        public async Task AddGame(string fullTitle, string shorthand)
        {
            int result = fiteEngine.AddGame(fullTitle, shorthand);
            if(result > 0)
            {
                // Success
                await ReplyAsync(fullTitle + " successfully added to game list with shorthand " + shorthand + ".");
            }
            else if (result == -1)
            {
                // Game with title already exists
                await ReplyAsync("Could not add " + fullTitle + " to the list. There is already a game with that title.");
            }
            else if(result == -2)
            {
                // Game with shorthand already exists
                await ReplyAsync("Could not add " + fullTitle + " to the list. There is already a game with the shorthand " + shorthand + ".");
            }
            else if(result == -3)
            {
                // Error with saving
                await ReplyAsync("Could not save list after addition.");
            }
        }

        [Command("RemoveGame"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Removes a game from the database.\nSyntax: !removegame [FULL TITLE]")]
        public async Task RemoveGame(string fullTitle)
        {
            if(fiteEngine.RemoveGame(fullTitle))
            {
                // Success
                await ReplyAsync(fullTitle + " successfully removed from game list.");
            }
            else
            {
                // Failure - Either no game w/ that title or couldn't save
                await ReplyAsync("Could not remove " + fullTitle + " from game list. Please make sure you're using the game's full title and you're spelling it correctly.");
            }
        }

        [Command("ListGames")]
        [Summary("Lists every game in the database.")]
        public async Task ListGames(int page = 1)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            List<List<string>> gameInformation = fiteEngine.GetAllGameTitlesAndDescriptions();

            // Get max number of pages and parse the page input
            double maxNumPages = CalculateMaxPageNumberAndAdjustPassedInPageNumber(gameInformation.Count(), ref page);

            // Get genre list bounds
            int startIndex, maxIndex;
            ConstructEmbeddedListBounds(gameInformation.Count(), page, out startIndex, out maxIndex);

            // Construct genre list embed
            for(int i = startIndex; i < maxIndex; i++)
            {
                embedBuilder.AddField(gameInformation[i][0], gameInformation[i][1]);
            }

            // Ship it out
            await ReplyAsync("Games (" + page + "/" + maxNumPages + "): ", false, embedBuilder.Build());
        }
        #endregion

        #region Genre Management Commands
        [Command("ListGenres")]
        [Summary("Lists every genre in the database.")]
        public async Task ListGenres(int page = 1)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            List<string> genres = fiteEngine.GetGenreList();

            // Get max number of pages and parse the page input
            double maxNumPages = CalculateMaxPageNumberAndAdjustPassedInPageNumber(genres.Count(), ref page);

            // Get genre list bounds
            int startIndex, maxIndex;
            ConstructEmbeddedListBounds(genres.Count(), page, out startIndex, out maxIndex);

            // Construct genre list embed
            for(int i = startIndex; i < maxIndex; i++)
            {
                if(genres[i] != null)
                {
                    string additional = "­"; // Not an empty string - An invisible character
                    embedBuilder.AddField(genres[i], additional);
                }
            }
            
            // Ship it out
            await ReplyAsync("Genres (" + page + "/" + maxNumPages + "): ", false, embedBuilder.Build());
        }

        [Command("AddGenreToBase"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a new genre into the database.\nSyntax: !addgenretobase [GENRE]")]
        public async Task AddGenreToBase(string name)
        {
            if(fiteEngine.AddGenre(name))
            {
                // Successful addition
                await ReplyAsync(name + " successfully added to genre list.");
            }
            else
            {
                // Failure
                await ReplyAsync(name + " could not be added to genre list. Perhaps it already exists?");
            }
        }

        [Command("RemoveGenreFromBase"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Removes a genre from the database.\nSyntax: !removegenrefrombase [GENRE]")]
        public async Task RemoveGenreFromBase(string name)
        {
            if(fiteEngine.RemoveGenre(name))
            {
                // Successful addition
                await ReplyAsync(name + " successfully removed from genre list.");
            }
            else
            {
                // Failure
                await ReplyAsync(name + " could not be removed from genre list. Perhaps it doesn't exist?");
            }
        }

        [Command("AddGenreToGame"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a genre to a game.\nSyntax: !addgenretogame [GENRE] [GAME]")]
        public async Task AddGenreToGame(string genre, string game)
        {
            int result = fiteEngine.AddGenreToGame(game, genre);
            if(result > 0)
            {
                // Success
                await ReplyAsync(genre + " successfully added to " + game + ".");
            } 
            else if(result == -1)
            {
                // Genre doesn't exist
                await ReplyAsync(genre + " is not a valid genre.");
            } 
            else if(result == -2)
            {
                // Game doesn't exist
                await ReplyAsync(game + " is not a valid game.");

            }
            else if(result == -3) 
            {
                // Failure to save
                await ReplyAsync("Failure to save. Please retry command");
            }
            else if(result == -4)
            {
                // If game is already associated with the genre
                await ReplyAsync(game + " already has the genre " + genre + ".");
            }
            else if(result == -5)
            {
                // If the game's genre list is already full
                await ReplyAsync(game + " can't have any more genres.");
            }
        }

        [Command("RemoveGenreFromGame"), RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Removes a genre from a game.\nSyntax: !removegenrefromgame [GENRE] [GAME]")]
        public async Task RemoveGenreFromGame(string genre, string game)
        {
            int result = fiteEngine.RemoveGenreFromGame(game, genre);
            if(result > 0)
            {
                // Success
                await ReplyAsync(genre + " successfully removed from " + game + ".");
            }
            else if(result == -1)
            {
                // Genre isn't in database
                await ReplyAsync(genre + " is not a valid genre.");
            }
            else if(result == -2)
            {
                // Game doesn't exist
                await ReplyAsync(game + " is not a valid game.");

            }
            else if(result == -3)
            {
                // Failure to save
                await ReplyAsync("Failure to save. Please retry command");
            }
            else if(result == -4)
            {
                // Game didn't have the genre
                await ReplyAsync(game + " not associated with genre " + genre + ".");
            }
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Used to get a message author's ID as a string from a context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string GetMessageAuthorID(SocketCommandContext context)
        {
            return context.Message.Author.Id.ToString();
        }

        /// <summary>
        /// Returns the role of the registered user. This implementation uses role colors. 
        /// Registered user role is Magenta. Don't make any other roles magenta.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private SocketRole GetRegisteredUserRole(SocketCommandContext context)
        {
            foreach(SocketRole role in Context.Guild.Roles)
            {
                if(role.Color == Color.Magenta)
                {
                    return role;
                }
            }
            return null;
        }

        /// <summary>
        /// Given a count of items in a list and the page number requested, will calculate the
        /// maximum number of pages available and make sure that the page number passed in is valid
        /// </summary>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        double CalculateMaxPageNumberAndAdjustPassedInPageNumber(int itemCount, ref int pageIn)
        {
            // Get max number of pages
            double ret = Math.Ceiling(itemCount / 25f);
            if(ret == 0) { ret = 1; }

            // Make sure the passed in page number is within bounds
            if(pageIn > ret) { pageIn = (int)ret; } // Make sure we're not above
            if(pageIn < 1) { pageIn = 1; } // Make sure we're not below

            return ret;
        }

        /// <summary>
        /// Constructs the bounds for an embedded list
        /// </summary>
        /// <param name="itemCount"></param>
        /// <param name="page"></param>
        /// <param name="startIndex"></param>
        /// <param name="maxIndex"></param>
        private void ConstructEmbeddedListBounds(int itemCount, int page, out int startIndex, out int maxIndex)
        {
            startIndex = 25 * (page - 1);
            maxIndex = startIndex + 25;
            if(maxIndex > itemCount) { maxIndex = itemCount; }
        }

        /// <summary>
        /// Given a match, will format it to be printed out upon request
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private Embed CreateMatchEmbed(Match match)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            // Challenger
            IGuildUser challenger = GetUserFromIdString(match.initiatingPlayer.GetName());
            embedBuilder.AddField("Challenger", GetUserLocalName(challenger));

            // Challenged
            IGuildUser challenged = GetUserFromIdString(match.challengedPlayer.GetName());
            embedBuilder.AddField("Challenged", GetUserLocalName(challenged));

            // Game
            embedBuilder.AddField("Game", match.game.fullTitle + " (" + match.game.shorthand + ")");

            // Status
            string status = match.isActive ? "Active match" : "Challenge";
            embedBuilder.AddField("Status", status);

            // Pending victor
            if(match.pendingVictor != null && match.trueVictor == null)
            {
                IGuildUser pendingVictor = GetUserFromIdString(match.pendingVictor.GetName());
                embedBuilder.AddField("Pending Victor", GetUserLocalName(pendingVictor));
            }

            // True victor
            if(match.trueVictor != null)
            {
                IGuildUser victor = GetUserFromIdString(match.trueVictor.GetName());
                embedBuilder.AddField("Victor", GetUserLocalName(victor));
            }

            return embedBuilder.Build();
        }

        /// <summary>
        /// Gets the local name of a user. Either their nickname or their username, if they have no set nickname
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private string GetUserLocalName(IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }

        /// <summary>
        /// Given a user id in the form of a string, will return the user associated with it
        /// </summary>
        /// <param name="idString"></param>
        /// <returns></returns>
        private IGuildUser GetUserFromIdString(string idString)
        {
            return Context.Guild.GetUser(Convert.ToUInt64(idString));
        }
        #endregion

        #region Overridden Base functionality
        /// <summary>
        /// Constructor - Makes sure to get a reference to the FiteEngine and the Command Service
        /// </summary>
        /// <param name="engineIn"></param>
        /// <param name="commandServiceIn"></param>
        public MainCommandModule(FiteEngine.FiteEngine engineIn, CommandService commandServiceIn)
        {
            this.fiteEngine = engineIn;
            this.commandService = commandServiceIn;
        }
        #endregion
    }
}