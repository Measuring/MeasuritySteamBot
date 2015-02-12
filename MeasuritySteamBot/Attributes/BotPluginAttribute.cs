using System;

namespace MeasuritySteamBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BotPluginAttribute : Attribute
    {
        private string _name;

        public BotPluginAttribute(string name, string description, string author)
        {
            Name = name;
            Description = description;
            Author = author;
            Version = new Version(1, 0);
        }

        public BotPluginAttribute(string name, string description, string author, Version version)
        {
            Name = name;
            Description = description;
            Author = author;
            Version = version;
        }

        /// <summary>
        ///     Description of the plugin. Newlines are not allowed and automatically replaced.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value.Replace("\r", "").Replace("\n", ""); }
        }

        public string Description { get; set; }
        public Version Version { get; set; }
        public string Author { get; set; }
    }
}