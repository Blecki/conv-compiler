using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConversationCompiler
{
    public class Parser
    {
        public static void Parse(System.IO.StreamReader Stream, QuipCompiler Compiler)
        {
            var file = Stream.ReadToEnd();
            var state = new ParseState { start = 0, end = file.Length, source = file, filename = "" };
            ParseLines(state, Compiler);
        }

        private static bool IsWhitespace(char c)
        {
            return " \t\r\n".Contains(c);
        }

        private static void DevourWhitespace(ParseState state)
        {
            while (!state.AtEnd() && " \t\r\n".Contains(state.Next())) state.Advance();
        }

        private static void DevourSpaces(ParseState state)
        {
            while (!state.AtEnd() && " \t".Contains(state.Next())) state.Advance();
        }

        private static void ParseLines(ParseState state, QuipCompiler Compiler)
        {
            while (true)
            {
                DevourWhitespace(state);
                if (state.AtEnd()) return;
                if (state.Next() == '}')
                {
                    Compiler.EndBlock();
                    state.Advance(1);
                    return;
                }

                if (state.Next() == '@') ParseDeclaration(state, Compiler);
                else if (state.Next() == '#') ParseDirective(state, Compiler);
                else if (state.MatchNext("follows")) ParseFollowsBlock(state, Compiler);
                else if (state.MatchNext("supplies")) ParseSuppliesBlock(state, Compiler);
                else if (state.MatchNext("//")) ParseComment(state);
                else throw new InvalidOperationException("Unknown line type");
            }
        }

        private static void ParseToken(out String token, ParseState state)
        {
            var t = "";
            while (!IsWhitespace(state.Next()))
            {
                t += state.Next();
                state.Advance(1);
            }
            token = t;
        }

        private static void ParseRestOfLine(out String line, ParseState state)
        {
            var t = "";
            while (state.Next() != '\r' && state.Next() != '\n')
            {
                t += state.Next();
                state.Advance(1);
            }
            line = t;
        }

        private static void ParseList(List<String> into, ParseState state)
        {
            DevourSpaces(state);
            while (!state.AtEnd() && state.Next() != '\r' && state.Next() != '\n' && state.Next() != '{' && state.Next() != '}')
            {
                var token = "";
                ParseToken(out token, state);
                into.Add(token);
                DevourSpaces(state);
            }
        }

        private static void ParseComment(ParseState state)
        {
            var comment = "";
            ParseRestOfLine(out comment, state);
        }

        private static void ParseDeclaration(ParseState state, QuipCompiler Compiler)
        {
            state.Advance(1); //skip opening '@'.
            var ID = "";
            var Name = "";
            if (!IsWhitespace(state.Next()))
                ParseToken(out ID, state);
            ParseRestOfLine(out Name, state);
            Name = Name.Trim();
            if (String.IsNullOrEmpty(Name)) throw new InvalidOperationException("Quip declared with no name");
            var lastChar = Name[Name.Length - 1];
            var Type = QuipType.Questioning;
            if (lastChar == '?') { Name = Name.Substring(0, Name.Length - 1); Type = QuipType.Questioning; }
            else if (lastChar == '.') { Name = Name.Substring(0, Name.Length - 1); Type = QuipType.Informative; }
            else if (lastChar == '!') { Name = Name.Substring(0, Name.Length - 1); Type = QuipType.Performative; }
            Compiler.BeginQuip(ID, Name, Type);
        }

        private static void ParseDirective(ParseState state, QuipCompiler Compiler)
        {
            state.Advance(1); //skip opening '#'.
            var Command = "";
            if (!IsWhitespace(state.Next()))
                ParseToken(out Command, state);

            if (String.IsNullOrEmpty(Command) || Command == "comment" || Command == "response")
            {
                var Text = "";
                ParseRestOfLine(out Text, state);
                if (Command == "comment") Compiler.CommentDirective(Text);
                else if (Command == "response") Compiler.ResponseDirective(Text);
                else Compiler.BlankDirective(Text);
            }
            else if (Command == "follows")
            {
                var TokenList = new List<String>();
                ParseList(TokenList, state);
                Compiler.FollowsDirective(TokenList);
            }
            else if (Command == "supplies")
            {
                var TokenList = new List<String>();
                ParseList(TokenList, state);
                Compiler.SuppliesDirective(TokenList);
            }
            else
            {
                throw new InvalidOperationException("Unknown directive: " + Command);
            }
        }

        private static void ParseFollowsBlock(ParseState state, QuipCompiler Compiler)
        {
            var token = "";
            ParseToken(out token, state); //Skip the word 'follows'.
            var TokenList = new List<String>();
            ParseList(TokenList, state);
            Compiler.BeginFollowsBlock(TokenList);
            DevourWhitespace(state);
            if (state.Next() != '{') throw new InvalidOperationException("Expected {");
            state.Advance(1);
            ParseLines(state, Compiler);
        }

        private static void ParseSuppliesBlock(ParseState state, QuipCompiler Compiler)
        {
            var token = "";
            ParseToken(out token, state); //Skip the word 'follows'.
            var TokenList = new List<String>();
            ParseList(TokenList, state);
            Compiler.BeginSuppliesBlock(TokenList);
            DevourWhitespace(state);
            if (state.Next() != '{') throw new InvalidOperationException("Expected {");
            state.Advance(1);
            ParseLines(state, Compiler);
        }
    }
}
