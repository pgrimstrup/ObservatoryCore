using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace StarGazer.UI
{
    public static class AppExtensions
    {
        public static T? GetService<T>(this Application app) 
            => ((App)app).Services.GetService<T>();

        public static T GetRequiredService<T>(this Application app) where T : notnull
            => ((App)app).Services.GetRequiredService<T>();
    }
}
