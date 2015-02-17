using System;

namespace MeasuritySteamBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BotAuthorizeAttribute : Attribute
    {
        private string _name;

        /// <summary>
        ///     Specifies that this class or method requires rights.
        /// </summary>
        public BotAuthorizeAttribute()
        {
        }

        /// <summary>
        ///     Creates an authentication name that must be fulfilled by the user to execute this command.
        /// </summary>
        /// <param name="name"></param>
        public BotAuthorizeAttribute(string name)
        {
            _name = name;
        }

        /// <summary>
        ///     Lowercase name name that the user must be in for authorizations.
        /// </summary>
        public string Name
        {
            get { return _name; }
            protected set { _name = value.ToLowerInvariant(); }
        }
    }
}