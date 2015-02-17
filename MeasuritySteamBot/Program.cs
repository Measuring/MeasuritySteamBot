using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using MeasuritySteamBot.Steam;

namespace MeasuritySteamBot
{
    internal class Program
    {
        private static Bot _bot;
        private static string _username;
        private static string _password;

        private static void Main(string[] args)
        {
            LoadConfig();
            _bot = new Bot(_username, _password);

            // Listen for commands.
            Task.Factory.StartNew(CaptureInput, TaskCreationOptions.LongRunning).ContinueWith((t) =>
            {
                Console.WriteLine("\r\nPress a key to continue..");
                Console.ReadKey(true);
            });

            _bot.LoadPlugins();
            _bot.Connect();
            _bot.Logon();
        }

        /// <summary>
        ///     Creates or reads the config file for username and password.
        /// </summary>
        private static void LoadConfig()
        {
            XDocument doc;
            if (!File.Exists("botsettings.xml"))
            {
                using (var stream = new FileStream("botsettings.xml", FileMode.CreateNew))
                {
                    doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
                        new XElement("Settings",
                            new XElement("Login", new XElement("Username", ""),
                                new XElement("Password", ""))));
                    doc.Save(stream);
                }
                return;
            }

            // Read settings from file.
            doc = XDocument.Parse(File.ReadAllText("botsettings.xml"));
            var elem = doc.Root.Element("Login");
            _username = elem.Element("Username").Value;
            _password = elem.Element("Password").Value;
        }

        private static void CaptureInput()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (new[] { "exit", "stop", "quit", "close" }.Contains(input, StringComparer.OrdinalIgnoreCase))
                {
                    _bot.Dispose();
                    break;
                }

                _bot.Execute(input);
            }
        }
    }
}