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

        string? _signals;
        string? _discovered;
        string? _mapped;
        string? _currentValue;
        string? _mappedValue;
        string? _distance;

        public string Signals
        {
            get => _signals ?? "";
            set
            {
                _signals = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Signals)));
            }
        }

        public string Discovered 
        {
            get => _discovered ?? "";
            set
            {
                _discovered = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Discovered)));
            }
        }

        public string Mapped 
        {
            get => _mapped ?? "";
            set
            {
                _mapped = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mapped)));
            }
        }

        public string CurrentValue
        {
            get => _currentValue ?? "";
            set
            {
                _currentValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentValue)));
            }
        }

        public string MappedValue 
        {
            get => _mappedValue ?? "";
            set
            {
                _mappedValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MappedValue)));
            }
        }

        public string Distance 
        {
            get => _distance ?? "";
            set
            {
                _distance = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Distance)));
            }
        }

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
