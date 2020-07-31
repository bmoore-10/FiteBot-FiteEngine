using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    [Serializable]
    public class Player
    {
        // Name of the player, should match its key in a FiteEngine's playerDict
        private string name;
        // Average MMR of the player across all games
        private double averageMMR; 
        // Dictionary of player's rankings across all games. Key is game's full title, value is GameRanking object.
        private Dictionary<string, GameRanking> gameRankings;
        // Player's match history across all players they've played against. Key is player name, value is the win/loss record
        private Dictionary<string, WinLossRecord> playerMatchHistory;
        // Player's 10 most recent matches
        private MatchResult[] recentMatches;
        // Maximum number of recent matches tracked
        private static int maximumNumRecentMatches = 10;
        // The current match we're associated with
        private Match currentMatch;

        /// <summary>
        /// Public constructor - Creates a new Player
        /// </summary>
        /// <param name="name"></param>
        /// <param name="gameList"></param>
        public Player(string nameIn, Dictionary<string, Game> gameList)
        {
            // Set our name
            this.name = nameIn;

            // Get all games and create a rating for it
            gameRankings = new Dictionary<string, GameRanking>();
            foreach(Game game in gameList.Values)
            {
                AddGameRanking(game.fullTitle);
            }

            // Calculate and set the average MMR
            CalculateAndSetAverageMMR();

            // Initialize dictionary of player match history
            this.playerMatchHistory = new Dictionary<string, WinLossRecord>();

            // Initialize list of recent matches - Want to explicitly make sure all values are null by default
            this.recentMatches = new MatchResult[maximumNumRecentMatches];
            for(int i = 0; i < maximumNumRecentMatches; i++)
            {
                recentMatches[i] = null;
            }

            // Set that we're not in a match currently
            this.currentMatch = null;
        }

        /// <summary>
        /// Handles post-match housekeeping local to specific player
        /// </summary>
        /// <param name="resultObject"></param>
        public bool HandlePostMatchCalculations(MatchResult resultObject)
        {
            // Update gameranking for this game
            UpdateGameRanking(resultObject);
            // Update win/loss record against the opponent and recalculate the average MMR
            AddPlayerMatchResult(resultObject);
            CalculateAndSetAverageMMR();
            // Save this result into the recent matches
            UpdateRecentMatchHistory(resultObject);
            // Remove currentMatch
            this.SetCurrentMatch(null);

            return true;
        }

        #region Name related
        /// <summary>
        /// Used for getting name of player
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return this.name;
        }
        #endregion

        #region Average MMR related
        /// <summary>
        /// Calculcates average MMR and sets the value
        /// </summary>
        public void CalculateAndSetAverageMMR()
        {
            this.averageMMR = CalculateAverageMMR();
        }

        /// <summary>
        /// Searches through every game saved in the ranking list to get the aggregate average MMR
        /// </summary>
        /// <returns></returns>
        public double CalculateAverageMMR()
        {
            double ret = 0;
            foreach(GameRanking ranking in gameRankings.Values)
            {
                ret += ranking.rating;
            }

            return (ret / gameRankings.Values.Count);
        }

        public double GetAverageMMR()
        {
            return this.averageMMR;
        }
        #endregion

        #region GameRanking related
        /// <summary>
        /// Handles updating the game ranking after a match has been reported
        /// </summary>
        /// <param name="result"></param>
        private void UpdateGameRanking(MatchResult result)
        {
            // Find the GameRanking related to this game
            GameRanking currGameRanking = gameRankings[result.game.fullTitle];
            // Update the ranking according to match result
            currGameRanking.updateRanking(result);
        }

        /// <summary>
        /// Allows for obtaining GameRanking object for a specific game
        /// </summary>
        /// <param name="game"></param>
        /// <returns>A GameRanking object if one exists. Null otherwise</returns>
        public GameRanking getGameRanking(string game)
        {
            // Parse game name

            // Return the related game ranking
            if(gameRankings.ContainsKey(game))
            {
                return gameRankings[game];
            }
            else
            {
                // Should never happen - A player should have a ranking in all games, whether or not they've played it
                return null;
            }
        }

        /// <summary>
        /// Adds a new default gameranking for a new game
        /// </summary>
        /// <param name="name"></param>
        public void AddGameRanking(string name)
        {
            this.gameRankings.Add(name, new GameRanking(name));
            CalculateAndSetAverageMMR();
        }

        /// <summary>
        /// Attempts to remove the gameranking for a game
        /// </summary>
        public bool RemoveGameRanking(string name)
        {
            if(gameRankings.ContainsKey(name))
            {
                gameRankings.Remove(name);
                return true;
            }
            else
            {
                // Game doesn't exist within our gamerankings
                return false;
            }
            CalculateAndSetAverageMMR();
        }
        #endregion

        #region Player Match History related
        /// <summary>
        /// Handles adding the results of a match to a player's match history
        /// </summary>
        /// <param name="result"></param>
        public void AddPlayerMatchResult(MatchResult result)
        {
            // If we don't have a history with this player, add it
            if(!playerMatchHistory.ContainsKey(result.opponent.GetName()))
            {
                playerMatchHistory.Add(result.opponent.GetName(), new WinLossRecord(result.opponent.GetName()));
            }
            playerMatchHistory[result.opponent.GetName()].AddResult(result);
        }
        #endregion

        #region Recent match history related
        /// <summary>
        /// Updates recent match history, making sure to only track a certain amount
        /// </summary>
        /// <param name="result"></param>
        public void UpdateRecentMatchHistory(MatchResult result)
        {
            // Check if we are at maximum capacity
            for(int i = 0; i < maximumNumRecentMatches; i++)
            {
                // If not, add the match into the next open space
                if(recentMatches[i] == null)
                {
                    recentMatches[i] = result;
                    return;
                }
            }

            // If so, add the most recent match to the front by constructing a new array
            MatchResult[] ret = new MatchResult[maximumNumRecentMatches];
            ret[0] = result;
            for(int i = 1; i < maximumNumRecentMatches; i++)
            {
                ret[i] = recentMatches[i - 1];
            }
            recentMatches = ret;
        }
        #endregion

        #region Current Match related
        /// <summary>
        /// Allows for controlling the value of inMatch
        /// </summary>
        /// <param name="val"></param>
        public void SetCurrentMatch(Match val)
        {
            this.currentMatch = val;
        }

        /// <summary>
        /// Allows for getting status of if player's in match or not
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public Match GetCurrentMatch()
        {
            return this.currentMatch;
        }
        #endregion
    }
}
