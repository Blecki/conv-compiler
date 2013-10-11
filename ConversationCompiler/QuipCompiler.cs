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
        private String NextGeneratedID { get { return "Quip" + (GeneratedID++); } }

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
                /*if (to.Follows == null)*/ to.Follows = new List<Quip>();
                to.Follows.AddRange(follows);
            }
        }

        private List<BlockState> BlockStateStack = new List<BlockState>();

        public void GlobalDirective(String name, String value)
        {
            if (GlobalDirectives.ContainsKey(name)) GlobalDirectives[name] = value;
            else GlobalDirectives.Add(name, value);
        }

        public void BeginQuip(String ID, String Name, QuipType Type)
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

            if (BlockStateStack.Count > 0)
               foreach (var state in BlockStateStack)
                    state.ApplyState(OpenQuip);
        }

        public void CommentDirective(String text)
        {
            OpenQuip.Comment = text.Trim();
        }

        public void ResponseDirective(String text)
        {
            OpenQuip.Response = text.Trim();
        }

        public void BlankDirective(String text)
        {
            if (String.IsNullOrEmpty(OpenQuip.Comment)) CommentDirective(text);
            else ResponseDirective(text);
        }

        public void FollowsDirective(List<String> names)
        {
            if (OpenQuip.Follows == null) OpenQuip.Follows = new List<Quip>();
            foreach (var name in names)
                if (Quips.ContainsKey(name)) OpenQuip.Follows.Add(Quips[name]);
        }

        public void SuppliesDirective(List<String> names)
        {
            if (OpenQuip.Supplies == null) OpenQuip.Supplies = new List<string>();
            OpenQuip.Supplies.AddRange(names);
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

            var id = 0;
            foreach (var quip in AllQuips)
            {
                quip.ID = String.Format("Quip{0,4:0000}", id);
                ++id;
            }

            foreach (var quip in AllQuips)
            {
                writer.WriteLine("");

                switch (quip.Type)
                {
                    case QuipType.Questioning:
                        writer.WriteLine(quip.ID + " is a privately-named questioning quip.");
                        break;
                    case QuipType.Informative:
                        writer.WriteLine(quip.ID + " is an privately-named informative quip.");
                        break;
                    case QuipType.Performative:
                        writer.WriteLine(quip.ID + " is a privately-named performative quip.");
                        break;
                }

                writer.WriteLine("The printed name is \"" + quip.Name + "\".");
                var nameTokens = quip.Name.Split(new char[] { ' ' });
                writer.Write("Understand ");
                for (var i = 0; i < nameTokens.Length; ++i)
                {
                    writer.Write("\"" + nameTokens[i] + "\"");
                    if (i != nameTokens.Length - 1) writer.Write(", ");
                }
                writer.WriteLine(" as " + quip.ID + ".");

                if (!String.IsNullOrEmpty(quip.Comment))
                    writer.WriteLine("The comment is \"" + Escape(quip.Comment) + "\".");
                if (!String.IsNullOrEmpty(quip.Response))
                    writer.WriteLine("The response is \"" + Escape(quip.Response) + "\".");

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
            }


            writer.Flush();
        }

        private static String Escape(String s)
        {
            return s.Replace('"', '\'');
        }
    }
}
