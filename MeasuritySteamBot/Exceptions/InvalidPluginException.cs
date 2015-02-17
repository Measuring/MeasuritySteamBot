using System;
using MeasuritySteamBot.Plugins;
using MeasuritySteamBot.Steam;

namespace MeasuritySteamBot.Exceptions
{
    /// <summary>
    ///     Thrown when a <see cref="BasePlugin" /> isn't correctly configured to be run by a <see cref="Bot" />.
    /// </summary>
    public class InvalidPluginException : Exception
    {
        public InvalidPluginException(string pluginName)
        {
            PluginName = pluginName;
        }

        public string PluginName { get; protected set; }
    }
}