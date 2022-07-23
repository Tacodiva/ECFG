using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO.Compression;

namespace Ecfg {
    public static class EcfgBin {

        private static readonly byte[] Header = new byte[] { (byte)'E', (byte)'b', (byte)'i', (byte)'n' };
        private static readonly byte Version = 0x00;

        public enum EcfgNodeType : byte {
            Null = 0,
            Boolean = 1,
            Blob = 2,
            Double = 3,
            List = 4,
            Long = 5,
            PosInt = 6,
            NegInt = 7,
            PosShort = 8,
            NegShort = 9,
            Object = 10,
            String = 11,

            NullList = Null + 20,
            BooleanList = Boolean + 20,
            BlobList = Blob + 20,
            DoubleList = Double + 20,
            ListList = List + 20,
            LongList = Long + 20,
            PosIntList = PosInt + 20,
            NegIntList = NegInt + 20,
            PosShortList = PosShort + 20,
            NegShortList = NegShort + 20,
            ObjectList = Object + 20,
            StringList = String + 20,
            ShortList,
            IntList,
            TypedListList,
            EmptyList
        }

        private class KeyInfo {
            public readonly string Name;
            public readonly EcfgNodeType Type;

            public KeyInfo(string name, EcfgNodeType type) {
                Name = name;
                Type = type;
            }

            public KeyInfo(KeyValuePair<string, EcfgNode?> node) : this(node.Key, GetNodeType(node.Value)) { }

            public override int GetHashCode() {
                return new {Name, Type}.GetHashCode();
            }

            public static bool operator ==(KeyInfo lhs, KeyInfo rhs) {
                return lhs.Name == rhs.Name && lhs.Type == rhs.Type;
            }

            public static bool operator !=(KeyInfo lhs, KeyInfo rhs) => !(lhs == rhs);

            public override bool Equals(object? obj) {
                if (ReferenceEquals(this, obj)) {
                    return true;
                }

                if (ReferenceEquals(obj, null)) {
                    return false;
                }

                if (obj is KeyInfo rhs) return this == rhs;
                return false;
            }
        }

        public static byte[] Serialize(EcfgNode? root, bool tryUseCompression = true) {
            MemoryStream memory = new MemoryStream();
            Serialize(root, memory, tryUseCompression);
            return memory.ToArray();
        }

        public static void Serialize(EcfgNode? root, string file, bool tryUseCompression = true) {
            Serialize(root, new FileStream(file, FileMode.Open), tryUseCompression);
        }

        public static void Serialize(EcfgNode? root, Stream stream, bool tryUseCompression = true) {
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryWriter baseWriter = writer;

            List<Tuple<KeyInfo, int>> keyUses = new List<Tuple<KeyInfo, int>>();
            EnumerateKeys(root, keyUses);
            keyUses.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            List<KeyInfo> keys = new List<KeyInfo>(keyUses.Count);
            foreach (Tuple<KeyInfo, int> keyUse in keyUses) keys.Add(keyUse.Item1);

            // Header w/ meme + version info
            writer.Write(Header);
            writer.Write(Version);

            // Root type
            EcfgNodeType rootType = GetNodeType(root);

            if (tryUseCompression) {
                MemoryStream ms = new MemoryStream();
                writer = new BinaryWriter(ms);
            } else {
                // If trying to use compression, root type and compression are written later
                writer.Write((byte)rootType);
            }


            // Serialize keys
            writer.Write7BitEncodedInt(keys.Count);
            for (int i = 0; i < keys.Count; i++) {
                KeyInfo key = keys[i];
                int idx = keyUses.FindIndex(other => other.Item1.Name == key.Name);
                if (idx >= i) idx = -1;

                byte keyTypeByte = (byte)key.Type;
                if (idx != -1) keyTypeByte |= 0x80;
                writer.Write(keyTypeByte);

                if (idx == -1) {
                    WriteStandardASCII(key.Name, writer);
                } else {
                    writer.Write7BitEncodedInt(idx);
                }
            }

            // Serialize root
            SerializeNode(root, rootType, writer, keys);

            // Compression!
            if (tryUseCompression) {
                MemoryStream compressedData = new MemoryStream();

                using (DeflateStream compressor = new DeflateStream(compressedData, CompressionMode.Compress, true)) {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo(compressor);
                }

                bool useCompression;
                Stream dataStream;

                if (compressedData.Length >= writer.BaseStream.Length) {
                    useCompression = false;
                    dataStream = writer.BaseStream;
                } else {
                    useCompression = true;
                    dataStream = compressedData;
                }

                baseWriter.Write((byte)((byte)rootType | (useCompression ? 0x80 : 0x00)));
                dataStream.Position = 0;
                dataStream.CopyTo(stream);
            }
        }

