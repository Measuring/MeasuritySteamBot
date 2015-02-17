using System;
using MeasuritySteamBot.Attributes;
using MeasuritySteamBot.Plugins;

namespace Minecraft
{
    [BotPlugin("Minecraft", "A plugin for managing your Minecraft server through Steam.", "Measurity")]
    public class Plugin : BasePlugin<Plugin, Settings>
    {
        // Use this method for initializing your plugin. This can be: reading out system data or retrieving something from the internet.
        // Note that this method isn't async and will block the plugin loader from exiting.
        public override void Initialize()
        {
            base.Initialize();
        }

        // Stop processes and clean up unmanaged resources and bloat from users PC if applicable.
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}