using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Represents a record of wins and losses in a certain game against a certain player
    /// </summary>
    [Serializable]
    public class WinLossRecord
    {
        // Name of WinLossRecord. Should match the key in Player's WinLossRecord dictionary - The opponent's name
        public string name { get; private set; } 

        // Dictionary containing win/loss between two players in a specific game.
        // Key is game, value is the corresponding WinLossObject
        private Dictionary<string, WinLossObject> winLossRecord;

        /// <summary>
        /// Constructor
        /// </summary>
        public WinLossRecord(string nameIn)
        {
            this.name = nameIn;
            this.winLossRecord = new Dictionary<string, WinLossObject>();
        }

        /// <summary>
        /// Adds the result of a game into the WinLossRecord
        /// </summary>
        /// <param name="game"></param>
        /// <param name="matchResult"></param>
        public void AddResult(MatchResult matchResult)
        {
            string game = matchResult.game.fullTitle; // Cache the game name for convenience
            // First, make sure a record exists for the specific game
            if(!winLossRecord.ContainsKey(game))
            {
                winLossRecord.Add(game, new WinLossObject(game));
            }
            // Alter the record depending on the match results
            if(matchResult.victory)
            {
                // Add a win to the win/loss record
                winLossRecord[game].AddWin(matchResult.ratingChange);
            }
            else
            {
                // Add a loss to the win/loss record
                winLossRecord[game].AddLoss(matchResult.ratingChange);
            }
        }
    }
}