        public static T Deserialize<T>(string file) where T : EcfgNode {
            return Deserialize<T>(new FileStream(file, FileMode.Open));
        }

        public static T Deserialize<T>(byte[] bytes) where T : EcfgNode {
            MemoryStream memory = new MemoryStream(bytes);
            return Deserialize<T>(memory);
        }

        public static T Deserialize<T>(Stream stream) where T : EcfgNode {
            EcfgNode? root = Deserialize(stream);
            if (root is T castRoot)
                return castRoot;
            throw new EcfgException($"Root node is of type {root?.GetType().Name ?? "null"}, not {typeof(T).Name}.");
        }

        public static EcfgNode? Deserialize(Stream stream) {
            BinaryReader reader = new BinaryReader(stream);

            // Validate header
            byte[] header = reader.ReadBytes(4);
            for (int i = 0; i < Header.Length; i++)
                if (header[i] != Header[i])
                    throw new EcfgException($"Invalid EBin header! {header[i].ToString("x")} != {Header[i].ToString("x")}.");
            byte version = reader.ReadByte();
            if (version != Version)
                throw new EcfgException($"Unsupported EBin version {version.ToString("X")}!");

            // Root type + compression
            byte rootTypeByte = reader.ReadByte();
            EcfgNodeType rootType = (EcfgNodeType)(rootTypeByte & 0x7F);
            bool compression = (rootTypeByte & 0x80) != 0;

            if (compression) {
                reader = new BinaryReader(new DeflateStream(stream, CompressionMode.Decompress));
            }

            // Load keys
            int keyCount = reader.Read7BitEncodedInt();
            List<KeyInfo> keys = new List<KeyInfo>(keyCount);
            for (int i = 0; i < keyCount; i++) {
                byte keyTypeByte = reader.ReadByte();
                EcfgNodeType keyType = (EcfgNodeType)(keyTypeByte & 0x7F);
                string keyName;
                if ((keyTypeByte & 0x80) == 0) {
                    keyName = ReadStandardASCII(reader);
                } else {
                    keyName = keys[reader.Read7BitEncodedInt()].Name;
                }
                keys.Add(new KeyInfo(keyName, keyType));
            }

            // Deserialize root
            return DeserializeNode(rootType, reader, keys);
        }

        private static void WriteStandardASCII(string str, BinaryWriter writer) {
            for (int i = 0; i < str.Length; i++) {
                char c = str[i];
                if (c >= 128) throw new EcfgException($"Non-standard ASCII character '{c}' in string.");
                writer.Write((byte)(c | (i == str.Length - 1 ? 0x80 : 0)));
            }
        }

        private static string ReadStandardASCII(BinaryReader reader) {
            byte b = 1;
            StringBuilder sb = new StringBuilder();
            while ((b & 0x80) == 0) {
                b = reader.ReadByte();
                sb.Append((char)(b & 0x7F));
            }
            return sb.ToString();
        }

