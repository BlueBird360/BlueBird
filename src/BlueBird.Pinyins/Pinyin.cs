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
            bool hasPreviousRune = false;
            foreach (Rune rune in text.EnumerateRunes())
            {
                if (hasPreviousRune && separator != null)
                {
                    builder.Append(separator);
                }

                if (TryGetPinyin(rune, out string? pinyin))
                {
                    builder.Append(pinyin);
                }
                else
                {
                    builder.Append(rune.ToString());
                }

                hasPreviousRune = true;
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
            bool hasPreviousRune = false;
            foreach (Rune rune in text.EnumerateRunes())
            {
                if (hasPreviousRune && separator != null)
                {
                    builder.Append(separator);
                }

                if (TryGetPinyin(rune, out string? pinyin))
                {
                    builder.Append(pinyin[0]);
                }
                else
                {
                    builder.Append(rune.ToString());
                }

                hasPreviousRune = true;
            }
            return builder.ToString();
        }

        /// <summary>
        /// Returns the Pinyin for a Unicode scalar value.
        /// </summary>
        /// <param name="character">Character to convert.</param>
        /// <returns>Pinyin for the character. Returns the character itself if not found in the pinyin data table.</returns>
        public static string GetPinyin(Rune character)
        {
            return TryGetPinyin(character, out string? pinyin) ? pinyin : character.ToString();
        }

        /// <summary>
        /// Attempts to return the Pinyin for a Unicode scalar value.
        /// </summary>
        /// <param name="character">Character to look up.</param>
        /// <param name="pinyin">When this method returns true, contains the Pinyin for the character.</param>
        /// <returns>true if the character exists in the pinyin data table; otherwise, false.</returns>
        public static bool TryGetPinyin(Rune character, [NotNullWhen(true)] out string? pinyin)
        {
            return PinyinData.TryGetPinyin(character, out pinyin);
        }

        /// <summary>
        /// Returns the list of Chinese characters matching the given Pinyin.
        /// </summary>
        /// <param name="pinyin">Pinyin string to look up.</param>
        /// <returns>Matching Chinese characters. Returns null if input is null; returns empty string if no match is found.</returns>
        [return: NotNullIfNotNull(nameof(pinyin))]
        public static string? GetCharacters(string? pinyin)
        {
            if (pinyin == null)
                return null;

            return PinyinData.GetCharacters(pinyin.Trim());
        }
    }
}
