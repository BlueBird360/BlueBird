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
            short bucketIndex = GetBucketIndex(ch);
            foreach (short index in PinyinIndex.Buckets[bucketIndex])
            {
                if (PinyinData.Entries[index].Characters.Contains(ch))
                {
                    return PinyinData.Entries[index].Pinyin;
                }
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

            string key = pinyin.Trim().ToLowerInvariant();
            foreach (var entry in PinyinData.Entries)
            {
                if (entry.Pinyin == key)
                    return entry.Characters;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the bucket index for a character.
        /// </summary>
        private static short GetBucketIndex(char ch)
        {
            return (short)((uint)ch % PinyinIndex.Buckets.Length);
        }
    }
}
