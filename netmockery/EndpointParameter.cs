using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netmockery
{
    public class EndpointParameter
    {
        private string _value;

        public string Value
        {
            set
            {
                _value = value;
            }

            get
            {
                return _value ?? DefaultValue;
            }
        }

        public string Name;
        public string DefaultValue;
        public string Description;

        public bool ValueIsDefault => Value == DefaultValue;
        public void ResetToDefaultValue() { _value = null; }
    }
}
