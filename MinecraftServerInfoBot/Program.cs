using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Webhook;

namespace MinecraftServerInfoBot
{
    class Program
    {
        static void Main(string[] args)
        {
            // Declaración de variables de programa
            var logFileName = "../Server/logs/latest.log";
            var previousLogFileName = "auxLog.log";
            var previouslyCheckedDataFileName = "alreadyChecked.log";
            var previousLogFileWasCreated = false;
            
            // Inicializacion de estructuras de datos
            List<string> currentLogFileLines = new List<string>();
            List<string> previousLogFileLines = new List<string>();
            Dictionary<string, bool> unanouncedPlayersDict = new Dictionary<string, bool>();
            Dictionary<string, bool> currentRegisteredPlayers = new Dictionary<string, bool>();
            Dictionary<string, bool> previousRegisteredPlayers = new Dictionary<string, bool>();

            // Creacion de fichero auxiliar para los jugadores
			// que siguen online y para las líneas de log que ya se han leído
            CreateFile(previouslyCheckedDataFileName);
            previousLogFileWasCreated = CreateFile(previousLogFileName);

            // Obtención de datos de los ficheros
            currentLogFileLines = GetFileContent(logFileName);
            previousLogFileLines = GetFileContent(previousLogFileName);
            currentLogFileLines = CleanLogData(GetFileContent(previouslyCheckedDataFileName), currentLogFileLines);
			
			// Las líneas de log que se procesan en este programa se almacenan
			// en este fichero para no volverlas a procesar.
            AppendLinesToFile(previouslyCheckedDataFileName, currentLogFileLines);

			// Si no hay novedades en el server return
            if (currentLogFileLines.Count == 0) return;

			// Obtención de los jugadores que estaban online y
			// los que se acaban de conectar
            previousRegisteredPlayers = FillPlayersDictionary(previousRegisteredPlayers, previousLogFileLines, previousLogFileWasCreated);
            currentRegisteredPlayers = FillPlayersDictionary(currentRegisteredPlayers, currentLogFileLines);

			// Los jugadores que ya estaban online no se tienen en cuenta y se filtran
            foreach (var item in currentRegisteredPlayers)
            {
                bool hasKey = previousRegisteredPlayers.ContainsKey(item.Key);
                if (hasKey) continue;

                unanouncedPlayersDict.Add(item.Key, item.Value);
            }
			
			// Los nuevos jugadores se almacenan para
			// tenerlos en cuenta en la próxima ejecución del programa
            AppendDictionaryLinesToFile(previousLogFileName, unanouncedPlayersDict);

            if (unanouncedPlayersDict.Count == 0) return;

            // Postear resultados a discord
            Console.WriteLine("Mandando mensaje a discord...");
            string postMessage = CreateNotificationMessage(unanouncedPlayersDict);
            var webhook = new DiscordWebhookClient("https://discord.com/api/webhooks/1115580657544986656/jUqw7ch9YyUY5wwR9esQf1r8NQ5GcwTqmVfvJhKYMF44JYeBiF5RnwAm5N15NJK4M1GV");
            Task<ulong> sendMessageTask = webhook.SendMessageAsync(postMessage);

            sendMessageTask.Wait();

            Console.WriteLine("Mensaje enviado...");
            Console.WriteLine(postMessage);
        }

        public static bool CreateFile(string auxFileName)
        {
            var path = auxFileName;
            var isFileCreated = false;

            if (!File.Exists(path))
            {
                using (var fileStream = File.Create(auxFileName))
                {
                    isFileCreated = true;
                }
            }

            return isFileCreated;
        }

        public static void AppendLinesToFile(string fileName, List<string> lines)
        {
            using (StreamWriter w = File.AppendText(fileName))
            {
                foreach (var item in lines)
                {
                    w.WriteLine(item);
                }
            }
        }

        public static void AppendDictionaryLinesToFile(string fileName, Dictionary<string, bool> lines)
        {
            using (StreamWriter w = File.AppendText(fileName))
            {
                foreach (var item in lines)
                {
                    w.WriteLine(item.Key);
                }
            }
        }

        public static List<string> GetFileContent(string logFileName)
        {
            List<string> logFileLines = new List<string>();

            using (var fileStream = File.OpenRead(logFileName))
            using (var streamReader = new StreamReader(fileStream))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    logFileLines.Add(line);
                }
            }

            return logFileLines;
        }

        public static string GetJoinedPlayerName(string playerJoinedLog)
        {
            int pFrom = playerJoinedLog.IndexOf("INFO]:") + "INFO]:".Length;
            int pTo = playerJoinedLog.LastIndexOf("joined");

            var result = playerJoinedLog.Substring(pFrom, pTo - pFrom);

            result = result.Replace(" ", "");

            return result;
        }

        public static string GetLeftPlayerName(string playerJoinedLog)
        {
            int pFrom = playerJoinedLog.IndexOf("INFO]:") + "INFO]:".Length;
            int pTo = playerJoinedLog.LastIndexOf("left");

            var result = playerJoinedLog.Substring(pFrom, pTo - pFrom);

            result = result.Replace(" ", "");

            return result;
        }

        public static Dictionary<string, bool> FillPlayersDictionary(Dictionary<string, bool> joinedPlayers,
            List<string> loggedPlayers, bool isRawData = true)
        {
            foreach (var item in loggedPlayers)
            {
                if (item.Contains("joined") && isRawData)
                {
                    string previousJoinedPlayer = GetJoinedPlayerName(item);
                    if (!joinedPlayers.ContainsKey(previousJoinedPlayer))
                    {
                        joinedPlayers.Add(previousJoinedPlayer, true);
                    }
                    else if (joinedPlayers[previousJoinedPlayer] == false)
                    {
                        joinedPlayers[previousJoinedPlayer] = true;
                    }
                } 
                else if (item.Contains("left"))
                {
                    string previousJoinedPlayer = GetLeftPlayerName(item);
                    if (!joinedPlayers.ContainsKey(previousJoinedPlayer))
                    {
                        joinedPlayers.Add(previousJoinedPlayer, false);
                    } 
                    else if(joinedPlayers[previousJoinedPlayer] == true)
                    {
                        joinedPlayers[previousJoinedPlayer] = false;
                    }
                }
                //else if (!isRawData)
                //{
                //    string previousJoinedPlayer = item;
                //    if (!joinedPlayers.ContainsKey(previousJoinedPlayer))
                //    {
                //        joinedPlayers.Add(previousJoinedPlayer, previousJoinedPlayer);
                //    }
                //}
            }

            return joinedPlayers;
        }

        public static void ClearFile(string fileName)
        {
            File.WriteAllText(fileName, string.Empty);
        }

        public static List<string> CleanLogData(List<string> previousData, List<string> newData)
        {
            List<string> cleanList = new List<string>();

            foreach (var item in newData)
            {
                if (previousData.Contains(item)) continue;

                cleanList.Add(item);
            }

            return cleanList;
        }

        public static string CreateNotificationMessage(Dictionary<string, bool> players)
        {
            var message = new StringBuilder();
            message.AppendLine("\t----- ¡Atenção chavalada! -----");
            message.AppendLine("");
            message.AppendLine("\tPeña conectada del server: ");
            foreach (var item in players)
            {
                if(item.Value)
                message.AppendLine($"\t   {item.Key}");
            }
            message.AppendLine("");
            message.AppendLine("\tTraidores que se han desconectado: ");
            foreach (var item in players)
            {
                if (!item.Value)
                    message.AppendLine($"\t   {item.Key}");
            }

            return message.ToString();
        }
    }
}
