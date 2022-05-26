using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ecfg {

    internal interface EcfgToken {
        public void AppendEcfg(StringBuilder ecfg);
        
        public string ToEcfg() {
            StringBuilder sb = new StringBuilder();
            AppendEcfg(sb);
            return sb.ToString();
        }
    }

    internal interface EcfgNodeToken : EcfgToken {
        public EcfgNode? ToEcfgNode();
    }

    internal class EcfgLiteralToken : EcfgToken {
        public string Value;

        public EcfgLiteralToken() {
            Value = "";
        }

        public EcfgLiteralToken(string value) {
            Value = value;
        }

        public void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append(Value);
        }
    }

    internal class EcfgObjectToken : EcfgNodeToken {

        // (BeforeKey)(Key)(BeforeColon):(AfterColon)(Node)(AfterNode)
        public record EcfgObjectEntryToken(
            EcfgLiteralToken BeforeKey,
            EcfgLiteralToken Key,
            EcfgLiteralToken BeforeColon,
            EcfgLiteralToken AfterColon,
            EcfgNodeToken NodeToken,
            EcfgLiteralToken AfterNode) : EcfgToken {

            public EcfgNode? Node;

            public void AppendEcfg(StringBuilder ecfg) {
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

        public void AppendEcfg(StringBuilder ecfg) {
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

        public EcfgNode? ToEcfgNode() {
            return new EcfgObject(ToChildren());
        }
    }

    internal class EcfgListToken : EcfgNodeToken {

        // (Before)(Node)(After)
        public record EcfgListEntryToken(
            EcfgLiteralToken Before,
            EcfgNodeToken NodeToken,
            EcfgLiteralToken After) : EcfgToken {

            public EcfgNode? Node;

            public void AppendEcfg(StringBuilder ecfg) {
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

        public void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append('[');
            foreach (EcfgListEntryToken entry in Entries)
                entry.AppendEcfg(ecfg);
            Suffix.AppendEcfg(ecfg);
            ecfg.Append(']');
        }

        public EcfgNode? ToEcfgNode() {
            List<EcfgNode?> nodes = new List<EcfgNode?>();
            foreach (EcfgListEntryToken entry in Entries) {
                nodes.Add(entry.NodeToken.ToEcfgNode());
            }
            return new EcfgList(nodes);
        }
    }

    internal abstract class EcfgSingleValueToken : EcfgToken {
        public EcfgLiteralToken Value;

        public EcfgSingleValueToken(EcfgLiteralToken value) {
            Value = value;
        }

        public void AppendEcfg(StringBuilder ecfg) {
            Value.AppendEcfg(ecfg);
        }
    }

    internal class EcfgValueToken : EcfgSingleValueToken, EcfgNodeToken {
        public EcfgValueToken(EcfgLiteralToken value) : base(value) {
        }

        public EcfgNode? ToEcfgNode() {
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

        public void AppendEcfg(StringBuilder ecfg) {
            ecfg.Append(Quote);
            Value.AppendEcfg(ecfg);
            ecfg.Append(Quote);
        }

        public EcfgNode? ToEcfgNode() {
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

    internal class EcfgHexNumberToken : EcfgSingleValueToken, EcfgNodeToken {
        public EcfgHexNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public EcfgNode? ToEcfgNode() {
            return new EcfgLong(long.Parse(Value.Value.Substring(2), NumberStyles.HexNumber));
        }
    }

    internal class EcfgDecimalNumberToken : EcfgSingleValueToken, EcfgNodeToken {
        public EcfgDecimalNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public EcfgNode? ToEcfgNode() {
            return new EcfgDouble(double.Parse(Value.Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign));
        }
    }

    internal class EcfgIntegerNumberToken : EcfgSingleValueToken, EcfgNodeToken {
        public EcfgIntegerNumberToken(EcfgLiteralToken value) : base(value) {
        }

        public EcfgNode? ToEcfgNode() {
            return new EcfgLong(long.Parse(Value.Value, NumberStyles.AllowLeadingSign));
        }
    }

}