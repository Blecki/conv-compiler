using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConversationCompiler
{
    public class QuipCompiler
    {
        private Dictionary<String, Quip> Quips = new Dictionary<string, Quip>();
        private List<Quip> AllQuips = new List<Quip>();
        private Quip OpenQuip = null;
        private Dictionary<String, String> GlobalDirectives = new Dictionary<string, string>();

        private int GeneratedID = 0;
        private String NextGeneratedID { get { return String.Format("Quip{0,6:000000}", GeneratedID++); } }

        public QuipCompiler(int BaseID)
        {
            GeneratedID = BaseID;
        }

        private class BlockState
        {
            internal Quip SavedOpenQuip = null;
            internal virtual void ApplyState(Quip to) { throw new NotImplementedException(); }
        }

        private class SuppliesBlock : BlockState
        {
            internal List<String> supplies;
            internal override void ApplyState(Quip to)
            {
                if (to.Supplies == null) to.Supplies = new List<string>();
                to.Supplies.AddRange(supplies);
            }
        }

        private class FollowsBlock : BlockState
        {
            internal List<Quip> follows;
            internal override void ApplyState(Quip to)
            {
                to.Directly = null; //Override any directly-following going on.
                /*if (to.Follows == null)*/ to.Follows = new List<Quip>();
                to.Follows.AddRange(follows);
            }
        }

        private class DirectlyBlock : BlockState
        {
            internal List<Quip> directly;
            internal override void ApplyState(Quip to)
            {
                to.Follows = null; //Override any indirectly-following going on.
                to.Directly = new List<Quip>();
                to.Directly.AddRange(directly);
            }
        }

        private List<BlockState> BlockStateStack = new List<BlockState>();

        public void GlobalDirective(String name, String value)
        {
            if (GlobalDirectives.ContainsKey(name)) GlobalDirectives[name] = value;
            else GlobalDirectives.Add(name, value);
        }

        public void BeginQuip(String ID, String Name, QuipType Type, bool Repeatable, bool Restrictive)
        {
            OpenQuip = new Quip();
            if (!String.IsNullOrEmpty(ID))
            {
                if (Quips.ContainsKey(ID)) Quips[ID] = OpenQuip;
                else Quips.Add(ID, OpenQuip);
            }
            AllQuips.Add(OpenQuip);
            OpenQuip.Name = Name;
            OpenQuip.Type = Type;
            OpenQuip.Repeatable = Repeatable;
            OpenQuip.Restrictive = Restrictive;
            OpenQuip.ID = NextGeneratedID;

            if (BlockStateStack.Count > 0)
               foreach (var state in BlockStateStack)
                    state.ApplyState(OpenQuip);
        }

        public void CommentDirective(String text)
        {
            if (OpenQuip.Type == QuipType.NpcDirected) 
                throw new InvalidOperationException("NPC-Directed quips do not have comments.");
            OpenQuip.Comment = ExpandEmbeddedNames(text.Trim());
        }

        public void ResponseDirective(String text)
        {
            OpenQuip.Response = ExpandEmbeddedNames(text.Trim());
        }

        public void NagDirective(String text)
        {
            OpenQuip.Nag = ExpandEmbeddedNames(text.Trim());
        }

        public void BlankDirective(String text)
        {
            if (OpenQuip.Type != QuipType.NpcDirected && String.IsNullOrEmpty(OpenQuip.Comment)) CommentDirective(text);
            else ResponseDirective(text);
        }

        public void FollowsDirective(List<String> names)
        {
            if (OpenQuip.Follows == null) OpenQuip.Follows = new List<Quip>();
            foreach (var name in names)
                if (Quips.ContainsKey(name)) OpenQuip.Follows.Add(Quips[name]);
        }

        public void DirectlyDirective(List<String> names)
        {
            if (OpenQuip.Directly == null) OpenQuip.Directly = new List<Quip>();
            foreach (var name in names)
                if (Quips.ContainsKey(name)) OpenQuip.Directly.Add(Quips[name]);
        }

        public void SuppliesDirective(List<String> names)
        {
            if (OpenQuip.Supplies == null) OpenQuip.Supplies = new List<string>();
            OpenQuip.Supplies.AddRange(names);
        }

        public void UnavailableDirective(String condition)
        {
            if (OpenQuip.OffLimitsRules == null) OpenQuip.OffLimitsRules = new List<string>();
            OpenQuip.OffLimitsRules.Add(condition);
        }

        public void BeginSuppliesBlock(List<String> names)
        {
            var block = new SuppliesBlock();
            block.supplies = names;
            block.SavedOpenQuip = OpenQuip;
            BlockStateStack.Add(block);
        }

        public void BeginFollowsBlock(List<String> names)
        {
            var block = new FollowsBlock();
            block.follows = new List<Quip>();
            foreach (var name in names)
                if (Quips.ContainsKey(name)) block.follows.Add(Quips[name]);
            if (names.Count == 0) block.follows.Add(OpenQuip);
            block.SavedOpenQuip = OpenQuip;
            BlockStateStack.Add(block);
        }

        public void BeginDirectlyBlock(List<String> names)
        {
            var block = new DirectlyBlock();
            block.directly = new List<Quip>();
            foreach (var name in names)
                if (Quips.ContainsKey(name)) block.directly.Add(Quips[name]);
            if (names.Count == 0) block.directly.Add(OpenQuip);
            block.SavedOpenQuip = OpenQuip;
            BlockStateStack.Add(block);
        }

        public void EndBlock()
        {
            if (BlockStateStack.Count > 0)
            {
                OpenQuip = BlockStateStack[BlockStateStack.Count - 1].SavedOpenQuip;
                BlockStateStack.RemoveAt(BlockStateStack.Count - 1);
            }
        }

        public void Emit(System.IO.StreamWriter writer)
        {
            writer.WriteLine("[Quips compiled by Conversation Compiler]");

            foreach (var quip in AllQuips)
            {
                writer.WriteLine("");

                writer.Write(quip.ID + " is a privately-named ");
                if (quip.Repeatable) writer.Write("repeatable ");
                if (quip.Restrictive) writer.Write("restrictive ");
                switch (quip.Type)
                {
                    case QuipType.Questioning:
                        writer.WriteLine("questioning quip.");
                        break;
                    case QuipType.Informative:
                        writer.WriteLine("informative quip.");
                        break;
                    case QuipType.Performative:
                        writer.WriteLine("performative quip.");
                        break;
                    case QuipType.NpcDirected:
                        writer.WriteLine("npc-directed quip.");
                        break;
                }

                if (quip.Type != QuipType.NpcDirected)
                {
                    writer.WriteLine("The printed name is \"" + quip.Name + "\".");
                    var nameTokens = quip.Name.Split(new char[] { ' ' });
                    writer.Write("Understand ");
                    for (var i = 0; i < nameTokens.Length; ++i)
                    {
                        writer.Write("\"" + nameTokens[i].TrimEnd('.') + "\"");
                        if (i != nameTokens.Length - 1) writer.Write(", ");
                    }
                    writer.WriteLine(" as " + quip.ID + ".");

                    if (!String.IsNullOrEmpty(quip.Comment))
                        writer.WriteLine("The comment is \"" + Escape(quip.Comment) + "\".");

                    if (!String.IsNullOrEmpty(quip.Response))
                        writer.WriteLine("The response is \"" + Escape(quip.Response) + "\".");

                    if (!String.IsNullOrEmpty(quip.Nag))
                        writer.WriteLine("The nag is \"" + Escape(quip.Nag) + "\".");

                    if (quip.Supplies != null && quip.Supplies.Count > 0)
                    {
                        writer.Write("It quip-supplies ");
                        for (var i = 0; i < quip.Supplies.Count; ++i)
                        {
                            writer.Write(quip.Supplies[i]);
                            if (i != quip.Supplies.Count - 1) writer.Write(", ");
                        }
                        writer.WriteLine(".");
                    }

                    if (quip.Follows != null && quip.Follows.Count > 0)
                    {
                        writer.Write("It indirectly-follows ");
                        for (var i = 0; i < quip.Follows.Count; ++i)
                        {
                            writer.Write(quip.Follows[i].ID + " [" + quip.Follows[i].Name + "]");

                            if (i != quip.Follows.Count - 1) writer.Write(", ");
                        }
                        writer.WriteLine(".");
                    }

                    if (quip.Directly != null && quip.Directly.Count > 0)
                    {
                        writer.Write("It directly-follows ");
                        for (var i = 0; i < quip.Directly.Count; ++i)
                        {
                            writer.Write(quip.Directly[i].ID + " [" + quip.Directly[i].Name + "]");

                            if (i != quip.Directly.Count - 1) writer.Write(", ");
                        }
                        writer.WriteLine(".");
                    }

                    if (quip.OffLimitsRules != null && quip.OffLimitsRules.Count > 0)
                    {
                        writer.WriteLine("An availability rule for " + quip.ID + ":");
                        foreach (var entry in quip.OffLimitsRules)
                            writer.WriteLine("\tIf " + entry + ", it is off-limits;");
                        writer.WriteLine();
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(quip.Response))
                        writer.WriteLine("The response is \"" + Escape(quip.Response) + "\".");

                    if (!String.IsNullOrEmpty(quip.Nag))
                        writer.WriteLine("The nag is \"" + Escape(quip.Nag) + "\".");
                }
            }


            writer.Flush();
        }

        private static String Escape(String s)
        {
            return s.Replace('"', '\'');
        }

        private String ExpandEmbeddedNames(String s)
        {
            int place = 0;
            var r = new StringBuilder();
            while (place < s.Length)
            {
                if (s[place] == '<') //Found a name.
                {
                    int end = place;
                    while (end < s.Length && s[end] != '>') ++end;
                    if (end == s.Length) throw new InvalidOperationException("Incomplete name escape");
                    var name = s.Substring(place + 1, end - place - 1);
                    place = end;
                    if (Quips.ContainsKey(name))
                        r.Append(Quips[name].ID);
                    else
                        throw new InvalidOperationException("Unknown quip " + name);
                }
                else
                    r.Append(s[place]);

                ++place;
            }

            return r.ToString();
        }
    }
}
