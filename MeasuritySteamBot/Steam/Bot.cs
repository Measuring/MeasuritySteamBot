﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeasuritySteamBot.Plugins;
using SteamKit2;

namespace MeasuritySteamBot.Steam
{
    public class Bot : IDisposable
    {
        private bool _firstHeader = true;

        public Bot(string username, string password)
        {
            Username = username;
            Password = password;
            Plugins = new PluginManager("Plugins");
        }

        public string Username { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected string Password { get; set; }

        internal PluginManager Plugins { get; set; }
        public SteamUser User { get; set; }
        public SteamClient Client { get; set; }
        public SteamFriends Friends { get; set; }
        public CallbackManager Manager { get; set; }

        public bool IsConnected
        {
            get { return Client != null && Client.IsConnected; }
        }

        /// <summary>
        ///     Disposes all plugins by calling <seealso cref="BasePlugin.Dispose" /> and then disposes the <see cref="Bot" />'s
        ///     connection.
        /// </summary>
        public void Dispose()
        {
            // Unload plugins.
            Plugins.Dispose();

            // Unload bot.
            if (Manager != null)
                Manager = null;
            if (User != null)
                User.LogOff();
            if (Client != null)
                Client.Disconnect();
        }

        /// <summary>
        ///     Connects the bot to the Steam client.
        /// </summary>
        /// <returns>True if connection succeeded.</returns>
        public bool Connect()
        {
            if (IsConnected)
            {
                Console.WriteLine("Client is already connected.");
                return true;
            }

            WriteHeader("Connecting to Steam");
            if (Client == null)
            {
                Client = new SteamClient();
                Manager = new CallbackManager(Client);
                User = Client.GetHandler<SteamUser>();
                Friends = Client.GetHandler<SteamFriends>();
            }

            Client.Connect();

            // Wait for connection result.
            var isConnecting = true;
            while (isConnecting)
            {
                var callback = Client.WaitForCallback(true);

                callback.Handle<SteamClient.ConnectedCallback>(c =>
                {
                    if (c.Result != EResult.OK)
                    {
                        Console.WriteLine("Unable to connect to Steam: {0}", c.Result);
                        isConnecting = false;
                        return;
                    }

                    Console.WriteLine("Connected to Steam! Logging in: {0}", User);
                    isConnecting = false;
                });
            }

            return true;
        }

        /// <summary>
        ///     Called after <see cref="Connect" /> to login with the credentials specified in the constructor.
        /// </summary>
        /// <returns></returns>
        public bool Logon()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                Console.WriteLine("Username or password was not given.");
                return false;
            }

            if (!IsConnected)
            {
                Console.WriteLine("Client is not yet connected.");
                return false;
            }

            if (User.SteamID != null && User.SteamID.IsValid)
            {
                Console.WriteLine("User has already been logged on.");
                return true;
            }

            WriteHeader("Logging into Steam");

            // Try to login user.
            User.LogOn(new SteamUser.LogOnDetails
            {
                Username = Username,
                Password = Password
            });

            var isLoggingIn = true;
            var loginSuccess = false;
            while (isLoggingIn)
            {
                var callback = Client.WaitForCallback(true);

                callback.Handle<SteamUser.LoggedOnCallback>(logOnCallback =>
                {
                    if (logOnCallback.Result == EResult.OK)
                    {
                        isLoggingIn = false;
                        loginSuccess = true;
                        Console.WriteLine("User {0} has been logged in!", User);

                        // Handle online status.
                        new Callback<SteamUser.AccountInfoCallback>(
                            c => { Friends.SetPersonaState(EPersonaState.Online); }, Manager);

                        // Handle chat (commands).
                        new Callback<SteamFriends.FriendMsgCallback>(c =>
                        {
                            if (c.EntryType == EChatEntryType.ChatMsg)
                                Execute(c.Message, c.Sender);
                        }, Manager);

                        // Handle callbacks.
                        Task.Factory.StartNew(() =>
                        {
                            while (IsConnected)
                            {
                                Manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                            }
                        }, TaskCreationOptions.LongRunning);

                        return;
                    }

                    isLoggingIn = false;
                    loginSuccess = false;
                });
            }

            return loginSuccess;
        }

        public void LoadPlugins()
        {
            WriteHeader("Loading plugins");

            // Load all plugin assemblies.
            Plugins.LoadPlugins(this);
        }

        public void Execute(string input, ulong steamId = 0)
        {
            // Split up input.
            var args = ParseCommand(input);

            // Check input correctness.
            if (args.Length <= 0)
                return;

            if (steamId == 0 || input.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                if (((string)args[0]).StartsWith("help", StringComparison.OrdinalIgnoreCase))
                {
                    Plugins.Help(args, this, steamId);
                }
                else
                {
                    // Execute command from user.
                    Plugins.Execute(args, steamId);
                }
            }
        }

        /// <summary>
        ///     Parses the input into processible parts.
        /// </summary>
        /// <param name="command">Command to parse from the user.</param>
        /// <returns></returns>
        protected object[] ParseCommand(string command)
        {
            return Regex.Matches(command.TrimStart('/'), @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value)
                .Select<string, object>(v =>
                {
                    var sVal = v.Trim('\"');
                    float fVal;
                    int iVal;

                    if (float.TryParse(sVal, out fVal))
                        return fVal;
                    if (int.TryParse(sVal, out iVal))
                        return iVal;
                    return sVal;
                }).ToArray();
        }

        /// <summary>
        ///     Sends a message to the requesting user.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="steamId">SteamId to send message to. If null, sends message to <see cref="Console" />.</param>
        protected internal void SendMessage(string message, SteamID steamId = null)
        {
            if (steamId == 0)
                Console.WriteLine(message);
            else
                Friends.SendChatMessage(steamId, EChatEntryType.ChatMsg, message);
        }

        public void WriteHeader(string message)
        {
            const int padLength = 60;
            const char fillChar = '=';
            const char padChar = ' ';

            // Build text.
            var textBuilder = new StringBuilder();

            if (!_firstHeader)
                textBuilder.AppendLine();

            // Build text header.
            textBuilder.AppendLine(new string(fillChar, padLength));

            // Build text content.
            textBuilder.Append(fillChar);
            textBuilder.Append(new string(padChar, (int)Math.Floor(padLength / 2.0 - message.Length / 2.0 + 0.5)));
            textBuilder.Append(message);
            textBuilder.Append(new string(padChar, (int)Math.Floor(padLength / 2.0 - message.Length / 2.0 - 2)));
            textBuilder.AppendLine(fillChar.ToString());

            // Build text footer.
            textBuilder.Append(new string(fillChar, padLength));

            // Write text.
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(textBuilder.ToString());
            Console.ForegroundColor = oldColor;

            _firstHeader = false;
        }
    }
}