using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Observatory.Framework
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
            BodyNameCharacterReplacements.TryAdd("-", " dash ");
            BodyNameCharacterReplacements.TryAdd(".", " dot ");

            BodyTypeReplacements.TryAdd("I", "1");
            BodyTypeReplacements.TryAdd("II", "2");
            BodyTypeReplacements.TryAdd("III", "3");
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
                _ssmlFragments.Add(ReplaceWords(name.Trim(), BodyNameWordReplacements, BodyNameCharacterReplacements));
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
            _textFragments.Add($"{text.Trim()} ");
            _ssmlFragments.Add($"<say-as interpret-as=\"digits\">{text.Trim()}</say-as> ");
            return this;
        }

        public SsmlBuilder AppendNumber(double value)
        {
            _textFragments.Add($"{value} ");

            if (value > 1500000000)
                _ssmlFragments.Add($"{value / 1000000000.0:n2} billion");
            else if (value > 1500000)
                _ssmlFragments.Add($"{value / 1000000.0:n2} million");
            else if (value > 1500)
                _ssmlFragments.Add($"{value / 1000.0:n1} thousand");
            else
                _ssmlFragments.Add($"{value:g}");
            return this;
        }

        public SsmlBuilder AppendNumber(long value)
        {
            _textFragments.Add($"{value:n0} ");

            if (value > 1500000000)
                _ssmlFragments.Add($"{value / 1000000000.0:n1} billion");
            else if (value > 1500000)
                _ssmlFragments.Add($"{value / 1000000.0:n1} million");
            else if (value > 1500)
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

        private string ReplaceWords(string text, IDictionary<string, string> replacements, IDictionary<string, string> characterReplacements = null)
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
                if (_ssmlFragments[i].StartsWith('<'))
                {
                    sb.Append(_ssmlFragments[i]);
                }
                else
                {
                    sb.Append(_ssmlFragments[i]);
                    sb.Append(" ");
                }
            }

            return $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"\">{sb.ToString().Trim()}</voice></speak>";
        }
    }
}
