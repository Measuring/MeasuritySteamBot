using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MeasuritySteamBot.Attributes;

namespace MeasuritySteamBot.Plugins
{
    internal class PluginAssembly
    {
        public PluginAssembly(AppDomain domain, string dll)
        {
            Assembly = domain.Load(dll);

            var type = Assembly.GetType(string.Format("{0}.{1}", Assembly.GetName().Name, "Plugin"));
            Plugin =
                (BasePlugin)
                    Activator.CreateInstance(type);

            Categories =
                Assembly.GetTypes()
                    .Where(t => t.Namespace.Split('.').Last() == "Categories")
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