using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using MeasuritySteamBot.Attributes;
using MeasuritySteamBot.Steam;
using SteamKit2;

namespace MeasuritySteamBot.Plugins
{
    public abstract class BasePlugin<TPlugin, TSettings> : BasePlugin
        where TPlugin : BasePlugin, new()
        where TSettings : BaseSettings, new()
    {
        protected BasePlugin()
        {
            Instance = this;
        }

        /// <summary>
        ///     Current settings of the <see cref="BasePlugin" />.
        /// </summary>
        public new TSettings Settings
        {
            get { return (TSettings)base.Settings; }
        }

        /// <summary>
        ///     Current instance of the <see cref="BasePlugin" />.
        /// </summary>
        public new static BasePlugin<TPlugin, TSettings> Instance { get; internal set; }
    }

    /// <summary>
    ///     Generic variant of the BasePlugin.
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class BasePlugin<TPlugin> : BasePlugin
        where TPlugin : class, new()
    {
        protected BasePlugin()
        {
            Instance = this;
        }

        /// <summary>
        ///     Current instance of the <see cref="BasePlugin" />.
        /// </summary>
        public new static BasePlugin<TPlugin> Instance { get; internal set; }
    }

    /// <summary>
    ///     Non-generic variant of the BasePlugin. Generic variant is prefered.
    /// </summary>
    public abstract class BasePlugin
    {
        protected BasePlugin()
        {
            Instance = this;
            Categories =
                GetType().Module.GetTypes()
                    .Where(
                        t =>
                            t.IsClass && t.IsPublic && !t.IsAutoClass &&
                            t.Namespace.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                                .Last()
                                .StartsWith("Categories", StringComparison.OrdinalIgnoreCase))
                    .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<BotDisplayAttribute>() })
                    .Select(
                        t =>
                            new KeyValuePair<string, PluginCategory>(
                                t.Attribute != null ? t.Attribute.Name : t.Type.Name.ToLowerInvariant(),
                                new PluginCategory(t.Type)))
                    .ToDictionary(k => k.Key, k => k.Value);
        }

        internal Dictionary<string, PluginCategory> Categories { get; set; }

        /// <summary>
        ///     Current instance of the plugin.
        /// </summary>
        public static BasePlugin Instance { get; set; }

        /// <summary>
        ///     Current settings of the <see cref="BasePlugin" />.
        /// </summary>
        public BaseSettings Settings { get; internal set; }

        public Bot Bot { get; protected internal set; }

        /// <summary>
        ///     Returns the <see cref="BasePlugin" />'s information specified by the developer.
        /// </summary>
        internal string InitializedMessage
        {
            get
            {
                var attr = GetType().GetCustomAttribute<BotPluginAttribute>();
                if (attr == null)
                    throw new Exception("Plugin information was not given by plugin.");
                return string.Format("[{0}{2}] {1}{3}", attr.Name, attr.Description,
                    attr.Version != null ? " v" + attr.Version : "",
                    !string.IsNullOrWhiteSpace(attr.Author) ? " by " + attr.Author : "");
            }
        }

        /// <summary>
        ///     Sender of the latest command. 0 for console operator.
        /// </summary>
        public ulong Sender { get; internal set; }

        /// <summary>
        ///     Directory that contains per plugin specific data.
        /// </summary>
        public string DataDir { get; internal set; }

        /// <summary>
        ///     Directory where all the <see cref="BasePlugin" />s are located.
        /// </summary>
        public string PluginsDir { get; internal set; }

        /// <summary>
        ///     Used by the <see cref="PluginManager" /> to initialize the <see cref="BasePlugin" />.
        ///     The <see cref="Initialize" /> method is for the <see cref="BasePlugin" /> author.
        /// </summary>
        internal void Setup(PluginAssembly plugin)
        {
            // Setup plugin data structure.
            Directory.CreateDirectory(DataDir);

            // Setup Settings.xml
            var asmName = plugin.Assembly.GetName();
            var settingsType = plugin.Assembly.GetType(string.Format("{0}.{1}", asmName.Name, "Settings"));
            var settingsFile = Path.Combine(DataDir, "Settings.xml");

            if (settingsType != null)
            {
                var serializer = new XmlSerializer(settingsType);
                if (!File.Exists(settingsFile))
                {
                    using (var stream = new FileStream(settingsFile, FileMode.CreateNew))
                        serializer.Serialize(stream, Activator.CreateInstance(settingsType));
                }

                using (var stream = new FileStream(settingsFile, FileMode.Open))
                    Settings = (BaseSettings)serializer.Deserialize(stream);
            }
        }

        /// <summary>
        ///     Method for initializing the plugin. Do not use the constructor for this.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        ///     Method for cleaning up plugin when <see cref="Bot" /> closes connection.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        ///     Executes the function if a match is found with the cached reflection data of this <see cref="BasePlugin" />.
        /// </summary>
        /// <param name="args"></param>
        public bool Execute(object[] args)
        {
            var category = Categories.FirstOrDefault(c => c.Key.StartsWith((string)args[0]));
            if (category.Value == null)
            {
                SendMessage(string.Format("Unknown category: {0}", (string)args[0]));
                return false;
            }
            ;
            var command = category.Value.Commands.FirstOrDefault(c => c.Key.StartsWith((string)args[1]));
            if (command.Value == null)
            {
                SendMessage(string.Format("Unknown command: {0} on category: {1}", (string)args[1], (string)args[0]));
                return false;
            }

            // Strip away category and command args.
            var parms = new object[args.Length - 2];
            Array.Copy(args, 2, parms, 0, parms.Length);

            // Invoke method with arguments (excluding category and command) or give usage.
            if (!command.Value.Invoke(parms))
                SendMessage(command.Value.Help());

            return true;
        }

        public bool Help(object[] args)
        {
            // Find suitable category.
            var category = Categories.FirstOrDefault(c => c.Key.StartsWith((string)args[1]));
            if (category.Value == null)
            {
                SendMessage(string.Format("Unknown category: {0}", (string)args[1]));
                return false;
            }

            // Display all commands of this category.
            if (args.Length <= 2)
            {
                var helpBuilder = new StringBuilder("Available commands:\r\n");
                foreach (var cmd in category.Value.Commands.OrderBy(c => c.Key))
                {
                    helpBuilder.Append(cmd.Key);
                    if (!string.IsNullOrWhiteSpace(cmd.Value.Description))
                    {
                        helpBuilder.Append(" - ");
                        helpBuilder.AppendLine(cmd.Value.Description);
                    }
                }
                SendMessage(helpBuilder.ToString());
                return true;
            }

            // Find suitable command.
            var command = category.Value.Commands.FirstOrDefault(c => c.Key.StartsWith((string)args[2]));
            if (command.Value == null)
            {
                SendMessage(string.Format("Unknown command: {0} on category: {1}", (string)args[2], (string)args[1]));
                return false;
            }

            // Strip away help, category and command args.
            var parms = new object[args.Length - 3];
            Array.Copy(args, 3, parms, 0, parms.Length);

            // Get method usage from plugin.
            SendMessage(command.Value.Help());

            return true;
        }

        /// <summary>
        ///     Sends a message to the user that wrote the command. Either the <see cref="Console" /> operator, or Steam user.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message, ulong? steamId = null)
        {
            if (!steamId.HasValue)
                steamId = Sender;

            if (steamId == 0)
                Console.WriteLine(message);
            else
                Bot.Friends.SendChatMessage(steamId, EChatEntryType.ChatMsg, message);
        }
    }
}