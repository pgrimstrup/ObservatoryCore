using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Files.Journal;

namespace Observatory.Bridge
{
    internal class BridgeLog
    {
        internal readonly Bridge Bridge;
        internal bool IsSpoken = true;
        internal bool IsText = true;
        internal bool IsTitleSpoken = false;
        internal bool IsDetailSpoken = true;
        internal DateTime EventTimeUTC;
        internal Guid NotificationId;
        internal SsmlBuilder TitleSsml;
        internal SsmlBuilder DetailSsml;

        [Display(Name = "Time")]
        public string EventTime => EventTimeUTC.ToString();

        public string Title => TitleSsml.ToString();

        public string Detail => DetailSsml.ToString();

        public BridgeLog(Bridge bridge, JournalBase journal)
            : this(bridge)
        {
            EventTimeUTC = journal.TimestampDateTime;
        }
        public BridgeLog(Bridge bridge)
        {
            Bridge = bridge;
            EventTimeUTC = DateTime.UtcNow;
            TitleSsml = new SsmlBuilder {
                CommaBreak = bridge.Options.SpokenCommaDelay,
                PeriodBreak = bridge.Options.SpokenPeriodDelay
            };
            DetailSsml = new SsmlBuilder {
                CommaBreak = bridge.Options.SpokenCommaDelay,
                PeriodBreak = bridge.Options.SpokenPeriodDelay
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
