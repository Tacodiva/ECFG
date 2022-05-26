
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace Ecfg {

    internal class EcfgTokenizer {

        internal static readonly char[] Ignore = { ' ', '\t', '\r', '#' };
        internal static readonly char[] IgnoreNewline = { '\n', ' ', '\t', '\r', '#' };
        internal static readonly Dictionary<char, char> EscapeChars = new Dictionary<char, char>() {
            ['n'] = '\n',
            ['r'] = '\r',
            ['t'] = '\t',
            ['0'] = '\0',
            ['\\'] = '\\',
            ['\''] = '\'',
            ['\"'] = '\"',
        };

        internal static bool IsLegalKeyChar(char c) => Char.IsLetterOrDigit(c) || c == '_';
        internal static bool IsLegalLiteralChar(char c) => Char.IsLetterOrDigit(c) || c == '+' || c == '-' || c == '.';
        internal static bool IsDelimiter(char c) => c == '\n' || c == ',';

        internal static bool TryLiteralValueToNode(string val, out EcfgNode? node) {
            switch (val) {
                case "true":
                    node = new EcfgBoolean(true);
                    return true;
                case "false":
                    node = new EcfgBoolean(false);
                    return true;
                case "+infinity":
                    node = new EcfgDouble(Double.PositiveInfinity);
                    return true;
                case "-infinity":
                    node = new EcfgDouble(Double.NegativeInfinity);
                    return true;
                case "nan":
                    node = new EcfgDouble(Double.NaN);
                    return true;
                case "null":
                    node = null;
                    return true;
            }
            node = null;
            return false;
        }

        private readonly string _input;
        private int _location;

        private char _current => _input[_location];
        private int _length => _input.Length;
        private bool _withinBounds => _location < _length;

        public EcfgTokenizer(string input) {
            _input = input;
            _location = 0;
        }

        public EcfgDocument ParseToDocument() {
            return new EcfgDocument(ReadObjectToken(true));
        }

        public EcfgObject ParseToObject() {
            return ReadObject(true);
        }

        private void SyntaxError(string exp) {
            int line = 1, col = 1;
            for (int i = 0; i < _location; i++) {
                if (_input[i] == '\n') {
                    ++line;
                    col = 0;
                }
                ++col;
            }
            throw new EcfgException("Syntax Error: " + exp, line, col);
        }

        private EcfgNodeToken ReadNodeToken() {

            switch (_current) {
                case '{':
                    return ReadObjectToken();
                case '[':
                    return ReadListToken();
                case '\'':
                case '"':
                    return ReadStringToken();
            }

            StringBuilder valSb = new StringBuilder();
            while (IsLegalLiteralChar(_current))
                valSb.Append(_input[_location++]);
            string val = valSb.ToString().ToLower();
            EcfgLiteralToken valToken = new EcfgLiteralToken(valSb.ToString());

            if (TryLiteralValueToNode(val, out _))
                return new EcfgValueToken(new EcfgLiteralToken(valSb.ToString()));

            if (val.StartsWith("0x")) {
                if (long.TryParse(val.Substring(2), NumberStyles.HexNumber, null, out _))
                    return new EcfgHexNumberToken(valToken);
                SyntaxError($"Badly formatted hexadecimal number \"{valSb}\".");
            }

            if (val.Contains(".") || val.Contains("e")) {
                if (double.TryParse(val, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, out _))
                    return new EcfgDecimalNumberToken(valToken);
                SyntaxError($"Badly formatted decimal number \"{valSb}\".");
            }

            if (!long.TryParse(val, NumberStyles.AllowLeadingSign, null, out _))
                SyntaxError($"Unknown literal \"{valSb}\".");

            return new EcfgIntegerNumberToken(valToken);
        }

        private EcfgObjectToken ReadObjectToken(bool allowOutOfBounds = false) {
            if (_current == '{')
                ++_location;
            EcfgObjectToken token = new EcfgObjectToken();

            for (; ; ) {
                EcfgLiteralToken beforeKey = SkipToken(IgnoreNewline);
                if ((allowOutOfBounds && !_withinBounds) || _current == '}') {
                    token.Suffix = beforeKey;
                    ++_location;
                    return token;
                }

                StringBuilder key = new StringBuilder();
                while (IsLegalKeyChar(_current)) key.Append(_input[_location++]);

                if (key.Length == 0) SyntaxError($"Illegal character '{_current}', expected a new key.");
                EcfgLiteralToken keyToken = new EcfgLiteralToken(key.ToString());

                EcfgLiteralToken beforeColon = SkipToken(Ignore);
                if (_current != ':') SyntaxError($"Illegal character '{_current}', expected ':'.");
                ++_location;
                EcfgLiteralToken afterColon = SkipToken(IgnoreNewline);

                EcfgNodeToken node = ReadNodeToken();
                EcfgLiteralToken afterNode = SkipToken(Ignore);
                if (IsDelimiter(_current)) afterNode.Value += _current;

                token.Entries.Add(new EcfgObjectToken.EcfgObjectEntryToken(
                    beforeKey,
                    keyToken,
                    beforeColon, afterColon,
                    node,
                    afterNode
                ));

                if ((allowOutOfBounds && !_withinBounds) || _current == '}') {
                    ++_location;
                    return token;
                }
                if (!IsDelimiter(_current)) SyntaxError($"Expected ',' or newline, got '{_current}'.");
                ++_location;
            }
        }

        private EcfgListToken ReadListToken() {
            ++_location;
            EcfgListToken token = new EcfgListToken();

            for (; ; ) {
                EcfgLiteralToken before = SkipToken(IgnoreNewline);
                if (_current == ']') {
                    token.Suffix = before;
                    ++_location;
                    return token;
                }

                EcfgNodeToken node = ReadNodeToken();
                EcfgLiteralToken after = SkipToken(Ignore);
                if (IsDelimiter(_current)) after.Value += _current;

                token.Entries.Add(new EcfgListToken.EcfgListEntryToken(
                    before, node, after
                ));

                if (_current == ']') {
                    ++_location;
                    return token;
                }
                if (!IsDelimiter(_current))
                    SyntaxError($"Expected ',' or newline, got '{_current}'.");
                ++_location;
            }
        }

        private EcfgStringToken ReadStringToken() {
            char quote = _current;
            ++_location;
            StringBuilder str = new StringBuilder();
            while (_current != quote) {
                str.Append(_current);
                if (_current == '\\')
                    if (!EscapeChars.ContainsKey(_input[_location + 1]))
                        SyntaxError($"Unknown escape character '{_input[_location + 1]}'");
                ++_location;
            }
            ++_location;
            return new EcfgStringToken(quote, new EcfgLiteralToken(str.ToString()));
        }

        private EcfgLiteralToken SkipToken(char[] ignore) {
            char character;
            StringBuilder sb = new StringBuilder();
            while (_location < _length && Array.IndexOf(ignore, character = _current) != -1) {
                sb.Append(_input[_location++]);
                if (character == '#')
                    while (_location < _length && _current != '\n') {
                        sb.Append(_current);
                        ++_location;
                    }
            }
            return new EcfgLiteralToken(sb.ToString());
        }

        private EcfgNode? ReadNode() {
            switch (_current) {
                case '{':
                    return ReadObject();
                case '[':
                    return ReadList();
                case '\'':
                case '"':
                    return ReadString();
            }

            StringBuilder valSb = new StringBuilder();
            while (IsLegalLiteralChar(_current))
                valSb.Append(_input[_location++]);
            string val = valSb.ToString().ToLower();

            if (TryLiteralValueToNode(val, out EcfgNode? node))
                return node;

            if (val.StartsWith("0x")) {
                if (long.TryParse(val.Substring(2), NumberStyles.HexNumber, null, out long vl))
                    return new EcfgLong(vl);
                SyntaxError($"Badly formatted hexadecimal number \"{valSb}\".");
            }

            if (val.Contains(".") || val.Contains("e")) {
                if (double.TryParse(val, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, out double vd))
                    return new EcfgDouble(vd);
                SyntaxError($"Badly formatted decimal number \"{valSb}\".");
            }

            if (long.TryParse(val, NumberStyles.AllowLeadingSign, null, out long v))
                return new EcfgLong(v);

            SyntaxError($"Badly formatted integer number \"{valSb}\".");
            return null!;
        }

        private EcfgObject ReadObject(bool allowOutOfBounds = false) {
            if (_current == '{')
                ++_location;
            EcfgObject obj = new EcfgObject();
            for (; ; ) {
                Skip(IgnoreNewline);
                if ((allowOutOfBounds && !_withinBounds) || _current == '}') {
                    ++_location;
                    return obj;
                }

                StringBuilder key = new StringBuilder();
                while (IsLegalKeyChar(_current)) key.Append(_input[_location++]);

                if (key.Length == 0) SyntaxError($"Illegal character '{_current}', expected a new key.");

                Skip(Ignore);
                if (_current != ':') SyntaxError($"Illegal character '{_current}', expected ':'.");
                ++_location;
                Skip(IgnoreNewline);

                EcfgNode? node = ReadNode();
                Skip(Ignore);

                obj.Children[key.ToString()] = node;

                if ((allowOutOfBounds && !_withinBounds) || _current == '}') {
                    ++_location;
                    return obj;
                }

                if (!IsDelimiter(_current)) SyntaxError($"Expected ',' or newline, got '{_current}'.");
                ++_location;
            }
        }

        private EcfgList ReadList() {
            ++_location;
            EcfgList list = new EcfgList();

            for (; ; ) {
                Skip(IgnoreNewline);
                if (_current == ']') {
                    ++_location;
                    return list;
                }

                EcfgNode? node = ReadNode();
                Skip(Ignore);

                list.List.Add(node);

                if (_current == ']') {
                    ++_location;
                    return list;
                }

                if (!IsDelimiter(_current))
                    SyntaxError($"Expected ',' or newline, got '{_current}'.");
                ++_location;
            }
        }

        private EcfgString ReadString() {
            char quote = _current;
            ++_location;
            StringBuilder str = new StringBuilder();
            while (_current != quote) {
                if (_current == '\\') {
                    if (EscapeChars.TryGetValue(_input[++_location], out char escaped))
                        str.Append(escaped);
                } else
                    str.Append(_current);
                ++_location;
            }
            ++_location;
            return new EcfgString(str.ToString());
        }

        private void Skip(char[] ignore) {
            char character;
            while (_location < _length && Array.IndexOf(ignore, character = _current) != -1) {
                ++_location;
                if (character == '#')
                    while (_location < _length && _current != '\n') ++_location;
            }
        }
    }

}