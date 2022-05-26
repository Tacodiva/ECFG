namespace Ecfg {

    public class EcfgLong : EcfgNumber {
        public long Value;

        public EcfgLong(long value) {
            this.Value = value;
        }

        public override bool DeepEquals(EcfgNode? node) {
            if (node is EcfgLong o) return o.Value == Value;
            return false;
        }

        public override double AsDouble() {
            return Value;
        }

        public override long AsLong() {
            return Value;
        }

        public static implicit operator EcfgLong(long v) => new EcfgLong(v);
        public static implicit operator long(EcfgLong v) => v.Value;

    }
}