using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConversationCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            String inFile = null;
            String outFile = null;

#region Parse switches
            if (args.Length == 0) 
            {
                Help();
                return;
            }

            for (var i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-in")
                {
                    if (i + 1 < args.Length)
                    {
                        inFile = args[i + 1];
                        ++i;
                    }
                    else
                    {
                        Help();
                        return;
                    }
                }
                else if (args[i] == "-out")
                {
                    if (i + 1 < args.Length)
                    {
                        outFile = args[i + 1];
                        ++i;
                    }
                    else
                    {
                        Help();
                        return;
                    }
                }
                else
                {
                    if (inFile == null) inFile = args[i];
                    else if (outFile == null) outFile = args[i];
                    else
                    {
                        Help();
                        return;
                    }
                }
            }
#endregion

            var source = System.IO.File.OpenText(inFile);

            var Compiler = new QuipCompiler();
            Parser.Parse(source, Compiler);
            source.Close();

            var destination = System.IO.File.OpenWrite(outFile);
            Compiler.Emit(new System.IO.StreamWriter(destination));
            destination.Flush();
            destination.Close();
        }

        static void Help()
        {
            Console.WriteLine("Conversation Compiler 0.1");
            Console.WriteLine("-in  : Specify the source file");
            Console.WriteLine("-out : Specify the destination file");
        }
    }
}
