using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiteEngine.Utilities
{
    /// <summary>
    /// Represents a game 
    /// </summary>
    [Serializable]
    public class Game
    {
        // Full title of the game
        public string fullTitle { get; private set; }
        // The shorthand/nickname of the game. We'll generally use this for commands
        public string shorthand { get; private set; }
        // List of genres this game belongs to
        public string[] genres { get; private set; }
        public int numGenres { get; private set; }
        // Tau value of the game
        public float tau = 0.7f;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game(string full, string nickname)
        {
            this.fullTitle = full;     // Store in lowercase
            this.shorthand = nickname; // Store in lowercase
            this.genres = new string[3] { null, null, null };
            this.numGenres = 0;
        }

        /// <summary>
        /// Handles attempting to add a genre to this game's genre list
        /// </summary>
        /// <param name="genres"></param>
        /// <returns> 
        ///     1 if additon success
        ///    -1 if list already includes genre
        ///    -2 if list is full
        /// </returns>
        public int addGenre(string genreToAdd)
        {
            if(this.genres.Contains(genreToAdd))
            {
                // If we already have this genre, fail
                return -1;
            } else if(this.numGenres >= 3)
            {
                // If we have too many genres, fail
                return -2;
            }
            else
            {
                // Otherwise, add the genre
                this.genres[numGenres] = genreToAdd;
                numGenres++;
                return 1;
            }
        }

        /// <summary>
        /// Handles attempting to remove a genre from the genre list
        /// </summary>
        /// <param name="genreToRemove"></param>
        /// <param name="genreList"></param>
        /// <returns>
        ///     True if removal success
        ///     False if the genre doesn't exist
        /// </returns>
        public bool removeGenre(string genreToRemove)
        {
            if(!this.genres.Contains(genreToRemove))
            {
                // If the genre isn't already attributed to this game, fail
                return false;
            }
            else
            {
                // Otherwise, remove the genre from the array
                for(int i = 0; i < this.genres.Length; i++)
                {
                    if(this.genres[i] != null && this.genres[i].Equals(genreToRemove))
                    {
                        this.genres[i] = null;
                        numGenres--;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Returns whether or not a genre exists within the genre list
        /// </summary>
        /// <param name="inGenre"></param>
        /// <returns></returns>
        public bool HasGenre(string inGenre)
        {
            foreach(string genre in genres)
            {
                if(genre != null && genre.Equals(inGenre)){
                    return true;
                }
            }
            return false;
        }
    }
}
