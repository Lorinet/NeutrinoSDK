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
        string version = "0.1f";
        int tab = 0;
        int prevTab = 0;
        bool retp = false;
        List<string> il = new List<string>();
        Dictionary<string, Regex> reg = new Dictionary<string, Regex>();
        Dictionary<string, Regex> textreg = new Dictionary<string, Regex>();
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
            il.Add("; Compiled with Whiplash Python Compiler version " + version);
            il.Add(":&__ret_func");
            il.Add("ret");
            List<string> lines = new List<string>(File.ReadAllLines(infile));
            lines.Add("");
            reg.Add("import", new Regex("^\\s*import\\s+(\\w+)\\s*$"));
            reg.Add("inline_il", new Regex("^\\s*!\\s*[(][']([\\w\\s\\(\\)\",=<>!.]*)['][)]\\s*"));
            reg.Add("def", new Regex("^\\s*def\\s+(\\w+)\\s*[(]((\\s*\\w+\\s*[,]{0,1}\\s*)*)[)]:$"));
            reg.Add("return", new Regex("^\\s*return\\s*$"));
            reg.Add("return_val", new Regex("^\\s*return\\s+(.+)$"));
            reg.Add("method_call", new Regex("^\\s*([@\\w]+)\\s*[(]([\\w\\s\\(\\),=<>!.\"']*)[)]"));
            reg.Add("assign_var_var", new Regex("^\\s*(\\w+)\\s*=\\s*(\\w+)$"));
            reg.Add("assign_var_call", new Regex("^\\s*(\\w+)\\s*=\\s*([@\\w\\s\\(\\),=<>!.\"']+)$"));
            reg.Add("while", new Regex("^\\s*while\\s*([\\w\\s=!<>()]+)\\s*:\\s*$"));
            reg.Add("if", new Regex("^\\s*if\\s*([\\w\\s=!<>()]+)\\s*:\\s*$"));
            textreg.Add("str_lit", new Regex("^\\s*(\"{1}([\\w\\s]*(\\\\|[\\w\\s\",?!])*)*((?!\\\\)\"){1})\\s*$"));
            textreg.Add("hex_lit", new Regex("^\\s*?(0x[\\dABCDEF]+)\\s*$"));
            textreg.Add("dec_lit", new Regex("^\\s*?(\\d)+\\s*$"));
            textreg.Add("name", new Regex("^\\s*(?!\\d)(\\w+)\\s*$"));
            textreg.Add("compare_equality", new Regex("\\s * (\\w +)\\s *==\\s * ([\\s\\w(),=] +)$"));
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
                    if (g == "import") il.Add("#include " + m.Groups[1] + ".ns");
                    else if (g == "def")
                    {
                        meth.Push(new Block(m.Groups[1].ToString(), BlockType.Method));
                        methcode.Add(m.Groups[1].ToString(), new List<string>());
                        if (m.Groups[1].ToString() == "main") methcode[m.Groups[1].ToString()].Add("link whiprt.lnx");
                        string[] cargs = Regex.Split(m.Groups[2].ToString(), "\\s*[,]\\s*");
                        for (int ax = cargs.Length - 1; ax >= 0; ax--)
                        {
                            if (cargs[ax].Trim() != "")
                            {
                                methcode[meth.Peek().Name].Add("pop " + meth.Peek() + "!" + cargs[ax]);
                                vars.Add(meth.Peek() + "!" + cargs[ax]);
                            }
                        }
                    }
                    else if (g == "inline_il") 
                        methcode[meth.Peek().Name].Add(m.Groups[1].ToString());
                    else if (g == "while")
                    {
                        nm = "_while_" + meth.Peek().Name + "@" + meth.Peek().WhileCount.ToString();
                        methcode[meth.Peek().Name].Add("goto " + nm);
                        meth.Peek().WhileCount += 1;
                        meth.Push(new Block(nm, BlockType.While, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                    }
                    else if (g == "if")
                    {
                        nm = "_if_" + meth.Peek().Name + "@" + meth.Peek().IfCount.ToString();
                        methcode[meth.Peek().Name].Add("goto " + nm);
                        meth.Peek().IfCount += 1;
                        meth.Push(new Block(nm, BlockType.If, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                    }
                    else if (g == "return")
                    {
                        if (meth.Peek().Type == BlockType.Method)
                        {
                            methcode[meth.Peek().Name].Add("ret");
                            meth.Pop();
                            retp = true;
                        }
                        else methcode[meth.Peek().Name].Add("lj &__ret_func");
                    }
                    else if (g == "return_val")
                    {
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
                    }
                    else if (g == "assign_var_var") methcode[meth.Peek().Name].Add("mov " + GetVarName(m.Groups[1].ToString()) + " " + GetVarName(m.Groups[2].ToString()));
                    else if (g == "assign_var_call" || g == "method_call")
                    {
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
                                    lev--;
                                    if (tkr.Tokens.Count == 0)
                                    {
                                        if (tkr.Type == TokenType.Name) methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                        else if (tkr.Type == TokenType.Literal) methcode[meth.Peek().Name].Add("spush " + tkr.Text);
                                    }
                                    tkr = Token.Node(ref tk, lev);
                                    tkr.ChildIndex++;
                                    if (tkr.ChildIndex == tkr.Tokens.Count && tkr.Tokens.Count > 0)
                                    {
                                        if (tkr.Type == TokenType.MethodCall)
                                        {
                                            if (tkr.Name[0] == '@') methcode[meth.Peek().Name].Add("extcall " + tkr.Name.Remove(0, 1));
                                            else methcode[meth.Peek().Name].Add("call " + tkr.Name);
                                        }
                                        else if (tkr.Type == TokenType.ComparisonEqual)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_eqf_func");
                                        }
                                        else if (tkr.Type == TokenType.ComparisonNotEqual)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_nef_func");
                                        }
                                        else if (tkr.Type == TokenType.ComparisonLess)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_lf_func");
                                        }
                                        else if (tkr.Type == TokenType.ComparisonGreater)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_gf_func");
                                        }

                                        else if (tkr.Type == TokenType.ComparisonLessEqual)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_lef_func");
                                        }

                                        else if (tkr.Type == TokenType.ComparisonGreaterEqual)
                                        {
                                            methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                            methcode[meth.Peek().Name].Add("extcall &__compare_gef_func");
                                        }
                                        else if (tkr.Type == TokenType.Assignment)
                                        {
                                            methcode[meth.Peek().Name].Add("pop " + GetVarName(tkr.Name));
                                        }
                                    }
                                }
                            }
                        }
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
        Void,
        Assignment,
        ComparisonLess,
        ComparisonLessEqual,
        ComparisonGreater,
        ComparisonGreaterEqual,
        ComparisonEqual,
        ComparisonNotEqual,
        Plus,
        Minus,
        Multiply,
        Divide,
        Or,
        And,
        Not,
        AndAnd,
        OrOr,
        LeftShift,
        RightShift
    }
    class Token
    {
        public List<Token> Tokens { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int ChildIndex { get; set; }
        public TokenType Type { get; set; }
        public Token()
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            ChildIndex = 0;
            Type = TokenType.Name;
        }
        public Token(string t)
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            ChildIndex = 0;
            Type = TokenType.Name;
            if(t == "\0")
            {
                Type = TokenType.Void;
                return;
            }
            Text = t.Replace("==", "\x01").Replace("<=", "\x02").Replace(">=", "\x03").Replace("!=", "\x04");
            string ct = "";
            string bound = "";
            string name = "";
            bool pastName = false;
            bool cp = false;
            bool relate = false;
            TokenType relationType = TokenType.Name;
            for (int i = 0; i < Text.Length; i++)
            {
                if (pastName)
                {
                    ct += Text[i];
                    if ((bound.Length == 0 || (bound.Length > 0 && bound[bound.Length - 1] != '"')) && Text[i] == '(')
                    {
                        bound += '(';
                        cp = true;
                    }
                    else if (Text[i] == '"' && Text[i - i] != '\\')
                    {
                        if (bound.Length > 0 && bound[bound.Length - 1] == '"' && Text[i] == '"' && Text[i - 1] != '\\')
                            bound = bound.Remove(bound.Length - 1);
                        else bound += '"';
                    }
                    else if (bound.Length > 0 && bound[bound.Length - 1] == '(' && Text[i] == ')')
                    {
                        bound = bound.Remove(bound.Length - 1);
                    }
                    else if (bound.Length == 0) cp = false;
                    if (bound.Length == 0 && (Text[i] == ',' || (!cp && Text[i] == ')')))
                    {
                        if (ct[ct.Length - 1] == ',' || (!cp && ct[ct.Length - 1] == ')')) ct = ct.Remove(ct.Length - 1);
                        for (int j = 0; j < ct.Length; j++)
                        {
                            if (ct[0] != ' ') break;
                            ct = ct.Remove(0, 1);
                        }
                        if (relationType != TokenType.MethodCall && i == Text.Length - 1 && Text[i] == ')')
                        {
                            Text = Text.Remove(Text.Length - 1, 1);
                            i--;
                        }
                        if (ct == Text) return;
                        if (ct.Trim() == "") ct = "\0";
                        Tokens.Add(new Token(ct));
                        ct = "";
                        cp = false;
                    }
                }
                else
                {
                    if (Text[i] == '(')
                    {
                        pastName = true;
                        Type = TokenType.MethodCall;
                    }
                    else if (Text[i] == '=' || Text[i] == '<' || Text[i] == '>' || Text[i] == '\x01' || Text[i] == '\x02' || Text[i] == '\x03' || Text[i] == '\x04' || Text[i] == '\x05' || Text[i] == '\x06' || Text[i] == '\x07' || Text[i] == '\x08')
                    {
                        relate = true;
                        switch (Text[i])
                        {
                            case '=':
                                relationType = TokenType.Assignment;
                                break;
                            case '<':
                                relationType = TokenType.ComparisonLess;
                                break;
                            case '>':
                                relationType = TokenType.ComparisonGreater;
                                break;
                            case '\x01':
                                relationType = TokenType.ComparisonEqual;
                                break;
                            case '\x02':
                                relationType = TokenType.ComparisonLessEqual;
                                break;
                            case '\x03':
                                relationType = TokenType.ComparisonGreaterEqual;
                                break;
                            case '\x04':
                                relationType = TokenType.ComparisonNotEqual;
                                break;
                            case '\x05':
                                relationType = TokenType.Plus;
                                break;
                            case '\x06':
                                relationType = TokenType.Minus;
                                break;
                            case '\x07':
                                relationType = TokenType.Multiply;
                                break;
                            case '\x08':
                                relationType = TokenType.Divide;
                                break;
                        }
                    }
                    else name += Text[i];
                }
                if (relate)
                {
                    i++;
                    while (i < Text.Length)
                    {
                        if (Text[i] != ' ') break;
                        i++;
                    }
                    string backtracking = "";
                    string cbr = "";
                    bool mod = false;
                    while (i < Text.Length)
                    {
                        if ((cbr.Length == 0 || (cbr.Length > 0 && cbr[cbr.Length - 1] != '"')) && Text[i] == '(')
                        {
                            cbr += '(';
                            mod = true;
                        }
                        else if (Text[i] == '"' && Text[i - i] != '\\')
                        {
                            if (cbr.Length > 0 && cbr[cbr.Length - 1] == '"' && Text[i] == '"' && Text[i - 1] != '\\')
                                cbr = cbr.Remove(cbr.Length - 1);
                            else cbr += '"';
                            mod = true;
                        }
                        else if (cbr.Length > 0 && cbr[cbr.Length - 1] == '(' && Text[i] == ')')
                        {
                            cbr = cbr.Remove(cbr.Length - 1);
                        }
                        else if (cbr.Length == 0 && mod) break;
                        backtracking += Text[i];
                        i++;
                    }
                    Tokens.Add(new Token(backtracking));
                    Type = relationType;
                    relate = false;
                    pastName = true;
                }
            }
            Name = name.Trim();
            int n;
            for (int j = 0; j < Text.Length; j++)
            {
                if (Text[0] != ' ') break;
                Text = Text.Remove(0, 1);
            }
            if (Type == TokenType.Name && (Text.StartsWith("\"") || Text.StartsWith("0x") || int.TryParse(Text, out n))) Type = TokenType.Literal;
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
