using FiteEngine.Utilities;
using FiteEngine.Utilities.SerializationUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using static FiteEngine.Utilities.WinLossRecord;

namespace FiteEngine
{
    public class SerializationSurrogate
    {
        public static string dataFolderName = "/Data";
        public static string playerDatafileName = "/FiteEnginePlayerData";
        public static string gameDatafileName = "/FiteEngineGameData";
        public static string genreDatafileName = "/FiteEngineGenreData";
        public static string fileExtension = ".sav";

        /// <summary>
        /// Handles saving a FiteEngine's current state
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns>If the file was successfully saved</returns>
        public static bool Save(Dictionary<string,Player> playerDict, Dictionary<string,Game> gameDict, List<string> genreList)
        {
            BinaryFormatter formatter = GetBinaryFormatter();
            string cwd = Directory.GetCurrentDirectory();

            if(!Directory.Exists(cwd + dataFolderName))
            {
                Directory.CreateDirectory(cwd + dataFolderName);
            }

            // Player data
            FileStream fs = File.Create(cwd + dataFolderName + playerDatafileName + fileExtension);
            formatter.Serialize(fs, playerDict);
            fs.Close();

            // Game data
            fs = File.Create(cwd + dataFolderName + gameDatafileName + fileExtension);
            formatter.Serialize(fs, gameDict);
            fs.Close();

            // Genre data
            fs = File.Create(cwd + dataFolderName + genreDatafileName + fileExtension);
            formatter.Serialize(fs, genreList);
            fs.Close();

            return true;
        }

        /// <summary>
        /// Handles loading a FiteEngine's most recent saved state
        /// </summary>
        /// <param name="saveFile"></param>
        /// <returns></returns>
        public static object[] Load()
        {
            BinaryFormatter formatter = GetBinaryFormatter();
            string cwd = Directory.GetCurrentDirectory();
            object[] ret = new object[3] { null, null, null };

            // Player data
            FileStream fs = File.Open(cwd + dataFolderName + playerDatafileName + fileExtension, FileMode.Open);
            try
            {
                ret[0] = formatter.Deserialize(fs);
                fs.Close();
            }
            catch
            {
                Console.WriteLine("Failed to load player data. Returning nothing.");
                fs.Close();
                return null;
            }

            // Game data
            fs = File.Open(cwd + dataFolderName + gameDatafileName + fileExtension, FileMode.Open);
            try
            {
                ret[1] = formatter.Deserialize(fs);
                fs.Close();
            }
            catch
            {
                Console.WriteLine("Failed to load game data. Returning nothing.");
                fs.Close();
                //return null;
            }

            // Genre data data
            fs = File.Open(cwd + dataFolderName + genreDatafileName + fileExtension, FileMode.Open);
            try
            {
                ret[2] = formatter.Deserialize(fs);
                fs.Close();
            }
            catch
            {
                Console.WriteLine("Failed to load genre data. Returning nothing.");
                fs.Close();
                return null;
            }

            
            return ret;
        }

        /// <summary>
        /// Run to check that we have all of the data files that we need to run. If we miss any, overwrite all.
        /// </summary>
        /// <returns></returns>
        public static bool allDataFilesExist()
        {
            string cwd = Directory.GetCurrentDirectory();

            return File.Exists(cwd + dataFolderName + playerDatafileName + fileExtension) &&
                   File.Exists(cwd + dataFolderName + gameDatafileName + fileExtension) &&
                   File.Exists(cwd + dataFolderName + genreDatafileName + fileExtension);
        }


        /// <summary>
        /// Constructs a new instance of a binary formatter
        /// </summary>
        /// <returns></returns>
        public static BinaryFormatter GetBinaryFormatter()
        {
            BinaryFormatter formatter= new BinaryFormatter();
            SurrogateSelector selector = new SurrogateSelector();

            PlayerDictionarySerializationSurrogate playerDictSurrogate = new PlayerDictionarySerializationSurrogate();
            GameDictSerializationSurrogate gameDictSerializationSurrogate= new GameDictSerializationSurrogate();
            GameRankingDictSerializationSurrogate gameRankingDictSerializationSurrogate = new GameRankingDictSerializationSurrogate();
            WinLossRecordDictSerializationSurrogate winLossRecordDictSerializationSurrogate = new WinLossRecordDictSerializationSurrogate();
            WinLossObjectDictSerializationSurrogate winLossObjectDictSerializationSurrogate = new WinLossObjectDictSerializationSurrogate();
            StringListSerializationSurrogate stringListSerializationSurrogate = new StringListSerializationSurrogate();

            // All of our various custom object dictionaries...
            selector.AddSurrogate(typeof(Dictionary<string, Player>), new StreamingContext(StreamingContextStates.All), playerDictSurrogate);
            selector.AddSurrogate(typeof(Dictionary<string, Game>), new StreamingContext(StreamingContextStates.All), gameDictSerializationSurrogate);
            selector.AddSurrogate(typeof(Dictionary<string, GameRanking>), new StreamingContext(StreamingContextStates.All), gameRankingDictSerializationSurrogate);
            selector.AddSurrogate(typeof(Dictionary<string, WinLossRecord>), new StreamingContext(StreamingContextStates.All), winLossRecordDictSerializationSurrogate);
            selector.AddSurrogate(typeof(Dictionary<string, WinLossObject>), new StreamingContext(StreamingContextStates.All), winLossObjectDictSerializationSurrogate);
            selector.AddSurrogate(typeof(List<string>), new StreamingContext(StreamingContextStates.All), stringListSerializationSurrogate);


            formatter.SurrogateSelector = selector;


            return formatter;
        }
    }
}
