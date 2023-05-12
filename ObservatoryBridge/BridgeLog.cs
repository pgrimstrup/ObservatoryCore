using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge
{
    internal class BridgeLog
    {
        internal string EventName = "";
        internal bool IsSpoken = true;
        internal bool IsText = true;
        internal bool IsTitleSpoken = false;
        internal bool IsDetailSpoken = true;
        internal DateTime EventTimeUTC;
        internal SsmlBuilder TitleSsml;
        internal SsmlBuilder DetailSsml;

        [Display(Name = "Time")]
        public string EventTime => EventTimeUTC.ToString();

        public string Title => TitleSsml.ToString();

        public string Detail => DetailSsml.ToString();

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
