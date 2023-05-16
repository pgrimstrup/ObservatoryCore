using System.Collections.Concurrent;
using System.Text;

namespace StarGazer.Framework
{
    public enum EmphasisType { Strong, Moderate, Reduced }

    public class SsmlBuilder
    {
        readonly List<string> _textFragments = new List<string>();
        readonly List<string> _ssmlFragments = new List<string>();
        bool _inParagraph;

        static ConcurrentDictionary<string, string> BodyNameWordReplacements = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static ConcurrentDictionary<string, string> BodyNameCharacterReplacements = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static ConcurrentDictionary<string, string> BodyTypeReplacements = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        static SsmlBuilder()
        {
            BodyNameWordReplacements.TryAdd("A", "<say-as interpret-as=\"characters\">A</say-as>");
            BodyNameWordReplacements.TryAdd("B", "<say-as interpret-as=\"characters\">B</say-as>");
            BodyNameWordReplacements.TryAdd("C", "<say-as interpret-as=\"characters\">C</say-as>");
            BodyNameWordReplacements.TryAdd("D", "<say-as interpret-as=\"characters\">D</say-as>");
            BodyNameWordReplacements.TryAdd("E", "<say-as interpret-as=\"characters\">E</say-as>");
            BodyNameWordReplacements.TryAdd("F", "<say-as interpret-as=\"characters\">F</say-as>");
            BodyNameWordReplacements.TryAdd("G", "<say-as interpret-as=\"characters\">G</say-as>");
            BodyNameWordReplacements.TryAdd("H", "<say-as interpret-as=\"characters\">H</say-as>");
            BodyNameWordReplacements.TryAdd("I", "<say-as interpret-as=\"characters\">I</say-as>");
            BodyNameWordReplacements.TryAdd("J", "<say-as interpret-as=\"characters\">J</say-as>");
            BodyNameWordReplacements.TryAdd("K", "<say-as interpret-as=\"characters\">K</say-as>");
            BodyNameWordReplacements.TryAdd("L", "<say-as interpret-as=\"characters\">L</say-as>");
            BodyNameWordReplacements.TryAdd("M", "<say-as interpret-as=\"characters\">M</say-as>");
            BodyNameWordReplacements.TryAdd("N", "<say-as interpret-as=\"characters\">N</say-as>");
            BodyNameWordReplacements.TryAdd("O", "<say-as interpret-as=\"characters\">O</say-as>");
            BodyNameWordReplacements.TryAdd("P", "<say-as interpret-as=\"characters\">P</say-as>");
            BodyNameWordReplacements.TryAdd("Q", "<say-as interpret-as=\"characters\">Q</say-as>");
            BodyNameWordReplacements.TryAdd("R", "<say-as interpret-as=\"characters\">R</say-as>");
            BodyNameWordReplacements.TryAdd("S", "<say-as interpret-as=\"characters\">S</say-as>");
            BodyNameWordReplacements.TryAdd("T", "<say-as interpret-as=\"characters\">T</say-as>");
            BodyNameWordReplacements.TryAdd("U", "<say-as interpret-as=\"characters\">U</say-as>");
            BodyNameWordReplacements.TryAdd("V", "<say-as interpret-as=\"characters\">V</say-as>");
            BodyNameWordReplacements.TryAdd("W", "<say-as interpret-as=\"characters\">W</say-as>");
            BodyNameWordReplacements.TryAdd("X", "<say-as interpret-as=\"characters\">X</say-as>");
            BodyNameWordReplacements.TryAdd("Y", "<say-as interpret-as=\"characters\">Y</say-as>");
            BodyNameWordReplacements.TryAdd("Z", "<say-as interpret-as=\"characters\">Z</say-as>");
            BodyNameCharacterReplacements.TryAdd("-", " dash ");
            BodyNameCharacterReplacements.TryAdd(".", " dot ");

            BodyTypeReplacements.TryAdd("I", "1");
            BodyTypeReplacements.TryAdd("II", "2");
            BodyTypeReplacements.TryAdd("III", "3");
            BodyTypeReplacements.TryAdd("IV", "4");
            BodyTypeReplacements.TryAdd("V", "5");
        }

        public SsmlBuilder()
        {
            CommaBreak = 250;
            PeriodBreak = 500;
        }

        public int CommaBreak { get; set; } 
        public int PeriodBreak { get; set; }

        public string CommaBreakSsml => $"<break time=\"{CommaBreak}ms\"/>";
        public string PeriodBreakSsml => $"<break time=\"{PeriodBreak}ms\"/>";

        public SsmlBuilder Append(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add(text.Trim());
            return this;
        }

