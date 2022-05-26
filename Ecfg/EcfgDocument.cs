using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Ecfg {

    public class EcfgDocument : EcfgObject {

        public static new EcfgDocument Parse(string text) {
            return new EcfgTokenizer(text).ParseToDocument();
        }

        internal EcfgObjectToken _rootToken;

        internal EcfgDocument(EcfgObjectToken root) : base(root.ToChildren()) {
            _rootToken = root;
        }

        public new string ToEcfg() {
            return _rootToken.ToEcfgDocument();
        }

        public override string ToString() {
            return ToEcfg();
        }
    }
}