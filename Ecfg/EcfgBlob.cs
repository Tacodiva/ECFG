using System;
namespace Ecfg {

    public class EcfgBlob : EcfgNode {
        public byte[] Value;

        public EcfgBlob(byte[] value) {
            Value = value;
        }

        public override bool DeepEquals(EcfgNode? otherNode) {
            if (otherNode is EcfgBlob other) {
                if (other.Value.Length != Value.Length)
                    return false;
                for (int i = 0; i < Value.Length; i++)
                    if (Value[i] != other.Value[i])
                        return false;
                return true;
            }
            return false;
        }

        public override string ToString() {
            return "<blob>";
        }
        
        public static implicit operator EcfgBlob(byte[] v) => new EcfgBlob(v);
        public static implicit operator byte[](EcfgBlob v) => v.Value;
    }
}