        private static EcfgNodeType GetNodeType(EcfgNode? node) {
            switch (node) {
                case null:
                    return EcfgNodeType.Null;
                case EcfgBoolean:
                    return EcfgNodeType.Boolean;
                case EcfgBlob:
                    return EcfgNodeType.Blob;
                case EcfgDouble:
                    return EcfgNodeType.Double;
                case EcfgList listNode:
                    if (listNode.List.Count == 0)
                        return EcfgNodeType.EmptyList;

                    long min = 0, max = 0;
                    bool pure = true, onlyLong = true;
                    EcfgNodeType listType = GetNodeType(listNode.List[0]);

                    foreach (EcfgNode? listItem in listNode.List) {
                        if (listItem is EcfgLong longListItem) {
                            if (longListItem.Value < min) min = longListItem.Value;
                            if (longListItem.Value > max) max = longListItem.Value;
                        } else
                            onlyLong = false;
                        if (GetNodeType(listItem) != listType)
                            pure = false;
                        if (!pure && !onlyLong) break;
                    }

                    if (pure) {
                        if (listType < EcfgNodeType.NullList)
                            return listType + 20; // See enum above
                        else
                            return EcfgNodeType.TypedListList;
                    }

                    if (onlyLong) {
                        if (min == 0) {
                            switch (max) {
                                case < ushort.MaxValue:
                                    return EcfgNodeType.PosShortList;
                                case < uint.MaxValue:
                                    return EcfgNodeType.PosIntList;
                            }
                        } else if (max == 0) {
                            switch (-min) {
                                case < ushort.MaxValue:
                                    return EcfgNodeType.NegShortList;
                                case < uint.MaxValue:
                                    return EcfgNodeType.NegIntList;
                            }
                        }
                        if (min >= short.MinValue && max <= short.MaxValue)
                            return EcfgNodeType.ShortList;
                        if (min >= int.MinValue && max <= int.MaxValue)
                            return EcfgNodeType.IntList;
                        return EcfgNodeType.LongList;
                    }

                    return EcfgNodeType.List;
                case EcfgLong longNode:
                    if (longNode >= 0)
                        switch (longNode.Value) {
                            case < ushort.MaxValue:
                                return EcfgNodeType.PosShort;
                            case < uint.MaxValue:
                                return EcfgNodeType.PosInt;
                        } else
                        switch (-longNode.Value) {
                            case < ushort.MaxValue:
                                return EcfgNodeType.NegShort;
                            case < uint.MaxValue:
                                return EcfgNodeType.NegInt;
                        }
                    return EcfgNodeType.Long;
                case EcfgObject:
                    return EcfgNodeType.Object;
                case EcfgString:
                    return EcfgNodeType.String;
            }
            throw new EcfgException($"Can't serialize {node.GetType()}.");
        }

        private static void SerializeNode(EcfgNode? node, EcfgNodeType nodeType, BinaryWriter bin, List<KeyInfo> keys) {
            switch (node) {
                case EcfgBoolean boolNode:
                    bin.Write(boolNode.Value);
                    return;
                case EcfgBlob blobNode:
                    bin.Write7BitEncodedInt(blobNode.Value.Length);
                    bin.Write(blobNode.Value);
                    return;
                case EcfgDouble doubleNode:
                    bin.Write(doubleNode.Value);
                    return;
                case EcfgList listNode:
                    if (nodeType == EcfgNodeType.EmptyList)
                        return;

                    if (nodeType == EcfgNodeType.List) {
                        for (int i = 0; i < listNode.List.Count; i++) {
                            EcfgNode? listItem = listNode.List[i];
                            EcfgNodeType listItemType = GetNodeType(listItem);
                            bin.Write((byte)((byte)listItemType | (i == listNode.List.Count - 1 ? 0x80 : 0)));
                            SerializeNode(listItem, listItemType, bin, keys);
                        }
                        return;
                    }

                    bin.Write7BitEncodedInt(listNode.List.Count);

                    if (nodeType == EcfgNodeType.ShortList || nodeType == EcfgNodeType.IntList) {
                        foreach (EcfgNode? listItem in listNode.List) {
                            EcfgLong longListItem = (EcfgLong)listItem!;
                            if (nodeType == EcfgNodeType.ShortList) {
                                bin.Write((short)longListItem.Value);
                            } else { // EcfgNodeType.IntList
                                bin.Write((int)longListItem.Value);
                            }
                        }
                        return;
                    }

                    EcfgNodeType listType;
                    if (nodeType == EcfgNodeType.TypedListList) {
                        listType = GetNodeType(listNode.List[0]);
                        bin.Write((byte)listType);
                    } else {
                        listType = nodeType - 20;
                    }

                    foreach (EcfgNode? listItem in listNode.List) {
                        SerializeNode(listItem, listType, bin, keys);
                    }
                    return;
                case EcfgLong longNode:
                    switch (nodeType) {
                        case EcfgNodeType.PosShort:
                            bin.Write((ushort)longNode.Value);
                            return;
                        case EcfgNodeType.NegShort:
                            bin.Write((ushort)(-longNode.Value));
                            return;
                        case EcfgNodeType.PosInt:
                            bin.Write((uint)longNode.Value);
                            return;
                        case EcfgNodeType.NegInt:
                            bin.Write((uint)(-longNode.Value));
                            return;
                        case EcfgNodeType.Long:
                            bin.Write(longNode.Value);
                            return;
                    }
                    return;
                case EcfgObject objNode:
                    bin.Write7BitEncodedInt(objNode.Children.Count);
                    foreach (KeyValuePair<string, EcfgNode?> entry in objNode.Entries) {
                        KeyInfo key = new KeyInfo(entry);
                        bin.Write7BitEncodedInt(keys.IndexOf(key));
                        SerializeNode(entry.Value, key.Type, bin, keys);
                    }
                    return;
                case EcfgString stringNode:
                    bin.Write(stringNode);
                    return;
            }
        }

