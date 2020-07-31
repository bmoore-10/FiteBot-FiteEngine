using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Represents a game ranking for a certain player in a certain game
    /// </summary>
    [Serializable]
    public class GameRanking
    {
        public string fullGameTitle { get; private set; }       // Full title of the game for which this ranking exists
        public double rating { get; private set; }     // Player's rating in this specific game
        public double deviation { get; private set; }  // Player's deviation value for this specific game
        public double volatility { get; private set; } // Player's volatility value for this specific game
        public long matchesPlayed { get; private set; } // How many matches have been played in this game for this ranking

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public GameRanking(string name)
        {
            this.fullGameTitle = name;
            // Fill out some default values
            this.rating = 1500;
            this.deviation = 350;
            this.volatility = 0.06;
            this.matchesPlayed = 0;
        }

        /// <summary>
        /// Updates the game ranking according to a match result
        /// </summary>
        /// <param name="result"></param>
        public void updateRanking(MatchResult result)
        {
            this.rating = result.newRating;
            this.deviation = result.newDeviation;
            this.volatility = result.newVolatility;
            this.matchesPlayed++;
        }
    }
}
