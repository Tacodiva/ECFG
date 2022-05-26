using System;
using System.Collections.Generic;
using System.Text;

namespace Ecfg {
    public class EcfgList : EcfgNode {

        public List<EcfgNode?> List;

        public int Count => List.Count;

        public EcfgList(List<EcfgNode?> List) {
            this.List = List;
        }

        public EcfgList() {
            this.List = new List<EcfgNode?>();
        }

        public EcfgList(EcfgNode?[] nodes) : this() {
            List.AddRange(nodes);
        }

        public EcfgNode? Get(int index) {
            return List[index];
        }

        public EcfgNode? this[int index] {
            get { return Get(index); }
            set {
                if (index == List.Count) List.Add(value);
                else List[index] = value;
            }
        }

        private T? Get<T>(int index) where T : EcfgNode {
            EcfgNode node = List[index] ??
                throw new EcfgException("List element " + index + " is null.");
            if (node is T cast) return cast;
            return null;
        }

        public long GetLong(int index) {
            return Get<EcfgLong>(index)?.Value ??
                throw new EcfgException("List element " + index + " is not a long.");
        }

        public double GetDouble(int index) {
            return Get<EcfgDouble>(index)?.Value ??
                throw new EcfgException("List element " + index + " is not a double.");
        }

        public string GetString(int index) {
            return Get<EcfgString>(index)?.Value ??
                throw new EcfgException("List element " + index + " is not a string.");
        }

        public bool GetBool(int index) {
            return Get<EcfgBoolean>(index)?.Value ??
                throw new EcfgException("List element " + index + " is not a boolean.");
        }

        public EcfgList GetList(int index) {
            return Get<EcfgList>(index) ??
                throw new EcfgException("List element " + index + " is not a list.");
        }

        public EcfgObject GetObject(int index) {
            return Get<EcfgObject>(index) ??
                throw new EcfgException("List element " + index + " is not an object.");
        }

        public void Set(int index, EcfgNode? value) {
            List[index] = value;
        }

        public void Set(int index, long value) {
            List[index] = new EcfgLong(value);
        }

        public void Set(int index, double value) {
            List[index] = new EcfgDouble(value);
        }

        public void Set(int index, string value) {
            List[index] = new EcfgString(value);
        }

        public void Set(int index, EcfgList value) {
            List[index] = value;
        }

        public void Set(int index, EcfgObject value) {
            List[index] = value;
        }

        public T[] ToArray<T>() {
            T[] arr = new T[List.Count];
            for (int i = 0; i < List.Count; i++) {
                EcfgNode? node = List[i];
                if (node is T t) arr[i] = t;
                else throw new EcfgException($"Node {node?.GetType()} cannot be cast to {typeof(T)}.");
            }
            return arr;
        }

        public T?[] ToNullableArray<T>() {
            T?[] arr = new T?[List.Count];
            for (int i = 0; i < List.Count; i++) {
                EcfgNode? node = List[i];
                if (node is T t) arr[i] = t;
                else if (node == null) arr[i] = default(T);
                else throw new EcfgException($"Node {node?.GetType()} cannot be cast to {typeof(T)}.");
            }
            return arr;
        }

        public override bool DeepEquals(EcfgNode? node) {
            if (node is EcfgList other) {
                if (other.List.Count != List.Count)
                    return false;
                for (int i = 0; i < List.Count; i++) {
                    if (!(List[i]?.DeepEquals(other.List[i]) ?? other.List[i] == null))
                        return false;
                }
                return true;
            }
            return false;
        }
    }
}