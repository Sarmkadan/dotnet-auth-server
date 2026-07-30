using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetAuthServer.Security
{
    public static class ScopeStringExtensions
    {
        public static HashSet<string> ToScopeSet(this string scopeString)
        {
            return new HashSet<string>(scopeString.Split(' '));
        }

        public static bool ContainsScope(this string scopeString, string scope)
        {
            var scopeSet = ToScopeSet(scopeString);
            return scopeSet.Contains(scope);
        }

        public static string ToScopeString(this IEnumerable<string> scopes)
        {
            return string.Join(" ", scopes);
        }
    }
}