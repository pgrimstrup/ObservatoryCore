using Observatory;
using Observatory.Framework.Interfaces;

namespace ObservatoryUI
{
    public partial class MainPage : ContentPage
    {
        readonly IObservatoryCoreAsync _core;
        int count = 0;

        public MainPage(IObservatoryCoreAsync core)
        {
            InitializeComponent();

            _core = core;
            _core.Initialize();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}