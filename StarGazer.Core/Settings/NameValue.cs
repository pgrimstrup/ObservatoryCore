using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarGazer.Settings
{
    public class NameValue
    {
        public string Name { get; }
        public object Value { get; }

        public NameValue(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
