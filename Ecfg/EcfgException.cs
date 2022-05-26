using System;
namespace Ecfg {

    public class EcfgException : SystemException {

        public EcfgException(string description) : base(description) { }

        public EcfgException(string description, int line, int col) : base(description + " (Line " + line + " Col "+col+")") { }

        public EcfgException(string description, Exception cause) : base(description, cause) {}

        public EcfgException(string description, int line, int col, Exception cause) : base(description + " (Line " + line + " Col "+col+")", cause) {}

    }
}