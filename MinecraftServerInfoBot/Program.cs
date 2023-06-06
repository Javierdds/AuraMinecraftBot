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
            var logFileName = "latest.log";
            var previousLogFileName = "auxLog.log";
            var previouslyCheckedDataFileName = "alreadyChecked.log";
            var previousLogFileWasCreated = false;

            // Inicializacion de estructuras de datos
            List<string> currentLogFileLines = new List<string>();
            List<string> previousLogFileLines = new List<string>();
            List<string> unanouncedPlayersList = new List<string>();
            Dictionary<string, string> currentRegisteredPlayers = new Dictionary<string, string>();
            Dictionary<string, string> previousRegisteredPlayers = new Dictionary<string, string>();

            // Creacion de fichero auxiliar para los jugadores
			// que siguen online y para las líneas de log que ya se han leído
            CreateFile(previouslyCheckedDataFileName);
            previousLogFileWasCreated = CreateFile(previousLogFileName);

            // Obtención de datos de los ficheros
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

                unanouncedPlayersList.Add(item.Value);
            }
			
			// Los nuevos jugadores se almacenan para
			// tenerlos en cuenta en la próxima ejecución del programa
            AppendLinesToFile(previousLogFileName, unanouncedPlayersList);

            if (unanouncedPlayersList.Count == 0) return;

            // Postear resultados a discord
            Console.WriteLine("Mandando mensaje a discord...");
            string postMessage = CreateNotificationMessage(unanouncedPlayersList);
            var webhook = new DiscordWebhookClient("https://discord.com/api/webhooks/1115580657544986656/jUqw7ch9YyUY5wwR9esQf1r8NQ5GcwTqmVfvJhKYMF44JYeBiF5RnwAm5N15NJK4M1GV");
            Task<ulong> sendMessageTask = webhook.SendMessageAsync(postMessage);

            sendMessageTask.Wait();

            Console.WriteLine("Mensaje enviado...");

            Console.ReadLine();
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

        public static Dictionary<string, string> FillPlayersDictionary(Dictionary<string, string> joinedPlayers,
            List<string> loggedPlayers, bool isRawData = true)
        {
            foreach (var item in loggedPlayers)
            {
                if (item.Contains("joined") && isRawData)
                {
                    string previousJoinedPlayer = GetJoinedPlayerName(item);
                    if (!joinedPlayers.ContainsKey(previousJoinedPlayer))
                    {
                        joinedPlayers.Add(previousJoinedPlayer, previousJoinedPlayer);
                    }
                } 
                else if (!isRawData)
                {
                    string previousJoinedPlayer = item;
                    if (!joinedPlayers.ContainsKey(previousJoinedPlayer))
                    {
                        joinedPlayers.Add(previousJoinedPlayer, previousJoinedPlayer);
                    }
                }
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

        public static string CreateNotificationMessage(List<string> loggedInPlayers)
        {
            var message = new StringBuilder();
            message.AppendLine("----- ESTE ES UN MENSAJE DE PRUEBA -----");
            message.AppendLine("Jugadores conectados: ");
            foreach (var item in loggedInPlayers)
            {
                message.AppendLine($"   {item}");
            }

            return message.ToString();
        }
    }
}
