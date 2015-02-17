using System;
using System.Diagnostics;
using System.IO;
using MeasuritySteamBot.Attributes;
using MeasuritySteamBot.Plugins;

namespace Minecraft.Categories
{
    [BotDisplay("mc", "Moderating Minecraft servers.")]
    [BotAuthorize]
    public class Minecraft : BaseCategory
    {
        protected internal Process MinecraftProcess;


        /// <summary>
        ///     Starts the Minecraft server.
        /// </summary>
        [BotDisplay(Description = "Starts the Minecraft server.")]
        public void Start()
        {
            Plugin.Instance.SendMessage("Server starting.. (this will take a while)");
            var path = Path.Combine(Plugin.Instance.Settings.ServerDirectory, Plugin.Instance.Settings.ExeName);
            MinecraftProcess = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            MinecraftProcess.EnableRaisingEvents = true;
            MinecraftProcess.ErrorDataReceived += Start_DataReceived;
            MinecraftProcess.OutputDataReceived += Start_DataReceived;

            MinecraftProcess.Start();

            MinecraftProcess.BeginErrorReadLine();
            MinecraftProcess.BeginOutputReadLine();
        }

        /// <summary>
        ///     Console output listening method for capturing when the server has been started.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event data that went with the event.</param>
        private void Start_DataReceived(object sender, DataReceivedEventArgs e)
        {
            // Check if server has started yet.
            if (e.Data.IndexOf("Started up server", StringComparison.OrdinalIgnoreCase) < 0)
                return;
            Plugin.Instance.SendMessage("Server started!");

            MinecraftProcess.ErrorDataReceived -= Start_DataReceived;
            MinecraftProcess.OutputDataReceived -= Start_DataReceived;
        }

        /// <summary>
        ///     Executes a command to the Minecraft server's console.
        /// </summary>
        /// <param name="command"></param>
        [BotDisplay("Exec", "Executes a command on the server.")]
        public void Execute([BotDisplay(Description = "Command to execute on the server.")] string command)
        {
            if (MinecraftProcess == null || MinecraftProcess.HasExited)
            {
                Plugin.Instance.SendMessage("No Minecraft server is currently running.");
                return;
            }
            MinecraftProcess.StandardInput.WriteLine(command);
        }

        public override void Dispose()
        {
            if (MinecraftProcess != null && !MinecraftProcess.HasExited)
            {
                Execute("stop");
            }
        }
    }
}