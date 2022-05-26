using System;

namespace Ecfg {
    public abstract class EcfgNode {
        public abstract bool DeepEquals(EcfgNode? node);

        public override string ToString() {
            return ToEcfg();
        }

        public string ToEcfg() {
            return EcfgRetoknizer.Retoknize(this);
        }
    }

    public abstract class EcfgNumber : EcfgNode {
        public abstract long AsLong();
        public abstract double AsDouble();
    }
}