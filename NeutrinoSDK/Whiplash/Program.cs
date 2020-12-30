﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Text;
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
        int lineNumber = 0;
        string currentFile = "";
        bool retp = false;
        bool nextShouldInvokeClassConstructor = false;
        List<string> il = new List<string>();
        Dictionary<string, Regex> reg = new Dictionary<string, Regex>();
        Dictionary<string, Regex> textreg = new Dictionary<string, Regex>();
        Stack<Block> meth = new Stack<Block>();
        Dictionary<string, ClassTemplate> classes = new Dictionary<string, ClassTemplate>();
        Dictionary<string, string> vars = new Dictionary<string, string>();
        Dictionary<string, List<string>> methcode = new Dictionary<string, List<string>>();
        Dictionary<string, string> methRetTypes = new Dictionary<string, string>();
        string importDirectories = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Neutrino\\ndk\\whiplash");
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
            il.Add("; File: " + infile);
            il.Add(":&__ret_func");
            il.Add("ret");
            List<string> lines = new List<string>(File.ReadAllLines(infile));
            lines.Add("");
            reg.Add("empty_line", new Regex("^\\s*$"));
            reg.Add("comment", new Regex("^\\s*#.*"));
            reg.Add("include", new Regex("^\\s*include\\s+(\\w+)\\s*$"));
            reg.Add("import", new Regex("^\\s*import\\s+(\\w+)\\s*$"));
            reg.Add("inline_il", new Regex("^\\s*!\\s*[(][']([#\\w\\s\\(\\)\",=<>!.]*)['][)]\\s*"));
            reg.Add("def", new Regex("^\\s*def\\s+(\\w+)\\s*[(]((\\s*\\w+\\s*[,]{0,1}\\s*)*)[)]:$"));
            reg.Add("class", new Regex("^\\s*class\\s+(\\w+)\\s*:"));
            reg.Add("return", new Regex("^\\s*return\\s*$"));
            reg.Add("return_val", new Regex("^\\s*return\\s+([\\w\\.]+)$"));
            reg.Add("return_call", new Regex("^\\s*return\\s*([@\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:\\.]+)\\s*"));
            reg.Add("method_call", new Regex("^\\s*([@\\w\\.]+)\\s*[(]([\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:\\.]*)[)]\\s*"));
            reg.Add("assign_var_var", new Regex("^\\s*([\\w\\.]+)\\s*=\\s*((?![\\d]{1})[\\w\\.]+)\\s*$"));
            reg.Add("assign_var_call", new Regex("^\\s*([\\w\\.]+)\\s*[+-/*%]*=\\s*([@\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:\\.]+)\\s*"));
            reg.Add("if", new Regex("^\\s*if\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%:\\.]+)\\s*:\\s*$"));
            reg.Add("elif", new Regex("^\\s*elif\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%:\\.]+)\\s*:\\s*$"));
            reg.Add("else", new Regex("^\\s*else\\s*:\\s*$"));
            reg.Add("while", new Regex("^\\s*while\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%\\.]+)\\s*:\\s*$"));
            textreg.Add("str_lit", new Regex("^\\s*(\"{1}([\\w\\s]*(\\\\|[\\w\\s\",?!])*)*((?!\\\\)\"){1})\\s*$"));
            textreg.Add("hex_lit", new Regex("^\\s*?(0x[\\dABCDEF]+)\\s*$"));
            textreg.Add("dec_lit", new Regex("^\\s*?(\\d)+\\s*$"));
            textreg.Add("name", new Regex("^\\s*(?!\\d)(\\w+)\\s*$"));
            methcode.Add("main", new List<string>());
            meth.Push(new Block("main", BlockType.Method));
            methcode["main"].Add("link whiprt.lnx");
            currentFile = infile;
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
                        if (meth.Peek().Type == BlockType.While)
                            methcode[meth.Peek().Name].Add("lj " + meth.Peek().Name);
                        else if (meth.Peek().Name.Split('!').Length == 2 && meth.Peek().Name.Split('!')[1] == "__init__")
                            methcode[meth.Peek().Name].Add("push " + meth.Peek().Name + "!self");
                        methcode[meth.Peek().Name].Add("ret");
                        meth.Pop();
                    }
                }
                else if (retp) retp = false;
                lineNumber = i;
                ParseLine(lines[i]);
            }
            foreach(KeyValuePair<string, ClassTemplate> ct in classes)
            {
                methcode["main"].Insert(1, "call " + ct.Key);
            }
            foreach (string k in methcode.Keys)
            {
                il.Add(":" + k);
                if (methcode[k].Count > 0 && !methcode[k][methcode[k].Count - 1].Trim().StartsWith("ret")) methcode[k].Add("ret");
                il.AddRange(methcode[k]);
            }
            File.WriteAllLines(outfile, il.ToArray());
            // Only for testing
            Process.Start("python", "C:\\Neutrino\\ndk\\ntrasm.py " + outfile + " C:\\Neutrino\\bin\\" + outfile.Replace(".ns", ".lex"));
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
                    if (g == "empty_line" || g == "comment") break;
                    else if (g == "import")
                    {
                        string ipa = "..\\" + m.Groups[1] + ".py";
                        if (!File.Exists(ipa))
                        {
                            ipa = Path.Combine(importDirectories, m.Groups[1] + ".py");
                        }
                        if (!File.Exists(ipa))
                        {
                            Error("Cannot find file: " + m.Groups[1]);
                        }
                    }
                    else if (g == "include") il.Add("#include " + m.Groups[1] + ".ns");
                    else if (g == "def")
                    {
                        if (methcode.ContainsKey(m.Groups[1].ToString())) Error("Redefinition of " + m.Groups[1].ToString());
                        meth.Push(new Block(m.Groups[1].ToString(), BlockType.Method));
                        methcode.Add(m.Groups[1].ToString(), new List<string>());
                        bool isMember = meth.ToArray()[1].Type == BlockType.Class;
                        List<string> cargs = new List<string>(Regex.Split(m.Groups[2].ToString(), "\\s*[,]\\s*"));
                        if (m.Groups[1].ToString() == "main") methcode[m.Groups[1].ToString()].Add("link whiprt.lnx");
                        else if (m.Groups[1].ToString() == "__init__" && isMember)
                        {
                            methcode.Remove(meth.Peek().Name);
                            meth.Peek().Name = meth.ToArray()[1].Name + "!__init__";
                            methcode.Add(meth.Peek().Name, new List<string>());
                            vars.Add(meth.Peek().Name + "!self", meth.ToArray()[1].Name);
                            cargs.RemoveAt(0);
                            SetMethRetType(meth.Peek().Name, meth.ToArray()[1].Name);
                        }
                        else if (isMember)
                        {
                            methcode.Remove(meth.Peek().Name);
                            meth.Peek().Name = meth.ToArray()[1].Name + "!" + meth.Peek().Name;
                            methcode.Add(meth.Peek().Name, new List<string>());
                            vars.Add(meth.Peek().Name + "!self", meth.ToArray()[1].Name);
                            classes[meth.ToArray()[1].Name].Members.Add(m.Groups[1].ToString(), (-1, true));
                            vars.Add(meth.Peek().Name, "");
                            // put self at the end so that we can push self automatically
                            cargs.Add(cargs[0]);
                            cargs.RemoveAt(0);
                        }
                        for (int ax = cargs.Count - 1; ax >= 0; ax--)
                        {
                            if (cargs[ax].Trim() != "")
                            {
                                methcode[meth.Peek().Name].Add("pop " + meth.Peek() + "!" + cargs[ax]);
                                if (!vars.ContainsKey(meth.Peek().Name + "!" + cargs[ax])) vars.Add(meth.Peek() + "!" + cargs[ax], "");
                            }
                        }
                        if (m.Groups[1].ToString() == "__init__" && isMember)
                        {
                            methcode[meth.Peek().Name].Add("vac");
                            methcode[meth.Peek().Name].Add("pop " + meth.Peek().Name + "!self");
                        }
                    }
                    else if (g == "class")
                    {
                        if (methcode.ContainsKey(m.Groups[1].ToString())) Error("Redefinition of " + m.Groups[1].ToString());
                        meth.Push(new Block(m.Groups[1].ToString(), BlockType.Class));
                        methcode.Add(m.Groups[1].ToString(), new List<string>());
                        classes.Add(meth.Peek().Name, new ClassTemplate(meth.Peek().Name));
                    }
                    else if (g == "inline_il")
                        methcode[meth.Peek().Name].Add(m.Groups[1].ToString());
                    else if (g == "if")
                    {
                        nm = meth.Peek().Name + "!&!cond";
                        if (!vars.ContainsKey(nm)) vars.Add(nm, "");
                        nm = "_if_" + meth.Peek().Name + "@" + meth.Peek().IfCount.ToString();
                        ProcessStatement("&!cond = " + m.Groups[1].ToString());
                        methcode[meth.Peek().Name].Add("mov " + meth.Peek().Name + "!&!cond " + meth.Peek().Name + "!&!orig_cond");
                        methcode[meth.Peek().Name].Add("cmpi " + meth.Peek().Name + "!&!cond 1");
                        methcode[meth.Peek().Name].Add("jeq " + nm);
                        meth.Peek().IfCount += 1;
                        meth.Push(new Block(nm, BlockType.If, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                    }
                    else if (g == "elif")
                    {
                        nm = meth.Peek().Name + "!&!cond";
                        if (!vars.ContainsKey(nm)) vars.Add(nm, "");
                        nm = "_elif_" + meth.Peek().Name + "@" + meth.Peek().IfCount.ToString();
                        ProcessStatement("&!cond = !&!orig_cond && " + m.Groups[1].ToString());
                        methcode[meth.Peek().Name].Add("cmpi " + meth.Peek().Name + "!&!cond 1");
                        methcode[meth.Peek().Name].Add("jeq " + nm);
                        meth.Peek().IfCount += 1;
                        meth.Push(new Block(nm, BlockType.If, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                        methcode[nm].Add("str " + meth.ToArray()[1].Name + "!&!orig_cond 1");
                    }
                    else if (g == "else")
                    {
                        nm = "_else_" + meth.Peek().Name + "@" + meth.Peek().IfCount.ToString();
                        methcode[meth.Peek().Name].Add("cmpi " + meth.Peek().Name + "!&!orig_cond 1");
                        methcode[meth.Peek().Name].Add("jne " + nm);
                        meth.Peek().IfCount += 1;
                        meth.Push(new Block(nm, BlockType.If, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                    }
                    else if (g == "while")
                    {
                        nm = meth.Peek().Name + "!&!cond";
                        if (!vars.ContainsKey(nm)) vars.Add(nm, "");
                        nm = "_while_" + meth.Peek().Name + "@" + meth.Peek().WhileCount.ToString();
                        methcode[meth.Peek().Name].Add("jmp " + nm);
                        meth.Peek().WhileCount += 1;
                        meth.Push(new Block(nm, BlockType.While, new Token(m.Groups[1].ToString())));
                        methcode.Add(nm, new List<string>());
                        ProcessStatement("&!cond = " + m.Groups[1].ToString());
                        methcode[meth.Peek().Name][methcode[meth.Peek().Name].Count - 1] = "pop " + nm + "!&!cond";
                        methcode[meth.Peek().Name].Add("cmpi " + nm + "!&!cond 1");
                        methcode[meth.Peek().Name].Add("ljne &__ret_func");
                    }
                    else if (g == "return")
                    {
                        if (meth.Peek().Type == BlockType.Method)
                        {
                            methcode[meth.Peek().Name].Add("ret");
                            meth.Pop();
                            retp = true;
                        }
                        // TODO: Return from if/while/for block
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
                                SetMethRetType(meth.Peek().Name, ResolveObjectChildren(GetVarName(tkr.Name)).classType);
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
                    else if (g == "assign_var_var")
                    {
                        // Resolve children!
                        (bool isMember, string classType, string memberName, string parent) cr = ResolveObjectChildren(GetVarName(m.Groups[2].ToString()));
                        if (cr.isMember) methcode[meth.Peek().Name].Add("vai");
                        else methcode[meth.Peek().Name].Add("push " + GetVarName(m.Groups[2].ToString()));
                        if (ResolveObjectChildren(GetVarName(m.Groups[1].ToString())).isMember) methcode[meth.Peek().Name].Add("vas");
                        else
                        {
                            methcode[meth.Peek().Name].Add("pop " + GetVarName(m.Groups[1].ToString()));
                            vars[GetVarName(m.Groups[1].ToString())] = cr.classType;
                        }
                    }
                    else if (g == "assign_var_call" || g == "method_call") ProcessStatement(lin);
                    else if (g == "return_call")
                    {
                        ProcessStatement(m.Groups[1].ToString());
                        if (meth.Peek().Type == BlockType.Method)
                        {
                            methcode[meth.Peek().Name].Add("ret");
                            meth.Pop();
                            retp = true;
                        }
                        else methcode[meth.Peek().Name].Add("lj &__ret_func");
                    }
                    break;
                }
            }
            return tk;
        }
        void ProcessStatement(string stmt)
        {
            for (int j = 0; j < stmt.Length; j++)
            {
                if (stmt[0] != ' ') break;
                stmt = stmt.Remove(0, 1);
            }
            if (!stmt.StartsWith("def"))
            {
                Token tk = new Token(stmt);
                Token tkr = tk;
                int lev = 0;
                string prevCall = "";
                (bool isMember, string classType, string memberName, string parent) re;
                (bool isClassVar, string identifier) cv;
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
                            if (tkr.Type == TokenType.Name)
                            {
                                re = ResolveObjectChildren(GetVarNameDot(tkr.Name));
                                if (re.isMember)
                                {
                                    methcode[meth.Peek().Name].Add("vai");
                                }
                                else
                                {
                                    cv = GetClassVar(tkr.Name);
                                    if(cv.isClassVar)
                                    {
                                        methcode[meth.Peek().Name].Add("push " + cv.identifier);
                                    }
                                    else methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                }
                            }
                            else if (tkr.Type == TokenType.Literal)
                            {
                                methcode[meth.Peek().Name].Add("spush " + tkr.Text);
                            }
                            if (tkr.Negate) methcode[meth.Peek().Name].Add("extcall &__not_func");
                        }
                        tkr = Token.Node(ref tk, lev);
                        tkr.ChildIndex++;
                        if (tkr.ChildIndex == tkr.Tokens.Count && tkr.Tokens.Count > 0)
                        {
                            if (tkr.Type == TokenType.MethodCall)
                            {
                                // FIXME: This is a hack. Really you shouldn't check for arithmetic operations
                                //        based on initial character. Beginner programmers forget about invalid
                                //        python characters in identifiers and then complain about random errors.
                                if (tkr.Name[0] == '~') methcode[meth.Peek().Name].Add(tkr.Name.Remove(0, 1));
                                else
                                {
                                    re = ResolveObjectChildren(GetVarNameDot(tkr.Name), false);
                                    if (re.isMember)
                                    {
                                        ResolveObjectChildren(GetVarNameDot(tkr.Name));
                                        methcode[meth.Peek().Name].RemoveAt(methcode[meth.Peek().Name].Count - 2);
                                        methcode[meth.Peek().Name].Add("extcall " + re.memberName);
                                    }
                                    else if (classes.ContainsKey(tkr.Name)) methcode[meth.Peek().Name].Add("extcall " + tkr.Name + "!__init__");
                                    else methcode[meth.Peek().Name].Add("extcall " + tkr.Name);
                                }
                                if (tkr.Negate) methcode[meth.Peek().Name].Add("extcall &__not_func");
                            }
                            else if (tkr.Type == TokenType.Assignment)
                            {
                                if (tkr.Negate) methcode[meth.Peek().Name].Add("extcall &__not_func");
                                prevCall = methcode[meth.Peek().Name][methcode[meth.Peek().Name].Count - 1];
                                if (prevCall.StartsWith("extcall"))
                                {
                                    if(classes.ContainsKey(prevCall.Split(' ')[1].Split('!')[0]))
                                    {
                                        vars[GetVarName(tkr.Name)] = prevCall.Split(' ')[1].Split('!')[0];
                                    }
                                }
                                if (tkr.AssignmentOperator != "=")
                                {
                                    re = ResolveObjectChildren(GetVarNameDot(tkr.Name));
                                    if (re.isMember) methcode[meth.Peek().Name].Add("vai");
                                    else methcode[meth.Peek().Name].Add("push " + GetVarName(tkr.Name));
                                    methcode[meth.Peek().Name].Add("swap");
                                    if (tkr.AssignmentOperator.Length == 3) methcode[meth.Peek().Name].Add(tkr.AssignmentOperator);
                                    else methcode[meth.Peek().Name].Add("extcall " + tkr.AssignmentOperator);
                                }
                                re = ResolveObjectChildren(GetVarNameDot(tkr.Name));
                                if (re.isMember)
                                {
                                    if (!classes[GetVarType(re.parent)].Members[re.memberName.Split('!')[1]].initialized)
                                    {
                                        classes[GetVarType(re.parent)].SetInitialized(re.memberName.Split('!')[1]);
                                        methcode[meth.Peek().Name].RemoveAt(methcode[meth.Peek().Name].Count - 2);
                                        methcode[meth.Peek().Name].Add("vad");
                                    }
                                    else methcode[meth.Peek().Name].Add("vas");
                                }
                                else
                                {
                                    methcode[meth.Peek().Name].Add("pop " + GetVarName(tkr.Name));
                                }
                            }
                        }
                    }
                }
            }
        }
        (bool isMember, string classType, string memberName, string parent) ResolveObjectChildren(string name, bool touchCode = true)
        {
            // Return if expression is a child of an object, parent's class, name of member and name of parent
            try
            {
                string nm = name;
                string par = "";
                string chi = "";
                bool pastPar = false;
                bool pastFirst = false;
                if (!name.Contains('.')) return (false, GetVarType(name), name, name);
                for (int i = 0; i < nm.Length; i++)
                {
                    if (nm[i] != '.')
                    {
                        if (!pastPar && !pastFirst) par += nm[i];
                        else chi += nm[i];
                    }
                    if (nm[i] == '.' || i == nm.Length - 1)
                    {
                        if (pastPar || i == nm.Length - 1)
                        {
                            if (!classes[GetVarType(par)].Members.ContainsKey(chi))
                            {
                                classes[GetVarType(par)].CreateField(chi);
                            }
                            if (touchCode)
                            {
                                methcode[meth.Peek().Name].Add("spush " + classes[GetVarType(par)].GetFieldIndex(chi));
                                methcode[meth.Peek().Name].Add("push " + meth.Peek().Name + "!__father__");
                            }
                            if (i < nm.Length - 1)
                            {
                                if (touchCode)
                                {
                                    methcode[meth.Peek().Name].Add("vai");
                                    methcode[meth.Peek().Name].Add("pop " + meth.Peek().Name + "!__father__");
                                }
                                par = GetVarType(par) + "!" + chi;
                                chi = "";
                                pastFirst = true;
                            }
                            else return (true, GetVarType(GetVarType(par) + "!" + chi), GetVarType(par) + "!" + chi, par);
                        }
                        else if (!pastFirst)
                        {
                            if (touchCode) methcode[meth.Peek().Name].Add("mov " + par + " " + meth.Peek().Name + "!__father__");
                        }
                        pastPar = !pastPar;
                    }
                }
                return (true, GetVarType(GetVarType(par) + "!" + chi), GetVarType(par) + "!" + chi, par);
            }
            catch (Exception e)
            {
                Warning("Identifier not found: " + name);
                return (false, "", "", "");
            }
        }
        (bool isClassVar, string identifier) GetClassVar(string name)
        {
            // Returns if expression is class variable and its identifier
            string[] exon = name.Split('.');
            if (exon.Length == 1) return (false, name);
            else
            {
                string trick = exon[0] + "!" + exon[1];
                for (int i = 2; i < exon.Length; i++) trick += "." + exon[i];
                return (true, trick);
            }
        }
        string GetVarName(string var)
        {
            // Returns actual variable name of identifier
            string nm = "";
            for (int i = 0; i < meth.Count; i++)
            {
                nm = meth.ToArray()[i] + "!" + var;
                if (vars.ContainsKey(nm)) break;
                else nm = "";
            }
            if (nm == "")
            {
                nm = meth.Peek() + "!" + var;
                vars.Add(nm, "");
            }
            return nm;
        }
        string GetVarNameDot(string var)
        {
            // Returns actual variable name of first parent object identifier followed by children unchanged
            string[] dnf = var.Split('.');
            if (dnf.Length > 1)
            {
                string trick = "." + dnf[1];
                for (int i = 2; i < dnf.Length; i++) trick += "." + dnf[i];
                return GetVarName(dnf[0]) + trick;
            }
            else return GetVarName(var);
        }
        string GetVarType(string var)
        {
            // Gets class of variable if there's one
            if (!vars.ContainsKey(var)) vars.Add(var, "");
            return vars[var];
        }
        string GetMethRetType(string meth)
        {
            if (methRetTypes.ContainsKey(meth)) return methRetTypes[meth];
            return "";
        }
        void SetMethRetType(string meth, string type)
        {
            if (!methRetTypes.ContainsKey(meth)) methRetTypes.Add(meth, type);
            else methRetTypes[meth] = type;
        }
        void Error(string err)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + err);
            Console.WriteLine("At line " + lineNumber + " of file " + currentFile);
            Console.ResetColor();
            Environment.Exit(-1);
        }
        void Warning(string war)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Warning: " + war);
            Console.WriteLine("At line " + lineNumber + " of file " + currentFile);
            Console.ResetColor();
        }
    }
    class ClassTemplate
    {
        public string TypeName { get; set; }
        public Dictionary<string, (int index, bool initialized)> Members { get; set; }
        private int FieldIndex { get; set; }
        public ClassTemplate(string name)
        {
            TypeName = name;
            Members = new Dictionary<string, (int, bool)>();
            FieldIndex = 0;
        }
        public void CreateField(string name)
        {
            if (!Members.ContainsKey(name))
            {
                Members.Add(name, (FieldIndex, false));
                FieldIndex++;
            }
        }
        public int GetFieldIndex(string name) => Members[name].index;
        public void SetInitialized(string name)
        {
            (int, bool) ib = Members[name];
            ib.Item2 = true;
            Members[name] = ib;
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
        For,
        Class
    }
    enum TokenType
    {
        Name,
        MethodCall,
        Assignment,
        Void,
        Literal,
        ClassMember
    }
    class Token
    {
        public List<Token> Tokens { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int ChildIndex { get; set; }
        public bool Negate { get; set; }
        public TokenType Type { get; set; }
        public string AssignmentOperator { get; set; }
        public Token()
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            ChildIndex = 0;
            Negate = false;
            Type = TokenType.Name;
        }
        public Token(string t)
        {
            Tokens = new List<Token>();
            Text = "";
            Name = "";
            AssignmentOperator = "";
            ChildIndex = 0;
            Negate = false;
            Type = TokenType.Name;
            if (t == "\0")
            {
                Type = TokenType.Void;
                return;
            }
            Text = t.Replace("==", "\x01").Replace("<=", "\x02").Replace(">=", "\x03").Replace("!=", "\x04").ReplaceEx("and", "\x05").ReplaceEx("or", "\x06").Replace("+=", "\x07").Replace("-=", "\x0E").Replace("*=", "\x0F").Replace("/=", "\x10").Replace("%=", "\x11").Replace("//", "/").ReplaceAndSubstLeadWs("not", "!");
            string ct = "", bound = "", name = "";
            bool pastName = false, str = false, cp = false;
            int n, k;
            for (k = 0; k < Text.Length; k++)
            {
                if (!str)
                {
                    if (Text[k] == '(') bound += '(';
                    else if (Text[k] == ')')
                    {
                        bound = bound.Remove(bound.Length - 1);
                        if (bound == "") break;
                    }
                    else if (bound == "" && (Text[k] == ' ' || Text[k] == '=' || Text[k] == '<' || Text[k] == '>' || Text[k] == '+' || Text[k] == '-' || Text[k] == '*' || Text[k] == '/' || Text[k] == '%' || Text[k] == '\x01' || Text[k] == '\x02' || Text[k] == '\x03' || Text[k] == '\x04' || Text[k] == '\x05' || Text[k] == '\x06' || Text[k] == '\x07' || Text[k] == '\x0E' || Text[k] == '\x0F' || Text[k] == '\x10' || Text[k] == '\x11'))
                    {
                        k -= 1;
                        break;
                    }
                    else if (Text[k] == '"') str = true;
                }
                else
                {
                    if (Text[k] == '"' && Text[k - 1] != '\\') str = false;
                }
            }
            k++;
            for (int j = k; j < Text.Length; j++)
            {
                if (Text[j] != ' ' && Text[j] != '=' && Text[j] != '<' && Text[j] != '>' && Text[j] != '+' && Text[j] != '-' && Text[j] != '*' && Text[j] != '/' && Text[j] != '%' && Text[j] != '\x01' && Text[j] != '\x02' && Text[j] != '\x03' && Text[j] != '\x04' && Text[j] != '\x05' && Text[j] != '\x06' && Text[j] != '\x07' && Text[j] != '\x0E' && Text[j] != '\x0F' && Text[j] != '\x10' && Text[j] != '\x11')
                    break;
                if (Text[j] == '=' || Text[j] == '\x07' || Text[j] == '\x0E' || Text[j] == '\x0F' || Text[j] == '\x10' || Text[j] == '\x11' || Text[j] == '\x12' || Text[j] == '\x13' || Text[j] == '\x14')
                {
                    Type = TokenType.Assignment;
                    if (Text[j] == '\x07') AssignmentOperator = "add";
                    else if (Text[j] == '\x0E') AssignmentOperator = "sub";
                    else if (Text[j] == '\x0F') AssignmentOperator = "mul";
                    else if (Text[j] == '\x10') AssignmentOperator = "div";
                    else if (Text[j] == '\x11') AssignmentOperator = "&__mod_func";
                    else AssignmentOperator = "=";
                    StringBuilder sb = new StringBuilder(Text);
                    sb[j] = '(';
                    Text = sb.ToString();
                    Text += ')';
                    break;
                }
                else if (Text[j] == '<')
                {
                    Name = "&__compare_lf_func";
                    cp = true;
                }
                else if (Text[j] == '>')
                {
                    Name = "&__compare_gf_func";
                    cp = true;
                }
                else if (Text[j] == '\x01')
                {
                    Name = "&__compare_eqf_func";
                    cp = true;
                }
                else if (Text[j] == '\x02')
                {
                    Name = "&__compare_lef_func";
                    cp = true;
                }
                else if (Text[j] == '\x03')
                {
                    Name = "&__compare_gef_func";
                    cp = true;
                }
                else if (Text[j] == '\x04')
                {
                    Name = "&__compare_nef_func";
                    cp = true;
                }
                else if (Text[j] == '\x05')
                {
                    Name = "&__logic_and_func";
                    cp = true;
                }
                else if (Text[j] == '\x06')
                {
                    Name = "&__logic_or_func";
                    cp = true;
                }
                else if (Text[j] == '+')
                {
                    Name = "~add";
                    cp = true;
                }
                else if (Text[j] == '-')
                {
                    Name = "~sub";
                    cp = true;
                }
                else if (Text[j] == '*')
                {
                    Name = "~mul";
                    cp = true;
                }
                else if (Text[j] == '/')
                {
                    Name = "~div";
                    cp = true;
                }
                else if (Text[j] == '%')
                {
                    Name = "@&__mod_func";
                    cp = true;
                }
                if (cp)
                {
                    pastName = true;
                    StringBuilder sb = new StringBuilder(Text);
                    sb[j] = ',';
                    Text = sb.ToString();
                    Text += ',';
                    Type = TokenType.MethodCall;
                    break;
                }
            }
            bound = "";
            cp = false;
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
                        if (bound.Length > 0 && bound[bound.Length - 1] == '"')
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
                        if (i == Text.Length - 1 && Text[i] == ')')
                        {
                            Text = Text.Remove(Text.Length - 1, 1);
                            i--;
                        }
                        if (ct == Text) return;
                        if (ct.Trim() == "") ct = "\0";
                        else if (ct.StartsWith("\x01") || ct.StartsWith("\x02") || ct.StartsWith("\x03") || ct.StartsWith("\x04") || ct.StartsWith("\x05") || ct.StartsWith("\x06"))
                        {
                            ct = ct.Remove(0, 1);
                            for (int j = 0; j < ct.Length; j++)
                            {
                                if (ct[0] != ' ') break;
                                ct = ct.Remove(0, 1);
                            }
                        }
                        Tokens.Add(new Token(ct));
                        ct = "";
                        cp = false;
                    }
                }
                else
                {
                    if (Text[i] == '"')
                    {
                        pastName = true;
                        Type = TokenType.Literal;
                        name = Text;
                    }
                    else if (Text[i] == '(')
                    {
                        pastName = true;
                        if (Type != TokenType.Assignment) Type = TokenType.MethodCall;
                    }
                    else name += Text[i];
                }
            }
            if (Type == TokenType.Name && (Text.StartsWith("\"") || Text.Trim().StartsWith("0x") || int.TryParse(Text.Trim().Replace("!", ""), out n))) Type = TokenType.Literal;
            if (Name == "") Name = name.Trim();
            if (Name.StartsWith("!"))
            {
                Negate = true;
                Name = Name.Remove(0, 1);
                
            }
            for (int j = 0; j < Text.Length; j++)
            {
                if (Text[0] != ' ') break;
                Text = Text.Remove(0, 1);
            }
            if (Type == TokenType.Literal && Negate)
            {
                Text = Text.Remove(0, 1);
            }
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
    public static class Extensions
    {
        public static string ReplaceEx(this string s, string from, string to)
        {
            if (s == "") return "";
            return Regex.Replace(s, "\\b" + Regex.Escape(from) + "\\b", to);
        }
        public static string ReplaceAndSubstLeadWs(this string s, string from, string to)
        {
            if (s == "") return "";
            return Regex.Replace(s, "\\b" + Regex.Escape(from) + "\\s\\b", to);
        }
    }
}
