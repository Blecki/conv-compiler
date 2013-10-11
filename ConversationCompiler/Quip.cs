using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConversationCompiler
{
    public enum QuipType
    {
        Questioning,
        Informative,
        Performative
    }

    public class Quip
    {
        public String ID;
        public String Name;
        public QuipType Type;
        public String Comment;
        public String Response;
        public List<Quip> Follows;
        public List<String> Supplies;
    }
}