        public SsmlBuilder AppendBodyName(string name)
        {
            if (!String.IsNullOrEmpty(name))
            {
                _textFragments.Add(name.Trim());
                _ssmlFragments.Add(ReplaceWords(name.Trim(), BodyNameWordReplacements, BodyNameCharacterReplacements, true));
            }
            return this;
        }

        public SsmlBuilder AppendBodyType(string name)
        {
            _textFragments.Add(name.Trim());
            _ssmlFragments.Add(ReplaceWords(name.Trim(), BodyTypeReplacements));
            return this;
        }

        public SsmlBuilder AppendSsml(string ssml)
        {
            _ssmlFragments.Add(ssml);
            return this;
        }

        public SsmlBuilder AppendUnspoken(string text)
        {
            _textFragments.Add(text);
            return this;
        }

        public SsmlBuilder AppendDigits(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<say-as interpret-as=\"digits\">{text.Trim()}</say-as>");
            return this;
        }

        public SsmlBuilder AppendNumber(double value)
        {
            _textFragments.Add($"{value}");

            if (value >= 1000000000)
                _ssmlFragments.Add($"{value / 1000000000.0:n2} billion");
            else if (value >= 1000000)
                _ssmlFragments.Add($"{value / 1000000.0:n2} million");
            else if (value >= 1000)
                _ssmlFragments.Add($"{value / 1000.0:n1} thousand");
            else
                _ssmlFragments.Add($"{value:g}");
            return this;
        }

        public SsmlBuilder AppendNumber(long value)
        {
            _textFragments.Add($"{value:n0}");

            if (value >= 1000000000)
                _ssmlFragments.Add($"{value / 1000000000.0:n1} billion");
            else if (value >= 1000000)
                _ssmlFragments.Add($"{value / 1000000.0:n1} million");
            else if (value >= 1000)
                _ssmlFragments.Add($"{value / 1000.0:n1} thousand");
            else
                _ssmlFragments.Add($"{value:n0}");
            return this;
        }

        public SsmlBuilder AppendCharacters(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<say-as interpret-as=\"characters\">{text.Trim()}</say-as>");
            return this;
        }

        public SsmlBuilder AppendEmphasis(string text, EmphasisType emphasis)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<emphasis level=\"{emphasis.ToString().ToLower()}\">{text.Trim()}</emphasis>");
            return this;
        }

        public SsmlBuilder AppendBreak(int milliseconds)
        {
            _ssmlFragments.Add($"<break time=\"{milliseconds}ms\" />");
            return this;
        }

        public SsmlBuilder EndSentence()
        {
            if (_textFragments.Count == 0)
                _textFragments.Add(".");
            else if (!_textFragments.Last().EndsWith('.'))
                _textFragments[_textFragments.Count - 1] += ".";

            if (_ssmlFragments.Count == 0)
                _ssmlFragments.Add(".");
            else if (_ssmlFragments.Last().StartsWith('<'))
                _ssmlFragments.Add(".");
            else if (!_ssmlFragments.Last().EndsWith('.'))
                _ssmlFragments[_ssmlFragments.Count - 1] += ".";

            return this;
        }

        public SsmlBuilder BeginParagraph()
        {
            EndParagraph();
            _ssmlFragments.Add("<p>");
            _inParagraph = true;
            return this;
        }

        public SsmlBuilder EndParagraph()
        {
            if (_inParagraph)
            {
                _ssmlFragments.Add("</p>");
                _inParagraph = false;
            }
            return this;
        }

        private string ReplaceWords(string text, IDictionary<string, string> replacements, IDictionary<string, string> characterReplacements = null, bool spellOutNumbers = false)
        {
            if (characterReplacements != null)
            {
                foreach (var key in characterReplacements.Keys)
                {
                    text = text.Replace(key, characterReplacements[key]);
                }
            }

            var words = text.Split();
            for (int i = 0; i < words.Length; i++)
            {
                if (replacements.TryGetValue(words[i], out var replacement))
                    words[i] = replacement;
                if (Int32.TryParse(words[i], out int number) && number >= 100)
                    words[i] = $"<say-as interpret-as=\"digits\">{words[i]}</say-as>";
            }
            text = string.Join(" ", words);

            return text;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < _textFragments.Count; i++)
            {
                if (i > 0 && !_textFragments[i].StartsWith(",") && !_textFragments[i].StartsWith("."))
                    sb.Append(" ");
                sb.Append(_textFragments[i]);
            }

            return sb.ToString();
        }

        public string ToSsml()
        {
            EndParagraph();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _ssmlFragments.Count; i++)
            {
                if (i > 0 && !_ssmlFragments[i].StartsWith(",") && !_ssmlFragments[i].StartsWith(".") && !_ssmlFragments[i].StartsWith("<"))
                    sb.Append(" ");
                sb.Append(_ssmlFragments[i]);
            }

            return $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">{sb.ToString().Trim()}</speak>";
        }
    }
}
