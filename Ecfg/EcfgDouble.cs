using System;
namespace Ecfg {

    public class EcfgDouble : EcfgNumber {

        public double Value;

        public EcfgDouble(double value) {
            this.Value = value;
        }

        public override double AsDouble() {
            return Value;
        }

        public override long AsLong() {
            long longVal = (long) Value;
            if (longVal != Value)
                throw new EcfgException($"Cannot convert {Value} into a long!");
            return longVal;
        }

        public override bool DeepEquals(EcfgNode? node) {
            if (node is EcfgDouble o) return o.Value.Equals(Value); // .Equals -> NaN == NaN
            return false;
        }

        public static implicit operator EcfgDouble(double v) => new EcfgDouble(v);
        public static implicit operator double(EcfgDouble v) => v.Value;
    }
}