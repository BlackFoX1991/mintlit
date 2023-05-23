using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mintlit.backend
{
    public class keyValue
    {

        public string inKeyPath { get; set; }
        public object value { get; set; }

        public keyValue(string inKeyPath, object value)
        {
            this.inKeyPath = inKeyPath;
            this.value = value;
        }
    }
}
