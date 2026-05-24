using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BlueBird.Pinyins
{
    /// <summary>
    /// Chinese character and Pinyin conversion utility.
    /// </summary>
    public static class Pinyin
    {
        /// <summary>
        /// Converts Chinese text to its Pinyin representation.
        /// </summary>
        /// <param name="text">Chinese text to convert.</param>
        /// <param name="separator">Separator between pinyins.</param>
        /// <returns>Pinyin string. Returns null if input is null.</returns>
        [return: NotNullIfNotNull(nameof(text))]
        public static string? GetPinyin(string? text, string? separator = null)
        {
            if (text == null)
                return null;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                string pinyin = GetPinyin(text[i]);
                builder.Append(pinyin);
                if (separator != null && i != text.Length - 1)
                {
                    builder.Append(separator);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Extracts the initial letters of Chinese text's Pinyin.
        /// </summary>
        /// <param name="text">Chinese text to convert.</param>
        /// <param name="separator">Separator between initial letters.</param>
        /// <returns>Initial letters string. Returns null if input is null.</returns>
        [return: NotNullIfNotNull(nameof(text))]
        public static string? GetInitials(string? text, string? separator = null)
        {
            if (text == null)
                return null;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                string pinyin = GetPinyin(text[i]);
                builder.Append(pinyin[0]);
                if (separator != null && i != text.Length - 1)
                {
                    builder.Append(separator);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Returns the Pinyin for a single Chinese character.
        /// </summary>
        /// <param name="ch">Character to convert.</param>
        /// <returns>Pinyin for the character. Returns the character itself if not found in the pinyin data table.</returns>
        public static string GetPinyin(char ch)
        {
            short hash = GetHashIndex(ch);
            foreach (short index in PyHash.Hashes[hash])
            {
                int position = PyCode.Codes[index].IndexOf(ch, 7);
                if (position != -1)
                    return PyCode.Codes[index].Substring(0, 6).TrimEnd();
            }
            return ch.ToString();
        }

        /// <summary>
        /// Returns the list of Chinese characters matching the given Pinyin.
        /// </summary>
        /// <param name="pinyin">Pinyin string to look up.</param>
        /// <returns>Matching Chinese characters. Returns null if input is null; returns empty string if no match is found.</returns>
        [return: NotNullIfNotNull(nameof(pinyin))]
        public static string? GetChineseText(string? pinyin)
        {
            if (pinyin == null)
                return null;

            string key = pinyin.Trim().ToLower();
            foreach (string text in PyCode.Codes)
            {
                if (text.StartsWith(key + " ") || text.StartsWith(key + ":"))
                    return text.Substring(7);
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the hash table index for a character.
        /// </summary>
        private static short GetHashIndex(char ch)
        {
            return (short)((uint)ch % PyHash.Hashes.Length);
        }
    }
}