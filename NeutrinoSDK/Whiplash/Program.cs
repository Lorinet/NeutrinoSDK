using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Whiplash
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Start(args);
        }

        int tab = 0;
        int lc = 0;
        List<string> il = new List<string>();
        Dictionary<string, Regex> reg = new Dictionary<string, Regex>();
        Stack<string> meth = new Stack<string>();
        List<string> vars = new List<string>();
        Dictionary<string, List<string>> methcode = new Dictionary<string, List<string>>();
        public void Start(string[] args)
        {
            Console.WriteLine("Neutrino Python Compiler");
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: whiplash <file.py> <out.ns>");
                Environment.Exit(-1);
            }
            string infile = args[0];
            string outfile = args[1];
            List<string> lines = new List<string>(File.ReadAllLines(infile));
            reg.Add("import", new Regex("\\s*import\\s+(\\w+)\\s*"));
            reg.Add("def", new Regex("\\s*def\\s+(\\w+)\\s*[(]((\\s*\\w+\\s*[,]{0,1}\\s*)*)[)]:"));
            reg.Add("return", new Regex("\\s*return\\s*"));
            reg.Add("assign_var_var", new Regex("\\s*(\\w+)\\s*=\\s*(\\w+)"));
            reg.Add("method_call", new Regex("\\s*(\\w+)\\s*[(](((\\s*\\w+\\s*[,]{0,1}\\s*){0,1}[,]{0,1}(\"(\\s*[\\w\\\\]*\\s*)*\"){0,1}[,]{0,1}\\s*)*)[)]"));
            Match m;
            for (int i = 0; i < lines.Count; i++)
            {
                string ln = lines[i];
                if (ln.StartsWith("\t")) tab = ln.Split('\t').Length - 1;
                else tab = ln.Split("    ").Length - 1;
                foreach(string g in reg.Keys)
                {
                    m = reg[g].Match(lines[i]);
                    if(m.Success)
                    {
                        switch (g)
                        {
                            case "import":
                                il.Add("#include " + m.Groups[1] + ".ns");
                                break;
                            case "def":
                                meth.Push(m.Groups[1].ToString());
                                methcode.Add(m.Groups[1].ToString(), new List<string>());
                                string[] cargs = Regex.Split(m.Groups[2].ToString(), "\\s*[,]\\s*");
                                for (int ax = cargs.Length - 1; ax >= 0; ax--)
                                {
                                    methcode[meth.Peek()].Add("pop " + meth.Peek() + "!" + cargs[ax]);
                                    vars.Add(meth.Peek() + "!" + cargs[ax]);
                                }
                                break;
                            case "return":
                                methcode[meth.Peek()].Add("ret");
                                meth.Pop();
                                break;
                            case "assign_var_var":
                                methcode[meth.Peek()].Add("mov " + GetVarName(m.Groups[1].ToString()) + " " + GetVarName(m.Groups[2].ToString()));
                                break;
                            case "method_call":
                                for(int j = 0; j < lines[i].Length; j++)
                                {
                                    if (lines[i][0] != ' ') break;
                                    lines[i] = lines[i].Remove(0, 1);
                                }
                                if(!lines[i].StartsWith("def")) ParseMethodCall(lines[i]);
                                break;
                        }
                    }
                }
            }
            foreach(string k in methcode.Keys)
            {
                il.Add(":" + k);
                il.AddRange(methcode[k]);
            }
            File.WriteAllLines(outfile, il.ToArray());
        }
        string GetVarName(string var)
        {
            string nm = "";
            for(int i = 0; i < meth.Count; i++)
            {
                nm = meth.ToArray()[i] + "!" + var;
                if (vars.Contains(nm)) break;
                else nm = "";
            }
            return nm;
        }
        void ParseMethodCall(string t)
        {
            Token k = new Token(t);
            return;
        }
    }
    class Token
    {
        List<Token> Tokens { get; set; }
        string Name { get; set; }
        string Text { get; set; }
        public Token(string t)
        {
            Text = t;
            string ct = "";
            string bound = "";
            Tokens = new List<Token>();
            string name = "";
            bool pastName = false;
            bool cp = false;
            for(int i = 0; i < t.Length; i++)
            {
                if (pastName)
                {
                    ct += t[i];
                    if ((bound.Length == 0 || (bound.Length > 0 && bound[bound.Length - 1] != '"')) && t[i] == '(')
                    {
                        bound += '(';
                        cp = true;
                    }
                    else if (t[i] == '"' && t[i - i] != '\\')
                    {
                        if (bound.Length > 0 && bound[bound.Length - 1] == '"' && t[i] == '"' && t[i - 1] != '\\')
                            bound = bound.Remove(bound.Length - 1);
                        else bound += '"';
                    }
                    else if (bound.Length > 0 && bound[bound.Length - 1] == '(' && t[i] == ')')
                        bound = bound.Remove(bound.Length - 1);
                    if (bound.Length == 0 && (t[i] == ',' || (!cp && t[i] == ')')))
                    {
                        if(ct[ct.Length - 1] == ',' || (!cp && ct[ct.Length - 1] == ')')) ct = ct.Remove(ct.Length - 1);
                        for (int j = 0; j < ct.Length; j++)
                        {
                            if (ct[0] != ' ') break;
                            ct = ct.Remove(0, 1);
                        }
                        if (ct == Text) return;
                        Tokens.Add(new Token(ct));
                        ct = "";
                        cp = false;
                    }
                }
                else
                {
                    if(t[i] == '(')
                    {
                        pastName = true;
                    }
                    else name += t[i];
                }
            }
            Name = name.Trim();
        }
    }
}
