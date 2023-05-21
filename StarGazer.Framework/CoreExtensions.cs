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

        public static bool ShouldBeSpeltOut(this string word)
        {
            if (String.IsNullOrWhiteSpace(word))
                return false;

            // "a" -> "A" (as the alphabet letter, not the article)
            if (word.Length == 1)
                return true;

            // "01" -> "Zero One"
            // "10" -> "Ten"
            // "101" -> "One Zero One"
            // "AB01" -> "A B Zero One"
            // "AB10" -> "A B Ten"
            // "AB101" -> "A B One Zero One"

            char[] numbers = "1234567890".ToCharArray();
            int numIndex = word.IndexOfAny(numbers);
            if(numIndex < 0)
            {
                // No numbers, just letters. If its all uppercase, then spell it out
                return word == word.ToUpper();
            }
            else if(numIndex == 0 && Int32.TryParse(word, out int num))
            {
                // Its just a number. 0..9 and 100+ are all spelt out
                return num < 10 || num >= 100;
            }
            else
            {
                // Combination of numbers and letters
                return true;
            }
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