        private static EcfgNode? DeserializeNode(EcfgNodeType nodeType, BinaryReader bin, List<KeyInfo> keys) {
            switch (nodeType) {
                case EcfgNodeType.Null:
                    return null;
                case EcfgNodeType.Blob:
                    return new EcfgBlob(bin.ReadBytes(bin.Read7BitEncodedInt()));
                case EcfgNodeType.Boolean:
                    return new EcfgBoolean(bin.ReadBoolean());
                case EcfgNodeType.Double:
                    return new EcfgDouble(bin.ReadDouble());
                case EcfgNodeType.List:
                    EcfgList list = new EcfgList();
                    while (true) {
                        byte itemTypeByte = bin.ReadByte();
                        EcfgNodeType itemType = (EcfgNodeType)(itemTypeByte & 0x7F);
                        list.List.Add(DeserializeNode(itemType, bin, keys));
                        if ((itemTypeByte & 0x80) != 0)
                            return list;
                    }
                case EcfgNodeType.Long:
                    return new EcfgLong(bin.ReadInt64());
                case EcfgNodeType.PosInt:
                    return new EcfgLong(bin.ReadUInt32());
                case EcfgNodeType.NegInt:
                    return new EcfgLong(-bin.ReadUInt32());
                case EcfgNodeType.PosShort:
                    return new EcfgLong(bin.ReadUInt16());
                case EcfgNodeType.NegShort:
                    return new EcfgLong(-bin.ReadUInt16());
                case EcfgNodeType.Object:
                    EcfgObject obj = new EcfgObject();
                    int childCount = bin.Read7BitEncodedInt();
                    for (int i = 0; i < childCount; i++) {
                        KeyInfo key = keys[bin.Read7BitEncodedInt()];
                        obj.Children[key.Name] = DeserializeNode(key.Type, bin, keys);
                    }
                    return obj;
                case EcfgNodeType.String:
                    return new EcfgString(bin.ReadString());
                case <= EcfgNodeType.StringList:
                case EcfgNodeType.TypedListList:
                    EcfgNodeType listType;
                    if (nodeType == EcfgNodeType.TypedListList) listType = (EcfgNodeType)bin.ReadByte();
                    else listType = nodeType - 20;
                    int length = bin.Read7BitEncodedInt();
                    list = new EcfgList();
                    for (int i = 0; i < length; i++)
                        list.List.Add(DeserializeNode(listType, bin, keys));
                    return list;
                case EcfgNodeType.ShortList:
                case EcfgNodeType.IntList:
                    length = bin.Read7BitEncodedInt();
                    list = new EcfgList();
                    for (int i = 0; i < length; i++) {
                        if (nodeType == EcfgNodeType.ShortList) list.List.Add(new EcfgLong(bin.ReadInt16()));
                        else /* EcfgNodeType.IntList */ list.List.Add(new EcfgLong(bin.ReadInt32()));
                    }
                    return list;
                case EcfgNodeType.EmptyList:
                    return new EcfgList();
            }
            throw new EcfgException($"Unknown data type 0x{nodeType.ToString("X")}.");
        }

        private static void EnumerateKeys(EcfgNode? node, List<Tuple<KeyInfo, int>> keys) {
            switch (node) {
                case EcfgObject obj:
                    foreach (KeyValuePair<string, EcfgNode?> entry in obj.Entries) {
                        KeyInfo key = new KeyInfo(entry);
                        int idx = keys.FindIndex(k => k.Item1 == key);

                        if (idx == -1) {
                            keys.Add(new Tuple<KeyInfo, int>(key, 1));
                        } else {
                            keys[idx] = new Tuple<KeyInfo, int>(key, keys[idx].Item2 + 1);
                        }

                        EnumerateKeys(entry.Value, keys);
                    }
                    break;
                case EcfgList list:
                    foreach (EcfgNode? listNode in list.List)
                        EnumerateKeys(listNode, keys);
                    break;
            }
        }
    }
}