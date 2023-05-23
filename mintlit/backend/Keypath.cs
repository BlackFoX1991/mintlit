using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mintlit.backend
{
    public class Keypath
    {

        public string keyname { get; set; }
        public string fullpath { get; set; }
        public List<keyValue> kValues { get; set; }
        public bool isContainer { get; set; }

        public Keypath(string keyname, string fullpath, List<keyValue> kValues, bool isContainer)
        {
            this.keyname = keyname;
            this.fullpath = fullpath;
            this.kValues = kValues;
            this.isContainer = isContainer;
        }

        public Keypath()
        {
            keyname = "";
            fullpath = "";
            kValues = new List<keyValue>();
            isContainer = false;
        }



    }
}
