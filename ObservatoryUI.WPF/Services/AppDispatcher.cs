using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.WPF.Services
{
    internal class AppDispatcher : IMainFormDispatcher
    {
        public void Run(Action action)
        {
            App.Current.Dispatcher.Invoke(action);
        }

        public async Task RunAsync(Action action)
        {
            await App.Current.Dispatcher.InvokeAsync(action);
        }
    }
}
