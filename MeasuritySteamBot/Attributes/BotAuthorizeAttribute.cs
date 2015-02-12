using System;

namespace MeasuritySteamBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BotAuthorizeAttribute : Attribute
    {
        private string _group;

        public BotAuthorizeAttribute(string @group)
        {
            _group = @group;
        }

        /// <summary>
        ///     Lowercase group name that the user must be in for authorizations.
        /// </summary>
        public string Group
        {
            get { return _group; }
            protected set { _group = value.ToLowerInvariant(); }
        }
    }
}