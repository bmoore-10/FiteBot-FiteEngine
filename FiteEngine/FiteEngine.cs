using FiteEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine
{
    public class FiteEngine
    {
        // The GlickoEngine responsible for handling all things Glicko
        private GlickoEngine glickoEngine;

        // Persistent data
        private List<string> genreList;
        private Dictionary<string, Player> playerDict;
        private Dictionary<string, Game> gameDict;

        // Match related
        private List<Match> pendingMatches;
        private List<Match> activeMatches;

        /// <summary>
        /// Constructor, creates a new instance of a FiteEngine
        /// </summary>
        public FiteEngine()
        {
            glickoEngine = new GlickoEngine();

            // Attempt to load the engine data. If it doesn't exist, create it.
            if(SerializationSurrogate.allDataFilesExist()) {
                // Load in all of our persistent data
                object[] obj = SerializationSurrogate.Load();
                this.playerDict = obj[0] as Dictionary<string, Player>;
                this.gameDict = obj[1] as Dictionary<string, Game>;
                this.genreList = obj[2] as List<string>;

                this.pendingMatches = new List<Match>();
                this.activeMatches = new List<Match>();

                // Since we don't store any matches, we have to make sure no players are stuck "inMatch" on load
                foreach(Player player in playerDict.Values)
                {
                    player.SetCurrentMatch(null);
                }
            }
            else
            {
                // Create all new persistent data and save them out for future use
                this.playerDict = new Dictionary<string, Player>();
                this.gameDict = new Dictionary<string, Game>();
                this.genreList = new List<string>();

                Save();
            }
        }

        #region Match-related functions
        /// <summary>
        /// Creates a challenge - a non active match added to the pendingMatches list
        /// </summary>
        /// <param name="initating"></param>
        /// <param name="challenged"></param>
        /// <param name="gameShorthand">Either the shorthand or full title of the game</param>
        /// <returns>
        ///     1 for success
        ///    -1 if one of the players isn't in the playerDict
        ///    -2 if the game isn't in the gameDict
        ///    -3 if either player is associated with a match
        ///    -4 if someone is trying to challenge themselves
        /// </returns>
        public int CreateChallenge(string initating, string challenged, string gameString)
        {
            // Get the player objects for each player, taking care to take the strings in as lowercase
            Player initiator = GetPlayer(initating.ToLower());
            Player opponent = GetPlayer(challenged.ToLower());

            // Make sure they both exist. If either doesn't, fail
            if(initiator == null || opponent == null) { return -1; }

            // Make sure they're not the same person
            if(initiator.GetName().Equals(opponent.GetName())) { return -4; }

            // Make sure neither player is already associated with a match
            if(initiator.GetCurrentMatch() != null || opponent.GetCurrentMatch() != null) { return -3; }

            // Get the game object, failing if we can't find it by title or shorthand. Remember to convert ToLower
            Game game;
            game = GetGame(gameString.ToLower()); // Try searching by full title
            if(game == null)
            {
                // Try searching by shorthand
                game = GetGameByShorthand(gameString.ToLower());
            }
            // Can't find game by title or shorthand
            if(game == null) { return -2; }

            // We've verified both players and the game exist. Let's create the match and add it to the pending matches list
            Match newMatch = new Match(initiator, opponent, game);
            pendingMatches.Add(newMatch);
            initiator.SetCurrentMatch(newMatch); // Both players are associated with a match
            opponent.SetCurrentMatch(newMatch); // So let's reflect that

            return 1;
        }

        /// <summary>
        /// Allows for acceptance of a match so that it becomes an active match instead of just a challenge
        /// </summary>
        /// <param name="inMatch"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the accepting player doesn't exist in our dictionary
        ///    -2 if accepting player is not currently being challenged
        ///    -3 if the match is already active, and has thus already been accepted
        ///    -4 if acceptor is trying to trying to accept their own challenge
        /// </returns>
        public int AcceptChallenge(string acceptingPlayerName)
        {
            // Try to get the accepting player and fail if they don't exist
            Player acceptingPlayer = GetPlayer(acceptingPlayerName.ToLower());
            if(acceptingPlayer == null) { return -1; }

            // Attempt to get the acceptor's current match. If nonexistant, fail
            Match currMatch = acceptingPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the acceptor is the challenged player and not the initiator of the current match. If they are, fail
            if(currMatch.initiatingPlayer.GetName().Equals(acceptingPlayer.GetName())){ return -4; }

            // Check that this match isn't already running
            if(currMatch.isActive) { return -3; }

            // Otherwise, allow the match to be accepted and make it an active match
            currMatch.SetActive();
            pendingMatches.Remove(currMatch);
            activeMatches.Add(currMatch);

            return 1;
        }

        /// <summary>
        /// Allows for declining of a match so that it gets taken off the active matches list and both players are freed from their matches
        /// </summary>
        /// <param name="decliningPlayerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the declining player doesn't exist in our dictionary
        ///    -2 if declining player is not currently being challenged
        ///    -3 if the match is already active, and has thus already been accepted
        ///    -4 if declining player is attempting to decline their own challenge
        /// </returns>
        public int DeclineChallenge(string decliningPlayerName)
        {
            // Try to get the accepting player and fail if they don't exist
            Player decliningPlayer = GetPlayer(decliningPlayerName.ToLower());
            if(decliningPlayer == null) { return -1; }

            // Attempt to get the acceptor's current match. If nonexistant, fail
            Match currMatch = decliningPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the acceptor is the challenged player and not the initiator of the current match. If they are, fail
            if(currMatch.initiatingPlayer.GetName().Equals(decliningPlayer.GetName())){ return -4; }

            // Check that this match isn't already running
            if(currMatch.isActive) { return -3; }

            // Otherwise, delete the match and remove it from both players' currentMatch lists
            currMatch.initiatingPlayer.SetCurrentMatch(null);
            currMatch.challengedPlayer.SetCurrentMatch(null);
            pendingMatches.Remove(currMatch);

            return 1;
        }

        /// <summary>
        /// Allows for a player to attempt reporting their match as a win. Sets them as the pending victor until confirmation
        /// Also allows for overwriting who is reported as the winner. Should avoid tedium on account of a mistake
        /// </summary>
        /// <param name="reportingPlayerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the reporting player isn't registered
        ///    -2 if the reporting player isn't currently associated with a match
        ///    -3 if the reporting player's match isn't active
        /// </returns>
        public int ReportWin(string reportingPlayerName)
        {
            // Try to get the reporting player and fail if they don't exist
            Player reportingPlayer = GetPlayer(reportingPlayerName.ToLower());
            if(reportingPlayer == null) { return -1; }

            // Attempt to get the acceptor's current match. If nonexistant, fail
            Match currMatch = reportingPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the player's match is active
            if(!currMatch.isActive) { return -3; }

            // Otherwise, set the pending victor and report success
            currMatch.SetPendingVictor(reportingPlayer);
            return 1;
        }

        /// <summary>
        /// Allows for a player to report that the pending victor did indeed win the match.
        /// </summary>
        /// <param name="confirmerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the confirming player isn't registered
        ///    -2 if the confirming player is not associated with a match
        ///    -3 if there is no pending victor for their match
        ///    -4 if the confirming player is trying to confirm their own win (They're the pending victor)
        ///    -5 if post-match housekeeping wasn't successfully handled
        /// </returns>
        public int ConfirmWin(string confirmerName)
        {
            // Try to get the confirming player and fail if they don't exist
            Player confirmingPlayer = GetPlayer(confirmerName.ToLower());
            if(confirmingPlayer == null) { return -1; }

            // Attempt to get the confirmer's current match. If nonexistant, fail
            Match currMatch = confirmingPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the match actually needs to be confirmed
            if(currMatch.pendingVictor == null) { return -3; }

            // Check that the confirmer isn't the pending victor
            if(currMatch.pendingVictor.GetName().Equals(confirmingPlayer.GetName())) { return -4; }

            // Otherwise, handle post-match housekeeping
            activeMatches.Remove(currMatch);
            currMatch.SetTrueVictor(currMatch.pendingVictor);
            if(HandlePostMatchCalculations(currMatch))
            {
                currMatch.initiatingPlayer.SetCurrentMatch(null);
                currMatch.challengedPlayer.SetCurrentMatch(null);
                return 1;
            }
            else
            {
                return -5;
            }
        }

        /// <summary>
        /// Allows for a player to report that they lost the match. No !confirmation check
        /// </summary>
        /// <param name="reportingPlayerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the reporting player isn't registered
        ///    -2 if the reporting player is not associated with a match
        ///    -3 if the reporting player's match isn't active
        ///    -4 if post-match housekeeping wasn't successfully handled
        /// </returns>
        public int ReportLoss(string reportingPlayerName)
        {
            // Try to get the reporting player and fail if they don't exist
            Player reportingPlayer = GetPlayer(reportingPlayerName.ToLower());
            if(reportingPlayer == null) { return -1; }

            // Attempt to get the confirmer's current match. If nonexistant, fail
            Match currMatch = reportingPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the match is active
            if(!currMatch.isActive) { return -3; }

            // Otherwise, handle post-match housekeeping
            activeMatches.Remove(currMatch);
            if(currMatch.initiatingPlayer.GetName().Equals(reportingPlayer.GetName()))
            {
                // If reporter is initator, set winner as challenged player
                currMatch.SetTrueVictor(currMatch.challengedPlayer);
            }
            else
            {
                // If reporter is the challenged player, set winner as initiator
                currMatch.SetTrueVictor(currMatch.initiatingPlayer);
            }
            if(HandlePostMatchCalculations(currMatch))
            {
                currMatch.initiatingPlayer.SetCurrentMatch(null);
                currMatch.challengedPlayer.SetCurrentMatch(null);
                return 1;
            }
            else
            {
                return -4;
            }
        }

        /// <summary>
        /// Forces a win for winningPlayerName
        /// </summary>
        /// <param name="winningPlayerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the player isn't in the database
        ///    -2 if the player isn't in a match
        ///    -3 if that match is not active and thus hasn't been accepted
        ///    -4 if the post match housekeeping fails
        /// </returns>
        public int forceWin(string winningPlayerName)
        {
            // Try to get the reporting player and fail if they don't exist
            Player winningPlayer = GetPlayer(winningPlayerName.ToLower());
            if(winningPlayer == null) { return -1; }

            // Attempt to get the confirmer's current match. If nonexistant, fail
            Match currMatch = winningPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Check that the match is active
            if(!currMatch.isActive) { return -3; }

            // Report that the proper player won
            activeMatches.Remove(currMatch);
            currMatch.SetTrueVictor(winningPlayer);
            if(HandlePostMatchCalculations(currMatch))
            {
                currMatch.initiatingPlayer.SetCurrentMatch(null);
                currMatch.challengedPlayer.SetCurrentMatch(null);
                return 1;
            }
            else
            {
                return -4;
            }
        }

        /// <summary>
        /// Allows for the cancelling of challenges
        /// Note that only the initating player may cancel a challenge. To cancel from the other side, must use Decline
        /// </summary>
        /// <param name="cancellingPlayerName"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the canceller isn't in the database
        ///    -2 if the canceller isn't in a match
        ///    -3 if the canceller isn't the person who issued the challenge
        ///    -4 if the match is already active and has thus already been accepted
        /// </returns>
        public int CancelChallenge(string cancellingPlayerName)
        {
            // Try to get the reporting player and fail if they don't exist
            Player cancellingPlayer = GetPlayer(cancellingPlayerName.ToLower());
            if(cancellingPlayer == null) { return -1; }

            // Attempt to get the confirmer's current match. If nonexistant, fail
            Match currMatch = cancellingPlayer.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Make sure the canceller is the person who issues the challenge
            // Probably not necessary to force the challenged player to use Decline but bleh
            if(!cancellingPlayer.GetName().Equals(currMatch.initiatingPlayer.GetName())) { return -3; }

            // Check that the match is active - Can't cancel an already active match
            if(currMatch.isActive) { return -4; }

            // Otherwise, cancel the challenge
            pendingMatches.Remove(currMatch);
            currMatch.initiatingPlayer.SetCurrentMatch(null);
            currMatch.challengedPlayer.SetCurrentMatch(null);
            return 1;
        }

        /// <summary>
        /// Allows for cancelling of matches - Should probably only be usable by moderators...
        /// </summary>
        /// <param name="playerAssociatedWithMatchName">The name of either player associated with the match to be cancelled</param>
        /// <returns>
        ///     1 for success
        ///    -1 if the passed in player name isn't registered in the database
        ///    -2 if that player doesn't have a match to be cancelled
        /// </returns>
        public int CancelMatch(string playerAssociatedWithMatchName)
        {
            // Try to get the reporting player and fail if they don't exist
            Player player = GetPlayer(playerAssociatedWithMatchName.ToLower());
            if(player == null) { return -1; }

            // Attempt to get the confirmer's current match. If nonexistant, fail
            Match currMatch = player.GetCurrentMatch();
            if(currMatch == null) { return -2; }

            // Otherwise, run cancel logic depending on if the match is active or still a challenge
            if(currMatch.isActive)
            {
                // If the match is already active
                activeMatches.Remove(currMatch);
                currMatch.initiatingPlayer.SetCurrentMatch(null);
                currMatch.challengedPlayer.SetCurrentMatch(null);
            }
            else
            {
                // If the match is still a challenge
                pendingMatches.Remove(currMatch);
                currMatch.initiatingPlayer.SetCurrentMatch(null);
                currMatch.challengedPlayer.SetCurrentMatch(null);
            }
            return 1;
        }

        /// <summary>
        /// Allows for retrieval of player's current match - DOES NOT NULLCHECK FOR PLAYER,
        /// MUST DO THAT IN INTERFACE
        /// </summary>
        /// <param name="player"></param>
        /// <returns>
        ///     The match object or null if the player's not in a match
        /// </returns>
        public Match GetPlayerMatch(string player)
        {
            return playerDict[player].GetCurrentMatch();
        }

        /// <summary>
        /// Handles MMR changes and other housekeeping related to a match being finalized
        /// </summary>
        /// <param name="match"></param>
        /// <returns>
        ///     True for successful handling
        ///     False for failure
        /// </returns>
        private bool HandlePostMatchCalculations(Match match)
        {
            // First, get the match results from the glicko engine
            MatchResult[] results = glickoEngine.HandleReport(match);
            if(this.Save()){

                // Handle housekeeping for the winner
                return (match.initiatingPlayer.HandlePostMatchCalculations(results[0]) &&
                       match.challengedPlayer.HandlePostMatchCalculations(results[1]));
            }
            // If saving fails
            return false;
        }
        #endregion

        #region Genre-related functions
        /// <summary>
        /// Handles adding a genre to the genrelist. Genres are stored in all lowercase
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <returns> Success or failure </returns>
        public bool AddGenre(string fullTitle)
        {
            // Want to handle these in lowercase
            string adjustedName = fullTitle.ToLower();
            if(!genreList.Contains(adjustedName))
            {
                genreList.Add(adjustedName);
                if(this.Save())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a genre to one of the games in the database
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="genreToQuery"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the genre doesn't exist in the database
        ///    -2 if the game doesn't exist in the database
        ///    -3 if saving fails
        ///    -4 if the game already is associated with that genre
        ///    -5 if the game's genre list is full
        /// </returns>
        public int AddGenreToGame(string gameName, string genreToQuery)
        {
            // Want to handle these in lowercase
            string adjustedGameTitle = gameName.ToLower();
            string adjustedGenre = genreToQuery.ToLower();

            Game gameObject = GetGame(adjustedGameTitle);
            if(gameObject == null)
            {
                // If game isn't in the dictionary, fail
                return -2;
            }
            if(!genreList.Contains(adjustedGenre))
            {
                // If genre doesn't exist in the databse
                return -1;
            }

            // If both game and genre exist, attempt to add the genre to the game
            int result = gameDict[gameObject.fullTitle].addGenre(adjustedGenre);
            // Make sure this addition worked
            if(result == -1)
            {
                // List already includes genre
                return -4;
                    
            }else if(result == -2)
            {
                // List is full
                return -5;
            }
            // Try saving
            if(this.Save())
            {
                return 1;
            }
            else
            {
                return -3;
            }
        }

        /// <summary>
        /// Remove genre from one of the games in the database
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="genreToQuery"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if genre isn't in database
        ///    -2 if game isn't in database
        ///    -3 if failed to save
        ///    -4 if game doesn't have the genre
        /// </returns>
        public int RemoveGenreFromGame(string gameName, string genreToQuery)
        {
            // Want to handle these in lowercase
            string adjustedGameTitle = gameName.ToLower();
            string adjustedGenre = genreToQuery.ToLower();

            Game gameObject = GetGame(adjustedGameTitle);
            if(gameObject == null)
            {
                // If game isn't in the dictionary, fail
                return -2;
            }
            if(!genreList.Contains(adjustedGenre))
            {
                // If genre doesn't exist in the databse
                return -1;
            }

            // If game and genre both exist, attemtp to remove genre from the game
            if(gameDict[gameObject.fullTitle].removeGenre(adjustedGenre))
            {
                // Attempt to save
                if(this.Save())
                {
                    return 1;
                }
                else
                {
                    return -3;
                }
            }
            else
            {
                // Failure - Genre already attributed to the game
                return -4;

            }
        }

        /// <summary>
        /// Handles removing a genre from the genrelist. Genres are stored in all lowercase
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <returns></returns>
        public bool RemoveGenre(string fullTitle)
        {
            // Want to handle these in lowercase
            string adjustedName = fullTitle.ToLower();
            if(genreList.Contains(adjustedName))
            {
                // Remove genre from every game that has the genre listed
                foreach(Game game in gameDict.Values)
                {
                    if(game.HasGenre(adjustedName))
                    {
                        game.removeGenre(adjustedName);
                    }
                }
                // Remove genre
                genreList.Remove(adjustedName);

                if(this.Save())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Genre doesn't exist
                return false;
            }
        }

        /// <summary>
        /// Returns the current genre list
        /// </summary>
        /// <returns></returns>
        public List<string> GetGenreList()
        {
            return this.genreList;
        }

        /// <summary>
        /// Returns whether or not the genre exists within the genrelist
        /// </summary>
        /// <param name="genre"></param>
        /// <returns></returns>
        public bool HasGenre(string genre)
        {
            return genreList.Contains(genre.ToLower());
        }
        #endregion

        #region Player-related functions
        /// <summary>
        /// Used to add player to player dictionary
        /// </summary>
        /// <param name="name"></param>
        /// <returns>
        ///     1 for success
        ///    -1 for player already exists
        ///    -2 for failure to save
        /// </returns>
        public int AddPlayer(string name)
        {
            // Want to handle these in lowercase
            string adjustedName = name.ToLower();

            if(playerDict.ContainsKey(adjustedName))
            {
                // If player already exists, fail
                return -1;
            }
            else
            {
                // Otherwise, create a new character and keep track of them
                playerDict.Add(adjustedName, new Player(adjustedName, gameDict));
                if(this.Save())
                {
                    return 1;
                }
                else
                {
                    return -2;
                }
            }
        }
        /// <summary>
        /// Used to remove player from player dictionary
        /// </summary>
        /// <param name="name"></param>
        /// <returns>
        ///     1 for success
        ///    -1 if the player doesn't exist within the playerdict
        ///    -2 if failure to save
        /// </returns>
        public int RemovePlayer(string name)
        {
            // Want to handle these in lowercase
            string adjustedName = name.ToLower();

            if(playerDict.ContainsKey(adjustedName))
            {
                // If player exists, remove them
                playerDict.Remove(adjustedName);
                if(this.Save())
                {
                    return 1;
                }
                else
                {
                    return -2;
                }
            }
            else
            {
                // Otherwise, fail
                return -1;
            }
        }
        /// <summary>
        /// Used for getting a player from the playerDict
        /// </summary>
        /// <param name="name"></param>
        /// <returns>
        ///     A player if valid query
        ///     Null if player doesn't exist
        /// </returns>
        public Player GetPlayer(string name)
        {
            // Want to handle these in lowercase
            string adjustedName = name.ToLower();

            if(playerDict.ContainsKey(adjustedName))
            {
                return playerDict[adjustedName];
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Game-related functions
        /// <summary>
        /// Adds a game to the gamedict
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <param name="shorthand"></param>
        /// <returns>
        ///     1 for success
        ///    -1 Game with full title already exists in gamedict
        ///    -2 Game with shorthand already exists in the gamedict
        ///    -3 if saving fails
        /// </returns>
        public int AddGame(string fullTitle, string shorthand)
        {
            // Want to handle these in lowercase
            string fullTitleAdjusted = fullTitle.ToLower();
            string shortHandAdjusted = shorthand.ToLower();

            // Create the game object and add it to the gamedict
            foreach(Game game in gameDict.Values)
            {
                if(game.fullTitle.Equals(fullTitleAdjusted))
                {
                    // Full title already exists in the dictionary
                    return -1;
                }

                if(game.shorthand.Equals(shortHandAdjusted))
                {
                    // Shorthand already exists in the director
                    return -2;
                }
            }

            // Add the game to the dictionary
            gameDict.Add(fullTitleAdjusted, new Game(fullTitleAdjusted, shortHandAdjusted));
            
            // Give every player a ranking in this game
            foreach(Player player in playerDict.Values)
            {
                player.AddGameRanking(fullTitleAdjusted);
            }

            if(this.Save())
            {
                return 1;
            }
            else
            {
                return -3;
            }
        }

        /// <summary>
        /// Removes a game from the gamedict
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <returns>
        ///     true for success
        ///     false if the game doesn't exist in the dictionary or failure to save
        /// </returns>
        public bool RemoveGame(string fullTitle)
        {
            // Want to handle these in lowercase
            string fullTitleAdjusted = fullTitle.ToLower();

            if(gameDict.ContainsKey(fullTitleAdjusted))
            {
                // Remove every player's ranking for this game
                foreach(Player player in playerDict.Values)
                {
                    player.RemoveGameRanking(fullTitleAdjusted);
                }
                // If the game does exist, remove it from the dictionary
                if(gameDict.Remove(fullTitleAdjusted))
                {
                    if(this.Save())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // If removal of the game fails for some reason
                    return false;
                }
            }
            else
            {
                // If the game doesn't exist in the library
                return false;
            }
        }

        /// <summary>
        /// Used to retrieve a game from the game dictionary
        /// </summary>
        /// <param name="gameName"></param>
        /// <returns>
        ///     The requested game object if valid
        ///     Null if the game object doesn't exist
        /// </returns>
        public Game GetGame(string gameName)
        {
            // Want to handle these in lowercase
            string fullTitleAdjusted = gameName.ToLower();

            // Attempt to get game by proper full title
            Game ret = GetGameByFullTitle(fullTitleAdjusted);
            if(ret != null) { return ret; }
            // Attempt to get game by shorthand
            ret = GetGameByShorthand(fullTitleAdjusted);
            if(ret != null) { return ret; }
            // Otherwise, return null
            return null;
        }

        /// <summary>
        /// Used to retrieve a game from the game dictionary by its full title
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <returns></returns>
        public Game GetGameByFullTitle(string fullTitle)
        {
            if(gameDict.ContainsKey(fullTitle.ToLower())){
                return gameDict[fullTitle.ToLower()];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Used to retrieve a game from the game dictionary by its shorthand
        /// </summary>
        /// <param name="gameShorthand"></param>
        /// <returns>Game if valid, null if doesn't exist</returns>
        public Game GetGameByShorthand(string gameShorthand)
        {
            foreach(Game game in gameDict.Values)
            {
                // If we find the game, return it
                if(game.shorthand.Equals(gameShorthand.ToLower())){
                    return game;
                }
            }
            // Otherwise, return null
            return null;
        }

        /// <summary>
        /// Used to retrieve formatted titles and descriptions of all games
        /// </summary>
        /// <returns>A list of formatted titles and descriptions of each game in the database</returns>
        public List<List<string>> GetAllGameTitlesAndDescriptions()
        {
            List<List<string>> ret = new List<List<string>>();
            foreach(string title in this.gameDict.Keys)
            {
                ret.Add(GetGameTitleAndDescription(title));
            }
            return ret;
        }

        /// <summary>
        /// Used to retrieve a formatted title and description of a game
        /// </summary>
        /// <param name="fullTitle"></param>
        /// <returns> A string list with element 0 being the full title and element 1 being information about the game</returns>
        public List<string> GetGameTitleAndDescription(string fullTitle)
        {
            List<string> ret = new List<string>();
            Game game = GetGame(fullTitle);
            if(game != null)
            {
                // Add the title as element 0
                ret.Add(game.fullTitle);
                // Format a string with the shorthand and genres as element 1
                string infoLine = "Shorthand: " + game.shorthand + " | Genres: ";
                foreach(string genre in game.genres)
                {
                    infoLine += genre + " ";
                }
                ret.Add(infoLine);
            }
            else
            {
                ret.Add(null);
            }

            return ret;
        }
        #endregion

        #region Data
        /// <summary>
        /// Handles saving our persistent data
        /// </summary>
        public bool Save()
        {
            return SerializationSurrogate.Save(this.playerDict, this.gameDict, this.genreList);
        }
        #endregion
    }
}
