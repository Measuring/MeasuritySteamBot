using System;
using System.Diagnostics;
using System.IO;
using MeasuritySteamBot.Attributes;

namespace Minecraft.Categories
{
    [BotDisplay("mc", "Moderating Minecraft servers.")]
    [BotAuthorize]
    public class Minecraft
    {
        private Process _minecraftProcess;

        /// <summary>
        ///     Starts the Minecraft server.
        /// </summary>
        [BotDisplay(Description = "Starts the Minecraft server.")]
        public void Start()
        {
            Plugin.Instance.SendMessage("Server starting.. (this will take a while)");
            var path = Path.Combine(Plugin.Instance.Settings.ServerDirectory, Plugin.Instance.Settings.ExeName);
            _minecraftProcess = new Process
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
            _minecraftProcess.EnableRaisingEvents = true;
            _minecraftProcess.ErrorDataReceived += Start_DataReceived;
            _minecraftProcess.OutputDataReceived += Start_DataReceived;

            _minecraftProcess.Start();

            _minecraftProcess.BeginErrorReadLine();
            _minecraftProcess.BeginOutputReadLine();
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

            _minecraftProcess.ErrorDataReceived -= Start_DataReceived;
            _minecraftProcess.OutputDataReceived -= Start_DataReceived;
        }

        /// <summary>
        ///     Executes a command to the Minecraft server's console.
        /// </summary>
        /// <param name="command"></param>
        [BotDisplay("Exec", "Executes a command on the server.")]
        public void Execute([BotDisplay(Description = "Command to execute on the server.")] string command)
        {
            if (_minecraftProcess == null || _minecraftProcess.HasExited)
            {
                Plugin.Instance.SendMessage("No Minecraft server is currently running.");
                return;
            }
            _minecraftProcess.StandardInput.WriteLine(command);
        }
    }
}