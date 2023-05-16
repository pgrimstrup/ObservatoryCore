using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarGazer.Framework.Interfaces;

namespace StarGazer.UI.Services
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
