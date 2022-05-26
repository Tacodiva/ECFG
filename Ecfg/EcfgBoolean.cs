namespace Ecfg {

    public class EcfgBoolean : EcfgNode {
        public bool Value;

        public EcfgBoolean(bool value) {
            this.Value = value;
        }

        public override bool DeepEquals(EcfgNode? node) {
            if (node is EcfgBoolean o) return o.Value == Value;
            return false;
        }

        public static implicit operator EcfgBoolean(bool v) => new EcfgBoolean(v);
        public static implicit operator bool(EcfgBoolean v) => v.Value;
    }
}