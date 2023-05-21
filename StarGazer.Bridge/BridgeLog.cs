using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Observatory.Framework.Files.Journal;
using StarGazer.Framework;

namespace StarGazer.Bridge
{
    internal class BridgeLog : INotifyPropertyChanged
    {
        internal string EventName = "";
        internal bool IsSpoken = true;
        internal bool IsText = true;
        internal bool IsTitleSpoken = false;
        internal bool IsDetailSpoken = true;
        internal DateTime EventTimeUTC;
        internal SsmlBuilder TitleSsml;
        internal SsmlBuilder DetailSsml;

        public event PropertyChangedEventHandler? PropertyChanged;

        [Display(Name = "Time")]
        public string EventTime => EventTimeUTC.ToString();

        public string Title => TitleSsml.ToString();

        public string Detail => DetailSsml.ToString();

        public string? EstimatedValue { get; set; }
        public string? Distance { get; set; }

        public BridgeLog(JournalBase journal) : this()
        {
            EventTimeUTC = journal.TimestampDateTime;
            EventName = journal.Event;
        }

        public BridgeLog()
        {
            EventTimeUTC = DateTime.UtcNow;
            TitleSsml = new SsmlBuilder {
                CommaBreak = Bridge.Instance.Settings.SpokenCommaDelay,
                PeriodBreak = Bridge.Instance.Settings.SpokenPeriodDelay
            };
            DetailSsml = new SsmlBuilder {
                CommaBreak = Bridge.Instance.Settings.SpokenCommaDelay,
                PeriodBreak = Bridge.Instance.Settings.SpokenPeriodDelay
            };

            TitleSsml.Changed += (sender, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            DetailSsml.Changed += (sender, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
        }

        public void Send()
        {
            Bridge.Instance.LogEvent(this);
        }

        public BridgeLog SpokenOnly()
        {
            IsSpoken = true;
            IsText = false;
            return this;
        }

        public BridgeLog TextOnly()
        {
            IsSpoken = false;
            IsText = true;
            return this;
        }
    }
}
