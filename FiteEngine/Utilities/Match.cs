using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Represents a match between two players
    /// </summary>
    [Serializable]
    public class Match
    {
        // The player who initiated the challenge
        public Player initiatingPlayer { get; private set; }
        // The player who was challenged
        public Player challengedPlayer { get; private set;}
        // What game the match is being played in
        public Game game { get; private set; }
        // Whether or not the match is currently active (No longer in pending state / isn't still a "challenge")
        public bool isActive { get; private set; }
        // The player who reported that they won. If not null, we are awaiting confirmation from the other player
        public Player pendingVictor { get; private set; }
        // The actual victor of the match
        public Player trueVictor { get; private set; }

        /// <summary>
        /// Constructor - Returns a pending match
        /// </summary>
        /// <param name="initator"></param>
        /// <param name="challenged"></param>
        /// <param name="gameIn"></param>
        public Match(Player initator, Player challenged, Game gameIn)
        {
            // Set the passed in data
            this.initiatingPlayer = initator;
            this.challengedPlayer = challenged;
            this.game = gameIn;

            // Set the rest of the defaults
            this.isActive = false;
            this.pendingVictor = null;
            this.trueVictor = null;
        }

        /// <summary>
        /// Allows for setting a match to active
        /// </summary>
        public void SetActive()
        {
            this.isActive = true;
        }

        /// <summary>
        /// Attempts to set the pending victor of the match
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void SetPendingVictor(Player player)
        {
            this.pendingVictor = player;
        }

        /// <summary>
        /// Attempts to set the true victor of the match
        /// </summary>
        /// <param name="player"></param>
        public void SetTrueVictor(Player player)
        {
            this.trueVictor = player;
        }
    }
}
