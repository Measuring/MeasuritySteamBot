using System;

namespace MeasuritySteamBot.Extensions
{
    public static class VersionExtensions
    {
        /// <summary>
        ///     Returns the shortest version of <see cref="Version" /> based on if a part is zero or not.
        /// </summary>
        /// <param name="version">Version to get shorted notation from.</param>
        /// <returns></returns>
        public static string ToShortString(this Version version)
        {
            if (version.Revision != 0)
                return version.ToString();
            if (version.Build != 0)
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            if (version.Minor != 0)
                return string.Format("{0}.{1}", version.Major, version.Minor);
            return version.Major.ToString();
        }
    }
}