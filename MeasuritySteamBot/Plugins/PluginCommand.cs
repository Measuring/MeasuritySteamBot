using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MeasuritySteamBot.Attributes;

namespace MeasuritySteamBot.Plugins
{
    internal class PluginCommand
    {
        public PluginCommand(object categoryInstance, MethodInfo methodInfo)
        {
            CategoryInstance = categoryInstance;
            MethodInfo = methodInfo;
            var attr = MethodInfo.GetCustomAttribute<BotDisplayAttribute>();
            Description = attr != null ? attr.Description : null;
            Parameters =
                MethodInfo.GetParameters()
                    .Select(
                        p =>
                            new
                            {
                                Parameter = p,
                                Attribute = p.GetCustomAttribute<BotDisplayAttribute>(),
                                Name = p.Name.ToLowerInvariant()
                            })
                    .Select(
                        p =>
                            new KeyValuePair<string, Tuple<ParameterInfo, string>>(p.Name,
                                new Tuple<ParameterInfo, string>(p.Parameter,
                                    p.Attribute != null ? p.Attribute.Description : null))).ToList();
        }

        public string Description { get; protected set; }
        public List<KeyValuePair<string, Tuple<ParameterInfo, string>>> Parameters { get; protected set; }
        protected MethodInfo MethodInfo { get; set; }
        protected object CategoryInstance { get; set; }

        public bool Invoke(params object[] parms)
        {
            if (Parameters.Count != parms.Length &&
                !Attribute.IsDefined(Parameters.Last().Value.Item1, typeof(ParamArrayAttribute)))
            {
                return false;
            }

            MethodInfo.Invoke(CategoryInstance, parms);
            return true;
        }

        /// <summary>
        ///     Returns user friendly method usage.
        /// </summary>
        /// <returns>User friendly representation of method usage.</returns>
        public string Help()
        {
            var helpBuilder = new StringBuilder();

            // Command indicator.
            helpBuilder.Append('/');

            // Category name.
            var catType = CategoryInstance.GetType();
            var catAttr = catType.GetCustomAttribute<BotDisplayAttribute>();
            helpBuilder.Append(catAttr != null ? catAttr.Name : catType.Name.ToLowerInvariant());
            helpBuilder.Append(' ');

            // Command name.
            var comAttr = MethodInfo.GetCustomAttribute<BotDisplayAttribute>();
            helpBuilder.Append(comAttr != null ? comAttr.Name : MethodInfo.Name.ToLowerInvariant());

            // Parameters.
            if (Parameters.Count > 0)
                helpBuilder.Append(' ');

            foreach (var parm in Parameters)
            {
                if (parm.Value.Item1.IsOptional)
                    helpBuilder.AppendFormat("[{0}:{1}]", parm.Value.Item1,
                        parm.Value.Item1.ParameterType.Name.ToLowerInvariant());
                else
                    helpBuilder.AppendFormat("<{0}:{1}>", parm.Value.Item1.Name,
                        parm.Value.Item1.ParameterType.Name.ToLowerInvariant());
            }

            return helpBuilder.ToString();
        }
    }
}