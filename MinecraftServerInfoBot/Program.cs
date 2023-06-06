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
            // Abrir fichero latest.log en modo lectura
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

            // Creacion de fichero auxiliar
            CreateFile(previouslyCheckedDataFileName);
            previousLogFileWasCreated = CreateFile(previousLogFileName);

            // Obtencion de datos 
            currentLogFileLines = GetFileContent(logFileName);
            previousLogFileLines = GetFileContent(previousLogFileName);

            currentLogFileLines = CleanLogData(GetFileContent(previouslyCheckedDataFileName), currentLogFileLines);
            AppendLinesToFile(previouslyCheckedDataFileName, currentLogFileLines);

            if (currentLogFileLines.Count == 0) return;

            previousRegisteredPlayers = FillPlayersDictionary(previousRegisteredPlayers, previousLogFileLines, previousLogFileWasCreated);
            currentRegisteredPlayers = FillPlayersDictionary(currentRegisteredPlayers, currentLogFileLines);

            foreach (var item in currentRegisteredPlayers)
            {
                bool hasKey = previousRegisteredPlayers.ContainsKey(item.Key);
                if (hasKey) continue;

                unanouncedPlayersList.Add(item.Value);
            }

            AppendLinesToFile(previousLogFileName, unanouncedPlayersList);

            foreach (var item in unanouncedPlayersList)
            {
                Console.WriteLine($"[New player joined]: {item}");
            }

            if (unanouncedPlayersList.Count == 0) return;

            // Posting results
            Console.WriteLine("Sending success to discord...");
            string postMessage = CreateNotificationMessage(unanouncedPlayersList);
            var webhook = new DiscordWebhookClient("https://discord.com/api/webhooks/1115580657544986656/jUqw7ch9YyUY5wwR9esQf1r8NQ5GcwTqmVfvJhKYMF44JYeBiF5RnwAm5N15NJK4M1GV");
            Task<ulong> sendMessageTask = webhook.SendMessageAsync(postMessage);

            sendMessageTask.Wait();

            Console.WriteLine("Message sent...");

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
