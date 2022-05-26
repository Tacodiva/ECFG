using System;
using System.Collections.Generic;
using System.Text;

namespace Ecfg {

    internal class EcfgRetoknizer {

        internal static readonly Dictionary<char, char> ReverseEscapeChars = new Dictionary<char, char>();
        static EcfgRetoknizer() {
            foreach (KeyValuePair<char, char> entry in EcfgTokenizer.EscapeChars) ReverseEscapeChars[entry.Value] = entry.Key;
        }

        public static string Retoknize(EcfgNode? node) {
            return ToToken(node, 0).ToEcfg();
        }

        private static string IndentString(int indentation) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < indentation; i++) sb.Append("  ");
            return sb.ToString();
        }

        private static EcfgNodeToken ToToken(EcfgNode? node, int indentation) {
            string indent = IndentString(indentation);
            switch (node) {
                case EcfgBlob:
                    throw new EcfgException("Cannot convert blobs to text format!");
                case EcfgBoolean boolNode:
                    if (boolNode.Value) return new EcfgValueToken(new EcfgLiteralToken("True"));
                    else return new EcfgValueToken(new EcfgLiteralToken("False"));
                case EcfgDocument docNode:
                    return docNode._rootToken;
                case EcfgDouble doubleNode:
                    if (double.IsNaN(doubleNode.Value))
                        return new EcfgValueToken(new EcfgLiteralToken("NaN"));
                    if (double.IsPositiveInfinity(doubleNode.Value))
                        return new EcfgValueToken(new EcfgLiteralToken("+Infinity"));
                    if (double.IsNegativeInfinity(doubleNode.Value))
                        return new EcfgValueToken(new EcfgLiteralToken("-Infinity"));
                    if ((long)doubleNode.Value == doubleNode.Value)
                        return new EcfgDecimalNumberToken(new EcfgLiteralToken(doubleNode.Value.ToString("N1")));
                    else
                        return new EcfgDecimalNumberToken(new EcfgLiteralToken(doubleNode.Value.ToString()));
                case EcfgList listNode:
                    EcfgListToken listToken = new EcfgListToken();
                    string childIndent = IndentString(indentation + 1);
                    foreach (EcfgNode? child in listNode.List) {
                        listToken.Entries.Add(new EcfgListToken.EcfgListEntryToken(
                            new EcfgLiteralToken(childIndent),
                            ToToken(child, indentation + 1),
                            new EcfgLiteralToken(",\n")
                        ));
                    }
                    if (listNode.List.Count != 0) {
                        listToken.Suffix = new EcfgLiteralToken(indent);
                        // Add a newline between the open [ and the first entry
                        listToken.Entries[0] = listToken.Entries[0] with { Before = new EcfgLiteralToken("\n" + childIndent) };
                        // Remove the comma after the last entry
                        listToken.Entries[listToken.Entries.Count - 1] = listToken.Entries[listToken.Entries.Count - 1]
                            with { After = new EcfgLiteralToken("\n") };
                    }
                    return listToken;
                case EcfgLong longNode:
                    return new EcfgIntegerNumberToken(new EcfgLiteralToken(longNode.Value.ToString()));
                case EcfgObject objNode:
                    EcfgObjectToken objToken = new EcfgObjectToken();
                    foreach (KeyValuePair<string, EcfgNode?> child in objNode.Children) {
                        objToken.Entries.Add(CreateObjectEntryToken(new EcfgLiteralToken(child.Key), child.Value, indentation));
                    }
                    if (!objNode.Empty) {
                        objToken.Suffix = new EcfgLiteralToken(indent);
                        // Add a newline between the open { and the first entry
                        objToken.Entries[0] = objToken.Entries[0] with { BeforeKey = new EcfgLiteralToken("\n" + IndentString(indentation + 1)) };
                        // Remove the comma after the last entry
                        objToken.Entries[objToken.Entries.Count - 1] = objToken.Entries[objToken.Entries.Count - 1]
                            with { AfterNode = new EcfgLiteralToken("\n") };
                    }
                    return objToken;
                case EcfgString strNode:
                    char quote = strNode.Value.Contains('"') ? '\'' : '"';
                    char otherQuote = quote == '"' ? '\'' : '"';
                    StringBuilder literalString = new StringBuilder();
                    foreach (char c in strNode.Value) {
                        if (c != otherQuote && ReverseEscapeChars.TryGetValue(c, out char escape)) {
                            literalString.Append('\\');
                            literalString.Append(escape);
                        } else
                            literalString.Append(c);
                    }
                    return new EcfgStringToken(quote, new EcfgLiteralToken(literalString.ToString()));
                case null:
                    return new EcfgValueToken(new EcfgLiteralToken("Null"));
            }
            throw new EcfgException($"Unknown node type {node.GetType()}.");
        }

        internal static EcfgObjectToken.EcfgObjectEntryToken CreateObjectEntryToken(EcfgLiteralToken key, EcfgNode? node, int indent) {
            return new EcfgObjectToken.EcfgObjectEntryToken(
                new EcfgLiteralToken(IndentString(indent + 1)),
                key,
                new EcfgLiteralToken(),
                new EcfgLiteralToken(" "),
                ToToken(node, indent + 1),
                new EcfgLiteralToken(",\n")
            );

        }
    }
}