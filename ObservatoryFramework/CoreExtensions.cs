using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Observatory.Framework.Interfaces;

namespace Observatory.Framework
{
    public static class CoreExtensions
    {
        public static void Initialize(this IObservatoryCore core)
        {
            (core as IObservatoryCore2)?.Initialize();
        }

        public static T GetService<T>(this IObservatoryCore core)
            where T : notnull
        {
            if(core is IObservatoryCore2 core2)
                return core2.GetService<T>();

            throw new NotImplementedException();
        }
    }
}
