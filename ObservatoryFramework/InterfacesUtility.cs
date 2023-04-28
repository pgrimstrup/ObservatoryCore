using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Framework.Interfaces
{
    public interface ILogMonitor
    {
        void Start();
        void Stop();

        LogMonitorState CurrentState { get; }

        event EventHandler<LogMonitorStateChangedEventArgs> LogMonitorStateChanged;

        event EventHandler<JournalEventArgs> JournalEntry;

        event EventHandler<JournalEventArgs> StatusUpdate;
    }

    public interface ISolutionPlugins
    {
        IDictionary<string, string> PluginTypes { get; }
    }

    public interface IMainFormDispatcher
    {
        void Run(Action action);
        Task RunAsync(Action action);
    }
}
