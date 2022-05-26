using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ecfg {

    internal abstract class EcfgToken {
        public abstract void AppendEcfg(StringBuilder ecfg);
        
        public string ToEcfg() {
            StringBuilder sb = new StringBuilder();
            AppendEcfg(sb);
            return sb.ToString();
        }
    }

    internal abstract class EcfgNodeToken : EcfgToken {
        public abstract EcfgNode? ToEcfgNode();
    }

    internal class EcfgLiteralToken : EcfgToken {
        public string Value;

        public EcfgLiteralToken() {
            Value = "";
        }

        public EcfgLiteralToken(string value) {
            Value = value;
        }

        public override void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append(Value);
        }
    }

    internal class EcfgObjectToken : EcfgNodeToken {

        // (BeforeKey)(Key)(BeforeColon):(AfterColon)(Node)(AfterNode)
        public class EcfgObjectEntryToken : EcfgToken {
            public readonly EcfgLiteralToken BeforeKey;
            public readonly EcfgLiteralToken Key;
            public readonly EcfgLiteralToken BeforeColon;
            public readonly EcfgLiteralToken AfterColon;
            public readonly EcfgNodeToken NodeToken;
            public readonly EcfgLiteralToken AfterNode;

            public EcfgNode? Node;

            public EcfgObjectEntryToken(EcfgLiteralToken beforeKey, EcfgLiteralToken key, EcfgLiteralToken beforeColon, EcfgLiteralToken afterColon, EcfgNodeToken nodeToken, EcfgLiteralToken afterNode) {
                BeforeKey = beforeKey;
                Key = key;
                BeforeColon = beforeColon;
                AfterColon = afterColon;
                NodeToken = nodeToken;
                AfterNode = afterNode;
            }

            public override void AppendEcfg(StringBuilder ecfg) {
                BeforeKey.AppendEcfg(ecfg);
                Key.AppendEcfg(ecfg);
                BeforeColon.AppendEcfg(ecfg);
                ecfg.Append(':');
                AfterColon.AppendEcfg(ecfg);
                NodeToken.AppendEcfg(ecfg);
                AfterNode.AppendEcfg(ecfg);
            }
        }

        // {(Entries...)(Suffix)}
        public List<EcfgObjectEntryToken> Entries;
        public EcfgLiteralToken Suffix;

        public EcfgObjectToken() {
            Entries = new List<EcfgObjectEntryToken>();
            Suffix = new EcfgLiteralToken();
        }

        private void AppendBracketlessEcfg(StringBuilder ecfg) {
            foreach (EcfgObjectEntryToken entry in Entries)
                entry.AppendEcfg(ecfg);
            Suffix.AppendEcfg(ecfg);
        }

        public override void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append('{');
            AppendBracketlessEcfg(ecfg);
            ecfg.Append('}');
        }

        public string ToEcfgDocument() {
            StringBuilder ecfg = new StringBuilder();
            AppendBracketlessEcfg(ecfg);
            return ecfg.ToString();
        }

        public Dictionary<string, EcfgNode?> ToChildren() {
            Dictionary<string, EcfgNode?> children = new Dictionary<string, EcfgNode?>();
            foreach (EcfgObjectEntryToken entry in Entries) {
                children.Add(entry.Key.Value, entry.NodeToken.ToEcfgNode());
            }
            return children;
        }

        public override EcfgNode? ToEcfgNode() {
            return new EcfgObject(ToChildren());
        }
    }

    internal class EcfgListToken : EcfgNodeToken {

        // (Before)(Node)(After)
        public class EcfgListEntryToken : EcfgToken {
            public readonly EcfgLiteralToken Before;
            public readonly EcfgNodeToken NodeToken;
            public readonly EcfgLiteralToken After;

            public EcfgNode? Node;

            public EcfgListEntryToken(EcfgLiteralToken before, EcfgNodeToken nodeToken, EcfgLiteralToken after) {
                Before = before;
                NodeToken = nodeToken;
                After = after;
            }

            public override void AppendEcfg(StringBuilder ecfg) {
                Before.AppendEcfg(ecfg);
                NodeToken.AppendEcfg(ecfg);
                After.AppendEcfg(ecfg);
            }

        }

        // [(Entries...)(Suffix)]
        public List<EcfgListEntryToken> Entries;
        public EcfgLiteralToken Suffix;

        public EcfgListToken() {
            Entries = new List<EcfgListEntryToken>();
            Suffix = new EcfgLiteralToken();
        }

        public override void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append('[');
            foreach (EcfgListEntryToken entry in Entries)
                entry.AppendEcfg(ecfg);
            Suffix.AppendEcfg(ecfg);
            ecfg.Append(']');
        }

        public override EcfgNode? ToEcfgNode() {
            List<EcfgNode?> nodes = new List<EcfgNode?>();
            foreach (EcfgListEntryToken entry in Entries) {
                nodes.Add(entry.NodeToken.ToEcfgNode());
            }
            return new EcfgList(nodes);
        }
    }

    internal abstract class EcfgSingleValueNodeToken : EcfgNodeToken {
        public EcfgLiteralToken Value;

        public EcfgSingleValueNodeToken(EcfgLiteralToken value) {
            Value = value;
        }

        public override void AppendEcfg(StringBuilder ecfg) {
            Value.AppendEcfg(ecfg);
        }
    }

    internal class EcfgValueToken : EcfgSingleValueNodeToken {
        public EcfgValueToken(EcfgLiteralToken value) : base(value) {
        }

        public override EcfgNode? ToEcfgNode() {
            EcfgTokenizer.TryLiteralValueToNode(Value.Value.ToLower(), out EcfgNode? node);
            return node;
        }
    }

    internal class EcfgStringToken : EcfgNodeToken {
        // (Quote)(Value)(Quote)
        public char Quote; // Either `'` or `"`
        public EcfgLiteralToken Value;

        public EcfgStringToken(char quote, EcfgLiteralToken value) {
            Quote = quote;
            Value = value;
        }

        public override void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append(Quote);
            Value.AppendEcfg(ecfg);
            ecfg.Append(Quote);
        }

        public override EcfgNode? ToEcfgNode() {
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < Value.Value.Length; i++) {
                if (Value.Value[i] == '\\')
                    str.Append(EcfgTokenizer.EscapeChars[Value.Value[++i]]);
                else
                    str.Append(Value.Value[i]);
            }
            return new EcfgString(str.ToString());
        }
    }

    internal class EcfgHexNumberToken : EcfgSingleValueNodeToken {
        public EcfgHexNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public override EcfgNode? ToEcfgNode() {
            return new EcfgLong(long.Parse(Value.Value.Substring(2), NumberStyles.HexNumber));
        }
    }

    internal class EcfgDecimalNumberToken : EcfgSingleValueNodeToken {
        public EcfgDecimalNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public override EcfgNode? ToEcfgNode() {
            return new EcfgDouble(double.Parse(Value.Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign));
        }
    }

    internal class EcfgIntegerNumberToken : EcfgSingleValueNodeToken {
        public EcfgIntegerNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public override EcfgNode? ToEcfgNode() {
            return new EcfgLong(long.Parse(Value.Value, NumberStyles.AllowLeadingSign));
        }
    }

}