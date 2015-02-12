using System;
using System.IO;
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

            _bot.LoadPlugins();
            _bot.Connect();
            _bot.Logon();

            // Capture commands.
            Task.Factory.StartNew(CaptureInput, TaskCreationOptions.LongRunning);
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
                _bot.Execute(Console.ReadLine());
            }
        }
    }
}