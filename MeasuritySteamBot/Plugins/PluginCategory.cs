using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MeasuritySteamBot.Attributes;

namespace MeasuritySteamBot.Plugins
{
    internal class PluginCategory
    {
        public PluginCategory(Type categoryType)
        {
            CategoryType = categoryType;
            CategoryInstance = (BaseCategory)Activator.CreateInstance(CategoryType);

            // Get category name.
            var attrName = CategoryType.GetCustomAttribute<BotDisplayAttribute>();
            Description = attrName != null ? attrName.Description : null;

            // Load all commands.
            Commands =
                categoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod |
                                        BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsVirtual)
                    .Select(m => new { Method = m, NameAttribute = m.GetCustomAttribute<BotDisplayAttribute>() })
                    .Select(
                        m =>
                            new KeyValuePair<string, PluginCommand>(
                                m.NameAttribute != null && m.NameAttribute.Name != null
                                    ? m.NameAttribute.Name
                                    : m.Method.Name.ToLowerInvariant(),
                                new PluginCommand(CategoryInstance, m.Method))).ToDictionary(k => k.Key, k => k.Value);
        }

        protected Type CategoryType { get; set; }
        public BaseCategory CategoryInstance { get; set; }
        public string Description { get; protected set; }
        public Dictionary<string, PluginCommand> Commands { get; set; }
    }
}