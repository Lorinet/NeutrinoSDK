using System;
using System.Collections.Generic;
using System.Dynamic;
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
        int prevTab = 0;
        bool retp = false;
        List<string> il = new List<string>();
        Dictionary<string, Regex> reg = new Dictionary<string, Regex>();
        Stack<Block> meth = new Stack<Block>();
        List<string> vars = new List<string>();
        Dictionary<string, List<string>> methcode = new Dictionary<string, List<string>>();
        public void Start(string[] args)
        {
            Console.WriteLine("Neutrino Python Compiler");
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: whiplash <file.py> <out.ns>");
                Environment.Exit(-1);
            }
            string infile = args[0];
            string outfile = args[1];
            List<string> lines = new List<string>(File.ReadAllLines(infile));
            lines.Add("");
            reg.Add("import", new Regex("^\\s*import\\s+(\\w+)\\s*$"));
            reg.Add("def", new Regex("^\\s*def\\s+(\\w+)\\s*[(]((\\s*\\w+\\s*[,]{0,1}\\s*)*)[)]:$"));
            reg.Add("return", new Regex("^\\s*return\\s*$"));
            reg.Add("return_val", new Regex("^\\s*return\\s+(.+)$"));
            reg.Add("assign_var_var", new Regex("^\\s*(\\w+)\\s*=\\s*(\\w+)$"));
            reg.Add("assign_var_call", new Regex("^\\s*(\\w+)\\s*=\\s*([\\s\\w(),=]+[(]+[\\s\\w(),]+)$"));
            reg.Add("method_call", new Regex("^\\s*(\\w+)\\s*[(](((\\s*\\w+\\s*[,]{0,1}\\s*){0,1}[,]{0,1}(\"(\\s*[\\w\\\\]*\\s*)*\"){0,1}[,]{0,1}\\s*)*)[)]$"));
            reg.Add("while", new Regex("^\\s*while\\s*([\\w\\s=!<>()]+)\\s*:\\s*$"));
            reg.Add("if", new Regex("^\\s*if\\s*([\\w\\s=!<>()]+)\\s*:\\s*$"));
            reg.Add("str_lit", new Regex("^\\s*(\"{1}([\\w\\s]*(\\\\|[\\w\\s\",?!])*)*((?!\\\\)\"){1})\\s*$"));
            reg.Add("hex_lit", new Regex("^\\s*?(0x[\\dABCDEF]+)\\s*$"));
            reg.Add("dec_lit", new Regex("^\\s*?(\\d)+\\s*$"));
            reg.Add("name", new Regex("^\\s*(?!\\d)(\\w+)\\s*$"));
            for (int i = 0; i < lines.Count; i++)
            {
                string ln = lines[i];
                prevTab = tab;
                if (ln.StartsWith("\t"))
                    tab = ln.Split('\t').Length - 1;
                else
                {
                    string ind = "";
                    for (int id = 0; id < ln.Length; id++)
                    {
                        if (ln[id] != ' ') break;
                        ind += " ";
                    }
                    tab = ind.Split("    ").Length - 1;
                }
                if (prevTab > tab && !retp)
                {
                    for (int id = 0; id < prevTab - tab; id++)
                    {
                        methcode[meth.Peek().Name].Add("ret");
                        meth.Pop();
                    }
                }
                else if (retp) retp = false;
                ParseLine(lines[i]);
            }
            il.Add(":&__ret_func");
            il.Add("ret");
            foreach (string k in methcode.Keys)
            {
                il.Add(":" + k);
                il.AddRange(methcode[k]);
            }
            File.WriteAllLines(outfile, il.ToArray());
        }
        Token ParseLine(string s)
        {
            Match m;
            string nm = "";
            Token tk = null;
            Token tkr = null;
            string lin = s;
            foreach (string g in reg.Keys)
            {
                m = reg[g].Match(lin);
                if (m.Success)
                {
                    switch (g)
                    {
                        case "import":
                            il.Add("#include " + m.Groups[1] + ".ns");
                            break;
                        case "def":
                            meth.Push(new Block(m.Groups[1].ToString(), BlockType.Method));
                            methcode.Add(m.Groups[1].ToString(), new List<string>());
                            string[] cargs = Regex.Split(m.Groups[2].ToString(), "\\s*[,]\\s*");
                            for (int ax = cargs.Length - 1; ax >= 0; ax--)
                            {
                                if (cargs[ax].Trim() != "")
                                {
                                    methcode[meth.Peek().Name].Add("pop " + meth.Peek() + "!" + cargs[ax]);
                                    vars.Add(meth.Peek() + "!" + cargs[ax]);
                                }
                            }
                            break;
                        case "while":
                            nm = "_while_" + meth.Peek().Name + "@" + meth.Peek().WhileCount.ToString();
                            methcode[meth.Peek().Name].Add("goto " + nm);
                            meth.Peek().WhileCount += 1;
                            meth.Push(new Block(nm, BlockType.While, new Token(m.Groups[1].ToString())));
                            methcode.Add(nm, new List<string>());
                            break;
                        case "if":
                            nm = "_if_" + meth.Peek().Name + "@" + meth.Peek().IfCount.ToString();
                            methcode[meth.Peek().Name].Add("goto " + nm);
                            meth.Peek().IfCount += 1;
                            meth.Push(new Block(nm, BlockType.If, new Token(m.Groups[1].ToString())));
                            methcode.Add(nm, new List<string>());
                            break;
                        case "return":
                            if (meth.Peek().Type == BlockType.Method)
                            {
                                methcode[meth.Peek().Name].Add("ret");
                                meth.Pop();
                                retp = true;
                            }
                            else methcode[meth.Peek().Name].Add("lj &__ret_func");
                            break;
                        case "return_val":
                            tkr = ParseLine(m.Groups[1].ToString());
                            if (tkr != null)
                            {
                                if (tkr.Type == TokenType.Literal)
                                {
                                    methcode[meth.Peek().Name].Add("spush " + tkr.Text);
                                }
                                else if (tkr.Type == TokenType.Name)
                                {
                                    methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                }
                            }
                            if (meth.Peek().Type == BlockType.Method)
                            {
                                methcode[meth.Peek().Name].Add("ret");
                                meth.Pop();
                                retp = true;
                            }
                            else methcode[meth.Peek().Name].Add("lj &__ret_func");
                            break;
                        case "assign_var_var":
                            methcode[meth.Peek().Name].Add("mov " + GetVarName(m.Groups[1].ToString()) + " " + GetVarName(m.Groups[2].ToString()));
                            break;
                        case "assign_var_call":
                            for (int j = 0; j < lin.Length; j++)
                            {
                                if (lin[0] != ' ') break;
                                lin = lin.Remove(0, 1);
                            }
                            if (!lin.StartsWith("def"))
                            {
                                tk = new Token(lin);
                                Stack<string> left = new Stack<string>();
                                left.Push(tk[0].Name);
                                List<Token> tkns = new List<Token>();
                                for (int i = 1; i < tk.Tokens.Count; i++) tkns.Add(tk[i]);
                                tkr = new Token();
                                tkr.Add(tkns.ToArray());
                                tkr.Name = tk.Name;
                                tk = tkr;
                                int lev = 0;
                                while (true)
                                {
                                    if (tkr.ChildIndex < tkr.Tokens.Count)
                                    {
                                        lev++;
                                        tkr = tkr.Tokens[tkr.ChildIndex];
                                    }
                                    else
                                    {
                                        if (lev == 0) break;
                                        if (tkr.IsAssignment) left.Push(GetVarName(tkr.Name));
                                        if (tkr.Tokens.Count == 0) methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                        lev--;
                                        tkr = Token.Node(ref tk, lev);
                                        tkr.ChildIndex++;
                                        if (tkr.ChildIndex == tkr.Tokens.Count && tkr.Tokens.Count > 0)
                                        {
                                            methcode[meth.Peek().Name].Add("call " + tkr.Name);
                                            methcode[meth.Peek().Name].Add("pop " + left.Pop());
                                        }
                                    }
                                }
                            }
                            break;
                        case "method_call":
                            for (int j = 0; j < lin.Length; j++)
                            {
                                if (lin[0] != ' ') break;
                                lin = lin.Remove(0, 1);
                            }
                            if (!lin.StartsWith("def"))
                            {
                                tk = new Token(lin);
                                tkr = tk;
                                int lev = 0;
                                while (true)
                                {
                                    if (tkr.ChildIndex < tkr.Tokens.Count)
                                    {
                                        lev++;
                                        tkr = tkr.Tokens[tkr.ChildIndex];
                                    }
                                    else
                                    {
                                        if (lev == 0) break;
                                        if (tkr.Tokens.Count == 0) methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                        lev--;
                                        tkr = Token.Node(ref tk, lev);
                                        tkr.ChildIndex++;
                                        if (tkr.ChildIndex == tkr.Tokens.Count && tkr.Tokens.Count > 0)
                                        {
                                            methcode[meth.Peek().Name].Add("call " + tkr.Name);
                                        }
                                    }
                                }
                            }
                            break;
                        case "name":
                            tk = new Token(m.Groups[0].ToString());
                            break;
                        case "str_lit":
                            tk = new Token(m.Groups[1].ToString());
                            break;
                        case "hex_lit":
                            tk = new Token(m.Groups[0].ToString());
                            break;
                        case "dec_lit":
                            tk = new Token(m.Groups[0].ToString());
                            break;
                    }
                }
            }
            return tk;
        }
        string GetVarName(string var)
        {
            string nm = "";
            for (int i = 0; i < meth.Count; i++)
            {
                nm = meth.ToArray()[i] + "!" + var;
                if (vars.Contains(nm)) break;
                else nm = "";
            }
            if (nm == "")
            {
                nm = meth.Peek() + "!" + var;
            }
            return nm;
        }
        void Error(string err, string ln)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Syntax error: " + err);
            Console.WriteLine("At: " + ln);
            Console.ResetColor();
            Environment.Exit(-1);
        }
    }
    class Block
    {
        public string Name { get; set; }
        public BlockType Type { get; set; }
        public Token Condition { get; set; }
        public int IfCount { get; set; }
        public int WhileCount { get; set; }
        public Block(string name, BlockType type)
        {
            Name = name;
            Type = type;
            IfCount = 0;
            WhileCount = 0;
        }
        public Block(string name, BlockType type, Token cond)
        {
            Name = name;
            Type = type;
            IfCount = 0;
            WhileCount = 0;
            Condition = cond;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    enum BlockType
    {
        Method,
        While,
        If,
        For
    }
    enum TokenType
    {
        MethodCall,
        Literal,
        Name,
        Operator
    }
    class Token
    {
        public List<Token> Tokens { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int ChildIndex { get; set; }
        public bool IsAssignment { get; set; }
        public TokenType Type { get; set; }
        public Token()
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            ChildIndex = 0;
            IsAssignment = false;
        }
        public Token(string t)
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            ChildIndex = 0;
            IsAssignment = false;
            Text = t;
            string ct = "";
            string bound = "";
            string name = "";
            bool pastName = false;
            bool cp = false;
            bool relate = false;
            for (int i = 0; i < t.Length; i++)
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
                    {
                        bound = bound.Remove(bound.Length - 1);
                    }
                    else if (bound.Length == 0) cp = false;
                    if (bound.Length == 0 && (t[i] == ',' || t[i] == '=' || (!cp && t[i] == ')')))
                    {
                        if (ct[ct.Length - 1] == ',' || ct[ct.Length - 1] == '=' || (!cp && ct[ct.Length - 1] == ')')) ct = ct.Remove(ct.Length - 1);
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
                    if (t[i] == '(')
                    {
                        pastName = true;
                    }
                    else if (t[i] == '=')
                    {
                        relate = true;
                    }
                    else name += t[i];
                }
                if (relate)
                {
                    for (int j = 0; j < name.Length; j++)
                    {
                        if (name[0] != ' ') break;
                        name = name.Remove(0, 1);
                    }
                    for (int j = name.Length - 1; j >= 0; j--)
                    {
                        if (name[0] != ' ') break;
                        name = name.Remove(0, 1);
                    }
                    Tokens.Add(new Token(name));
                    Tokens[Tokens.Count - 1].IsAssignment = true;
                    name = "";
                    for (int j = 0; j <= i; j++)
                    {
                        Text = Text.Remove(0, 1);
                    }
                    for (int j = 0; j < Text.Length; j++)
                    {
                        if (Text[0] != ' ') break;
                        Text = Text.Remove(0, 1);
                    }
                    relate = false;
                }
            }
            Name = name.Trim();
            int n;
            if (Tokens.Count > 0) Type = TokenType.MethodCall;
            else if (Text.StartsWith("\"") || Text.StartsWith("0x") || int.TryParse(Text, out n)) Type = TokenType.Literal;
            else if (Text == "==" || Text == "=") Type = TokenType.Operator;
            else Type = TokenType.Name;
        }
        public Token this[int index]
        {
            get => Tokens[index];
            set => Tokens[index] = value;
        }
        public static Token Node(ref Token tree, int lev)
        {
            Token tkr = tree;
            for (int l = 0; l < lev; l++)
            {
                tkr = tkr.Tokens[tkr.ChildIndex];
            }
            return tkr;
        }
        public void Add(params Token[] t)
        {
            for (int i = 0; i < t.Length; i++)
            {
                Tokens.Add(t[i]);
            }
        }
    }
}
