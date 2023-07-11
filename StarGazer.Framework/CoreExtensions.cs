using System.Text.Json;

namespace StarGazer.Framework
{
    public static class CoreExtensions
    {
        public static JsonSerializerOptions SerializerOptions;

        static CoreExtensions()
        {
            SerializerOptions = new JsonSerializerOptions();
            SerializerOptions.WriteIndented = true;
            SerializerOptions.IgnoreReadOnlyProperties = true;
            SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
            SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        }

        public static string SafeTrim(this string value, params char[] chars)
        {
            if (value == null)
                return null;

            if (chars == null || chars.Length == 0)
                return value.Trim();

            return value.Trim(chars);
        }

        public static bool ShouldBeVerbatim(this string word)
        {
            if (String.IsNullOrWhiteSpace(word))
                return false;

            // "a" -> "A" (as the alphabet letter, not the article)
            if (word.Length == 1 && Char.IsLetter(word[0]))
                return true;

            // "01" -> "Zero One"
            // "10" -> "Ten"
            // "101" -> "One Zero One"
            // "AB01" -> "A B Zero One"
            // "AB10" -> "A B Ten"
            // "AB101" -> "A B One Zero One"

            bool isNumeric = false;
            bool isAlpha = false;
            bool isSymbol = false;

            // We wil ignore certain letters
            char[] ignored = "-[]()_'.\"".ToCharArray();
            for(int i = 0; i < word.Length; i++)
            {
                if (ignored.Contains(word[i]))
                    continue;

                if (Char.IsNumber(word[i]))
                    isNumeric = true;
                else if (Char.IsLetter(word[i]))
                    isAlpha = true;
                else
                    isSymbol = true;
            }

            // Contains non-alphanumeric, spell it out
            if (isSymbol)
                return true;

            // Contains alpha and numbers, spell it out
            if (isAlpha && isNumeric)
                return true;

            // Its just a number. 0..9 and 100+ are all spelt out
            if (isNumeric && Int64.TryParse(word, out long num))
                return num < 10 || num >= 100 || word.StartsWith('0');

            // Its just alpha
            return false;
        }

        public static object SimpleClone(this object source)
        {
            if (source == null)
                return null;

            var target = Activator.CreateInstance(source.GetType());
            foreach(var prop in source.GetType().GetProperties())
            {
                if(prop.CanRead && prop.CanWrite)
                    prop.SetValue(target, prop.GetValue(source, null));
            }
            return target;
        }
    }
}
