using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PermissionDaemon
{
    public static class GlobPatternMatcher
    {
        /// <summary>
        /// Check if a file path matches a glob pattern
        /// </summary>
        /// <param name="pattern">The glob pattern (supports *, **, ?)</param>
        /// <param name="filePath">The file path to test</param>
        /// <returns>True if the file path matches the pattern</returns>
        public static bool IsMatch(string pattern, string filePath)
        {
            // Normalize paths to use forward slashes for consistent matching
            var normalizedPath = filePath.Replace('\\', '/');
            var normalizedPattern = pattern.Replace('\\', '/');

            // Convert glob pattern to regex
            string regexPattern = ConvertGlobToRegex(normalizedPattern);

            return Regex.IsMatch(normalizedPath, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Check if a file path matches any pattern in the provided list
        /// </summary>
        /// <param name="patterns">List of glob patterns</param>
        /// <param name="filePath">The file path to test</param>
        /// <returns>True if the file path matches any of the patterns</returns>
        public static bool IsMatchAny(List<string> patterns, string filePath)
        {
            foreach (var pattern in patterns)
            {
                if (IsMatch(pattern, filePath))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ConvertGlobToRegex(string glob)
        {
            // Escape special regex characters except for our glob wildcards
            string regex = Regex.Escape(glob);

            // Replace glob wildcards with regex equivalents
            regex = regex.Replace(@"\*\*/", "(.*/)?"); // ** followed by slash (matches zero or more directories)
            regex = regex.Replace(@"\*\*", ".*");      // ** (matches zero or more directories/files)
            regex = regex.Replace(@"\*", @"[^/]*");    // * (matches zero or more characters except slash)
            regex = regex.Replace(@"\?", ".");         // ? (matches exactly one character except slash)

            return "^" + regex + "$";
        }
    }
}