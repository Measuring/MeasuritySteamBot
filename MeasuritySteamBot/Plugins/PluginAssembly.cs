using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MeasuritySteamBot.Attributes;
using MeasuritySteamBot.Exceptions;

namespace MeasuritySteamBot.Plugins
{
    internal class PluginAssembly
    {
        public PluginAssembly(Assembly assembly)
        {
            Assembly = assembly;

            var type = Assembly.GetExportedTypes().FirstOrDefault(t => typeof(BasePlugin).IsAssignableFrom(t));
            if (type == null)
                throw new InvalidPluginException(Assembly.GetName().Name);
            Plugin =
                (BasePlugin)
                    Activator.CreateInstance(type);

            Categories =
                Assembly.GetExportedTypes()
                    .Where(t => typeof(BaseCategory).IsAssignableFrom(t))
                    .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<BotDisplayAttribute>() })
                    .Select(
                        t =>
                            new KeyValuePair<string, PluginCategory>(
                                t.Attribute != null ? t.Attribute.Name : t.Type.Name.ToLower(),
                                new PluginCategory(t.Type))).ToDictionary(k => k.Key, k => k.Value);
        }

        public BasePlugin Plugin { get; set; }
        public Assembly Assembly { get; set; }
        public Dictionary<string, PluginCategory> Categories { get; set; }
    }
}