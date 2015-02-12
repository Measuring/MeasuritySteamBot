using System;

namespace MeasuritySteamBot.Attributes
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter |
        AttributeTargets.GenericParameter)]
    public class BotDisplayAttribute : Attribute
    {
        private string _name;

        public BotDisplayAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        ///     Name of the class or method. Has no effect on parameters.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value != null ? value.ToLowerInvariant() : null; }
        }

        /// <summary>
        ///     Description of the class, method or parameter.
        /// </summary>
        public string Description { get; set; }
    }
}