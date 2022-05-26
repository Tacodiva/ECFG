using System.Collections.Generic;
using System.Text;

namespace Ecfg {
    public class EcfgObject : EcfgNode {

        public static EcfgObject Parse(string zcfg) {
            return new EcfgTokenizer(zcfg).ParseToObject();
        }

        public Dictionary<string, EcfgNode?> Children;

        public bool Empty => Children.Count == 0;

        public EcfgObject(Dictionary<string, EcfgNode?> children) {
            this.Children = children;
        }

        public EcfgObject() {
            Children = new Dictionary<string, EcfgNode?>();
        }

        private T? GetNode<T>(string key) where T : EcfgNode {
            Children.TryGetValue(key, out EcfgNode? value);
            if (value == null) throw new EcfgException("\"" + key + "\" is null.");
            if (value is T cast) return cast;
            return null;
        }

        public bool TryGetValue(string key, out EcfgNode? node) {
            return Children.TryGetValue(key, out node);
        }

        public IEnumerable<KeyValuePair<string, EcfgNode?>> Entries {
            get {
                return (IEnumerable<KeyValuePair<string, EcfgNode?>>)Children;
            }
        }

        public IEnumerable<string> Keys {
            get {
                return (IEnumerable<string>)Children.Keys;
            }
        }

        public EcfgNode? Get(string key) {
            Children.TryGetValue(key, out EcfgNode? value);
            return value;
        }

        public bool ContainsKey(string key) {
            return Children.ContainsKey(key);
        }

        public bool IsNull(string key) {
            Children.TryGetValue(key, out EcfgNode? value);
            return value == null;
        }

        public double GetDouble(string key) {
            return GetNode<EcfgNumber>(key)?.AsDouble() ??
                throw new EcfgException("\"" + key + "\" is not a double.");
        }

        public long GetLong(string key) {
            return GetNode<EcfgNumber>(key)?.AsLong() ??
                throw new EcfgException("\"" + key + "\" is not a long.");
        }

        public string GetString(string key) {
            return GetNode<EcfgString>(key)?.Value ??
                throw new EcfgException("\"" + key + "\" is not a string.");
        }

        public EcfgList GetList(string key) {
            return GetNode<EcfgList>(key) ??
                throw new EcfgException("\"" + key + "\" is not a list.");
        }

        public bool GetBool(string key) {
            return GetNode<EcfgBoolean>(key)?.Value ??
                throw new EcfgException("\"" + key + "\" is not a boolean.");
        }

        public EcfgObject GetObject(string key) {
            return GetNode<EcfgObject>(key) ??
                throw new EcfgException("\"" + key + "\" is not an object.");
        }

        public void Set(string key, double value) {
            Children[key] = new EcfgDouble(value);
        }

        public void Set(string key, long value) {
            Children[key] = new EcfgLong(value);
        }

        public void Set(string key, string value) {
            Children[key] = new EcfgString(value);
        }

        public void Set(string key, EcfgList value) {
            Children[key] = value;
        }

        public void Set(string key, bool value) {
            Children[key] = new EcfgBoolean(value);
        }

        public void Set(string key, EcfgObject value) {
            Children[key] = value;
        }

        public EcfgNode? this[string key] {
            get { return Children[key]; }
            set { Children[key] = value; }
        }

        public override bool DeepEquals(EcfgNode? otherNode) {
            if (otherNode is EcfgObject other) {
                if (Children.Count != other.Children.Count)
                    return false;
                foreach (KeyValuePair<string, EcfgNode?> entry in Children) {
                    if (!other.Children.TryGetValue(entry.Key, out EcfgNode? otherChildNode))
                        return false;
                    if (!(entry.Value?.DeepEquals(otherChildNode) ?? otherChildNode == null))
                        return false;
                }
                return true;
            }
            return false;
        }
    }
}