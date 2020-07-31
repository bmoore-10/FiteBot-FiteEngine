using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Used to hold match result data once a match has been completed
    /// </summary>
    [Serializable]
    public class MatchResult
    {
        public Game game { get; private set; }            // The game that the match was played in
        public Player opponent { get; private set; }      // Opponent
        public bool victory { get; private set; }         // Whether the match resulted in a victory for this player
        public double oldRating { get; private set; }     // Rating before the match results were evaluated
        public double newRating { get; private set; }     // Rating after the match results were evaluated
        public double ratingChange { get; private set; }  // Change in rating from before and after match results were evaluated
        public double newDeviation { get; private set; }  // Player's new deviation
        public double newVolatility { get; private set; } // Player's new volatility rating

        public MatchResult(Game gamein, Player opponentIn, bool victoryIn, double oldRatingIn, double newRatingIn, double newDeviationIn, double newVolatilityIn)
        {
            this.game = gamein;
            this.opponent = opponentIn;
            this.victory = victoryIn;
            this.oldRating = oldRatingIn;
            this.newRating = newRatingIn;
            this.ratingChange = newRatingIn - oldRatingIn;
            this.newDeviation = newDeviationIn;
            this.newVolatility = newVolatilityIn;
        }
    }
}