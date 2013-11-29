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
        Performative,
        NpcDirected
    }

    public class Quip
    {
        public String ID;
        public String Name;
        public QuipType Type;
        public bool Repeatable;
        public bool Restrictive;
        public String Comment;
        public String Response;
        public String Nag;
        public List<Quip> Follows;
        public List<Quip> Directly;
        public List<String> Supplies;
        public List<String> OffLimitsRules;
    }
}
