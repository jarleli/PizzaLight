using System;

namespace PizzaLight.Resources.ExtensionClasses
{
    public static class StringExtensions
    {
        public static string SafeSubstring(this string target, int startIndex, int length)
        {
            length = Math.Min(length, target.Length);
            return target.Substring(startIndex, length);
        }
    }
}