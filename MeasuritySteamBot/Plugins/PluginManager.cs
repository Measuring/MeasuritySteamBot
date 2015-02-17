using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MeasuritySteamBot.Steam;
using SteamKit2;

namespace MeasuritySteamBot.Plugins
{
    [Serializable]
    internal class PluginManager : MarshalByRefObject, IDisposable
    {
        public PluginManager(Bot bot, string pluginsDir = "Plugins", string dataDir = "Data")
        {
            if (!Directory.Exists(pluginsDir))
                Directory.CreateDirectory(Path.Combine(pluginsDir, dataDir));

            Bot = bot;
            PluginsDir = pluginsDir;
            DataDir = dataDir;
            Plugins = new List<PluginAssembly>();
        }

        /// <summary>
        ///     PluginsDir to search for plugins.
        /// </summary>
        public string PluginsDir { get; protected set; }

        /// <summary>
        ///     Relative path that will be appended to the <see cref="PluginsDir" /> property.
        /// </summary>
        public string DataDir { get; protected set; }

        /// <summary>
        ///     <see cref="Bot" /> that is requesting plugins.
        /// </summary>
        public Bot Bot { get; protected internal set; }

        public List<PluginAssembly> Plugins { get; set; }
        public AppDomain Domain { get; protected set; }

        public void Dispose()
        {
            // Cleanup plugin references.
            foreach (var plugin in Plugins)
            {
                // Cleanup category specific resources.
                foreach (var category in plugin.Categories)
                {
                    category.Value.CategoryInstance.Dispose();
                }

                // Plugin author's code.
                plugin.Plugin.Dispose();
            }
            Plugins.Clear();

            // Unload plugin AppDomain.
            if (Domain != null && Domain != AppDomain.CurrentDomain)
            {
                Domain.AssemblyResolve -= AssemblyResolve;
                Domain.ReflectionOnlyAssemblyResolve -= AssemblyResolve;
                AppDomain.Unload(Domain);
                Domain = null;
            }
        }

        protected Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dir = Path.Combine(curDir, PluginsDir, Path.ChangeExtension(args.Name, ".dll"));
            if (!File.Exists(dir))
                dir = Path.Combine(curDir, args.Name);

            return Assembly.LoadFrom(dir) ?? args.RequestingAssembly;
        }

        /// <summary>
        ///     Loads all the plugins into the <see cref="PluginManager" />.
        /// </summary>
        public void LoadPlugins()
        {
            // Unload domain.
            Dispose();

            // Setup domain.
            var setup = new AppDomainSetup();
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

            Domain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"), AppDomain.CurrentDomain.Evidence, setup);

            // Register plugins.
            Domain.DoCallBack(() =>
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolve;
                foreach (var plugin in new DirectoryInfo(PluginsDir).GetFiles("*.dll"))
                {
                    var bytes = new byte[0];
                    using (var stream = new FileStream(plugin.FullName, FileMode.Open, FileAccess.Read))
                    {
                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                    }
                    var asm = AppDomain.CurrentDomain.Load(bytes);
                    LoadPlugin(Bot, asm);
                }
            });
        }

        /// <summary>
        ///     Loads the plugin and caches the required reflection data.
        /// </summary>
        /// <param name="bot">Bot that handles the execution of the <see cref="BasePlugin" />.</param>
        /// <param name="pluginAssembly">Assembly of the <see cref="BasePlugin" />.</param>
        public void LoadPlugin(Bot bot, Assembly pluginAssembly)
        {
            // Cache plugin metadata through reflection.
            var plugin = new PluginAssembly(pluginAssembly);

            // Global per plugin initialze.
            plugin.Plugin.Categories = plugin.Categories;
            plugin.Plugin.Bot = bot;
            plugin.Plugin.PluginsDir = PluginsDir;
            plugin.Plugin.DataDir = Path.Combine(PluginsDir, DataDir, plugin.Assembly.GetName().Name);
            plugin.Plugin.Setup(plugin);

            // Plugin specific initialize.
            plugin.Plugin.Initialize();

            Plugins.Add(plugin);

            Bot.SendMessage(plugin.Plugin.InitializedMessage);
        }

        /// <summary>
        ///     Executes a command on all connected plugins.
        /// </summary>
        /// <param name="args">Command data from user input.</param>
        /// <param name="steamId">SteamId of the user. 0 for console.</param>
        public bool Execute(object[] args, ulong steamId = 0)
        {
            foreach (var plugin in Plugins)
            {
                plugin.Plugin.Sender = steamId;
                if (plugin.Plugin.Execute(args))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Sends a help message for the plugins or a plugin's command.
        /// </summary>
        /// <param name="args">Arguments to get help for.</param>
        /// <param name="bot"><see cref="Bot" /> were help is requested for.</param>
        /// <param name="steamId"><see cref="SteamID" /> of the user requesting help.</param>
        /// <returns></returns>
        public bool Help(object[] args, Bot bot, ulong steamId = 0)
        {
            if (args.Length == 1)
            {
                // Show all available categories.
                var pluginHelpBuilder = new StringBuilder();
                pluginHelpBuilder.AppendLine("Available plugins:");
                foreach (var cat in Plugins.SelectMany(p => p.Categories).OrderBy(k => k.Key))
                {
                    pluginHelpBuilder.Append(cat.Key);
                    if (cat.Value.Description != null)
                    {
                        pluginHelpBuilder.Append(" - ");
                        pluginHelpBuilder.AppendLine(cat.Value.Description);
                    }
                }

                bot.SendMessage(pluginHelpBuilder.ToString(), steamId);
            }
            else
            {
                // Get help for command.
                foreach (var plugin in Plugins)
                {
                    plugin.Plugin.Sender = steamId;
                    if (plugin.Plugin.Help(args))
                        return true;
                }
            }
            return false;
        }
    }
}