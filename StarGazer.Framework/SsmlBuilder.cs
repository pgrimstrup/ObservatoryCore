using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace StarGazer.Framework
{
    public enum EmphasisType { Strong, Moderate, Reduced }

    public class SsmlBuilder
    {
        readonly List<string> _textFragments = new List<string>();
        readonly List<string> _ssmlFragments = new List<string>();
        bool _inParagraph;
        // Letters followed by a Number - 2 groups
        Regex _alphaNumeric = new Regex("([a-zA-Z]+)([0-9]+)", RegexOptions.IgnoreCase);

        static ConcurrentDictionary<string, string> BodyTypeWordReplacements = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler Changed;

        static SsmlBuilder()
        {
            BodyTypeWordReplacements.TryAdd("I", "1");
            BodyTypeWordReplacements.TryAdd("II", "2");
            BodyTypeWordReplacements.TryAdd("III", "3");
            BodyTypeWordReplacements.TryAdd("IV", "4");
            BodyTypeWordReplacements.TryAdd("V", "5");
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

        public void Replace(string text, string replacement)
        {
            for(int i = 0; i < _textFragments.Count; i++)
                if (_textFragments[i].Contains(text))
                    _textFragments[i] = _textFragments[i].Replace(text, replacement);

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void InsertEmoji(string emoji)
        {
            if(_textFragments.Count > 0 && _textFragments[0].Length > 0)
            {
                string first = _textFragments[0];
                if (Char.IsAscii(first[0]))
                {
                    _textFragments.Insert(0, emoji);
                }
                else
                {
                    _textFragments[0] = $"{first}{emoji}";
                }
            }
            else
            {
                _textFragments.Insert(0, emoji);
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public SsmlBuilder Append(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add(text.Trim());
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendBodyName(string name)
        {
            if (!String.IsNullOrEmpty(name))
            {
                _textFragments.Add(name.Trim());
                _ssmlFragments.Add(ReplaceWords(name.Replace("-", " dash ").Replace(".", " dot ").Trim()));
            }
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendBodyType(string name)
        {
            _textFragments.Add(name.Trim());

            _ssmlFragments.Add(ReplaceWords(name.Trim(), BodyTypeWordReplacements));
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendSsml(string ssml)
        {
            _ssmlFragments.Add(ssml);
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendUnspoken(string text)
        {
            _textFragments.Add(text);
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendDigits(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<say-as interpret-as=\"digits\">{text.Trim()}</say-as>");
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendNumber(double value)
        {
            _textFragments.Add($"{value}");

            if (value >= 1000000000)
                _ssmlFragments.Add($"{value / 1000000000.0:n1} billion");
            else if (value >= 100000000)
                _ssmlFragments.Add($"{value / 1000000.0:n0} million");
            else if (value >= 1000000)
                _ssmlFragments.Add($"{value / 1000000.0:n1} million");
            else if (value >= 10000)
                _ssmlFragments.Add($"{value / 1000.0:n0} thousand");
            else if (value >= 1000)
                _ssmlFragments.Add($"{value / 1000.0:n1} thousand");
            else
                _ssmlFragments.Add($"{value:g}");
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        //public SsmlBuilder AppendNumber(long value)
        //{
        //    _textFragments.Add($"{value:n0}");

        //    if (value >= 1000000000)
        //        _ssmlFragments.Add($"{value / 1000000000.0:n1} billion");
        //    else if (value >= 10000000)
        //        _ssmlFragments.Add($"{value / 1000000.0:n0} million");
        //    else if (value >= 1000000)
        //        _ssmlFragments.Add($"{value / 1000000.0:n1} million");
        //    else if (value >= 10000)
        //        _ssmlFragments.Add($"{value / 1000.0:n0} thousand");
        //    else if (value >= 1000)
        //        _ssmlFragments.Add($"{value / 1000.0:n1} thousand");
        //    else
        //        _ssmlFragments.Add($"{value:n0}");
        //    Changed?.Invoke(this, EventArgs.Empty);
        //    return this;
        //}

        public SsmlBuilder AppendCharacters(string text)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<say-as interpret-as=\"characters\">{text.Trim()}</say-as>");
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendEmphasis(string text, EmphasisType emphasis)
        {
            _textFragments.Add(text.Trim());
            _ssmlFragments.Add($"<emphasis level=\"{emphasis.ToString().ToLower()}\">{text.Trim()}</emphasis>");
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder AppendBreak(int milliseconds)
        {
            _ssmlFragments.Add($"<break time=\"{milliseconds}ms\" />");
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        // Like EndSentence, but with a comma instead
        public SsmlBuilder AppendBreak()
        {
            if (_textFragments.Count == 0)
                _textFragments.Add(",");
            else if (!_textFragments.Last().EndsWith(','))
                _textFragments[_textFragments.Count - 1] += ",";

            if (_ssmlFragments.Count == 0)
                _ssmlFragments.Add(",");
            else if (_ssmlFragments.Last().StartsWith('<'))
                _ssmlFragments.Add(",");
            else if (!_ssmlFragments.Last().EndsWith(','))
                _ssmlFragments[_ssmlFragments.Count - 1] += ",";

            Changed?.Invoke(this, EventArgs.Empty);
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

            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder BeginParagraph()
        {
            EndParagraph();
            _ssmlFragments.Add("<p>");
            _inParagraph = true;
            Changed?.Invoke(this, EventArgs.Empty);
            return this;
        }

        public SsmlBuilder EndParagraph()
        {
            if (_inParagraph)
            {
                _ssmlFragments.Add("</p>");
                _inParagraph = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }
            return this;
        }

        // All single-letter words are spelt out.
        // All-lowercase or mixed-case words are spoken as given.
        // All-uppercase words are spelt out, as are words in the form Alpha-Number (AB01)
        // This means that the word "a" is always spelt out as the alphabet letter and not the noun-article.
        // The numbers 10 to 99 are spoken unless prefixed with a zero, all other numbers are spelt out.
        private string ReplaceWords(string text, IDictionary<string, string> replacements = null)
        {
            var words = text.Split();
            for (int i = 0; i < words.Length; i++)
            {
                var parts = words[i].Split('-');
                for(int p = 0; p < parts.Length; p++)
                {
                    Match match = _alphaNumeric.Match(parts[p]);
                    if (match.Success)
                    {
                        // Spell out the letters, but only spell out the numbers if it isn't 10 to 99
                        if (!match.Groups[2].Value.StartsWith("0") &&  Int32.TryParse(match.Groups[2].Value, out int num))
                        {
                            if (num < 10 || num >= 100)
                                parts[p] = $"<say-as interpret-as=\"characters\">{parts[p]}</say-as>";
                            else
                                parts[p] = $"<say-as interpret-as=\"characters\">{match.Groups[1].Value}</say-as> {num}";
                        }
                        else
                        {
                            // Number if prefixed with '0', so spell out the number "AB00" -> "A B zero zero"
                            parts[p] = $"<say-as interpret-as=\"characters\">{parts[p]}</say-as>";
                        }
                    }
                    else if(replacements != null && replacements.TryGetValue(parts[p], out string replacement))
                    {
                        // Simple replacement
                        parts[p] = replacement;
                    }
                    else if (parts[p].ShouldBeSpeltOut())
                    {
                        // Single letter, all upper case, or a combination of alpha-numeric that didn't match the regex
                        parts[p] = $"<say-as interpret-as=\"characters\">{parts[p]}</say-as>";
                    }
                }
                words[i] = String.Join("-", parts);
            }

            return string.Join(" ", words);
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
