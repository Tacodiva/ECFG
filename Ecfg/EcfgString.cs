namespace Ecfg {

    public class EcfgString : EcfgNode {
        public string Value;

        public EcfgString(string value) {
            this.Value = value;
        }
        
        public override bool DeepEquals(EcfgNode? node) {
            if (node is EcfgString o) return o.Value == Value;
            return false;
        }

        public override string ToString() {
            return "'" + Value + "'";
        }

        public static implicit operator EcfgString(string v) => new EcfgString(v);
        public static implicit operator string(EcfgString v) => v.Value;

    }
}