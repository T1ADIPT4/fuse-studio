﻿using Uno.Compiler;
using Uno.Compiler.Frontend.Parser;
using Outracks.UnoDevelop.CodeNinja.AmbientParser;

namespace Outracks.CodeNinja.Tests
{
    internal class CodeReader : ICodeReader
    {
        private readonly string _s;
        private int _p;
        private readonly Lexer.Token[] _tokens;

        public CodeReader(string s, int caret)
        {
            _s = s;
            _p = caret;
            _tokens = Lexer.Lex(s, false, false, true);
        }

        private TokenType Token(bool peek, bool reverse = false)
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                Lexer.Token token = _tokens[i];
                int ol = token.Offset + token.Length;

                if (ol <= _p) continue;

                if (reverse)
                {
                    if (i <= 0) return TokenType.StartOfFile;

                    token = _tokens[i - 1];
                    if (!peek)
                        _p = token.Offset;
                }
                else
                {
                    if (!peek)
                        _p = i < _tokens.Length - 1 ? ol : _s.Length - 1;
                }
                return token.Type;
            }
            return TokenType.EndOfFile;
        }

        public TokenType PeekTokenReverse()
        {
            return Token(true, true);
        }

        public TokenType ReadTokenReverse()
        {
            return Token(false, true);
        }

        public TokenType ReadToken()
        {
            return Token(false);
        }

        public TokenType PeekToken()
        {
            return Token(true);
        }

        public int Offset
        {
            get { return _p; }
            set { _p = value; }
        }

        public string PeekTextReverse(int charCount)
        {
            return _s.Substring(_p - charCount, charCount);
        }

        public string ReadText(int charCount)
        {
            var r = _s.Substring(_p, charCount);
            _p += charCount;
            return r;
        }

        public string ReadTextReverse(int charCount)
        {
            _p -= charCount;
            return _s.Substring(_p, charCount);
        }

        public string PeekText(int charCount)
        {
            return _s.Substring(_p, charCount);
        }

        public int Length
        {
            get { return _s.Length; }
        }
    }
}