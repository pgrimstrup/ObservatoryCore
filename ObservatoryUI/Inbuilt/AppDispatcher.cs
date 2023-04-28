using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI.Inbuilt
{
    internal class AppDispatcher : IMainFormDispatcher
    {
        public void Run(Action action)
        {
            App.Current.Dispatcher.Dispatch(action);
        }

        public async Task RunAsync(Action action)
        {
            await App.Current.Dispatcher.DispatchAsync(action);
        }
    }
}
