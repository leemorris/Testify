using System;
using System.Text;

namespace Leem.Testify
{
    public static class StringExtensions
    {
        public static string ReplaceAt(this string input, int index, string newString)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            var builder = new StringBuilder(input);
            builder.Remove(index, 1);
            builder.Insert(index, newString);
            return builder.ToString();
        }
    }
}