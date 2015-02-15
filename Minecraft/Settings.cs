using MeasuritySteamBot.Plugins;

namespace Minecraft
{
    /// <summary>
    ///     Every plugin has a data directory where this settings file will be stored.
    /// </summary>
    public class Settings : BaseSettings
    {
        public Settings()
        {
            ServerDirectory = @"D:\Games\Minecraft\Servers\Monster 1.1.2\";
            ExeName = "ServerStart.bat";
        }

        public string ServerDirectory { get; set; }
        public string ExeName { get; set; }
    }
}