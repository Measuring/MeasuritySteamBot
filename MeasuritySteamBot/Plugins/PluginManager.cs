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
    internal class PluginManager : IDisposable
    {
        public PluginManager(string folder)
        {
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException(string.Format("Directory {0} not found.", folder));

            SetupDomain();
            Plugins = new List<PluginAssembly>();
            Folder = folder;
        }

        /// <summary>
        ///     Folder to search for plugins.
        /// </summary>
        public string Folder { get; set; }

        public List<PluginAssembly> Plugins { get; set; }
        public AppDomain Domain { get; protected set; }

        public void Dispose()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Plugin.Dispose();
            }
        }

        protected void SetupDomain()
        {
            // TODO: Enable hot plugin reloading through separate AppDomain.
            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };
            //Domain = AppDomain.CreateDomain("SteamBotPluginDomain", AppDomain.CurrentDomain.Evidence, setup);
            Domain = AppDomain.CurrentDomain;
            Domain.AssemblyResolve += AssemblyResolve;
            Domain.ReflectionOnlyAssemblyResolve += AssemblyResolve;
        }

        protected Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var newPath = Path.Combine(dir,
                "Plugins", new AssemblyName(args.Name).Name);

            return File.Exists(newPath) ? Assembly.LoadFrom(newPath) : args.RequestingAssembly;
        }

        /// <summary>
        ///     Loads all the plugins into the <see cref="PluginManager" />.
        /// </summary>
        /// <param name="bot"></param>
        public void LoadPlugins(Bot bot)
        {
            foreach (var plugin in new DirectoryInfo(Folder).GetFiles("*.dll"))
            {
                LoadPlugin(bot, plugin);
            }
        }

        /// <summary>
        ///     Loads the plugin and caches the required reflection data.
        /// </summary>
        /// <param name="bot">Bot that handles the execution of the <see cref="BasePlugin" />.</param>
        /// <param name="pluginDll">Dll file of the <see cref="BasePlugin" />.</param>
        public void LoadPlugin(Bot bot, FileInfo pluginDll)
        {
            // Cache plugin metadata through reflection.
            var plugin = new PluginAssembly(Domain, pluginDll.Name);

            // Initialize plugin.
            plugin.Plugin.Bot = bot;
            plugin.Plugin.Initialize();

            Plugins.Add(plugin);

            Console.WriteLine(plugin.Plugin.InitializedMessage);
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