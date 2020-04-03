using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace BytecodeAssembler
{
    class Program
    {
        static string IncludePath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "neutrino", "ndk", "include");
        static string LibPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "neutrino", "ndk", "lib");
        public static List<string> includes = new List<string>();
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("ntrasm -help for usage information");
                    Environment.Exit((int)NError.InvalidUsage);
                }
                string source = "";
                string binary = "";
                List<string> flags = new List<string>();
                if (args.Length == 1)
                {
                    if (args[0] == "-help")
                    {
                        Console.WriteLine("Usage:\nntrasm <inputFile> [outputFile] [-options]\nOptions:\n-genRelocTable: include symbol table in NEX header (for dynamic linking support)\n-genModuleFile: create module descriptor file alongside main executable (for dynamically linking the file and accessing its symbols from another project)\n-silent: silent mode, does not write to console output\nfor more info visit lorinet.rf.gd/neutrino/docs/ntrasm");
                        Environment.Exit((int)NError.InvalidUsage);
                    }
                    else
                    {
                        source = args[0];
                        binary = Path.GetFileNameWithoutExtension(args[0]) + ".lex";
                    }
                }
                else if (args.Length == 2)
                {
                    if (args[1].StartsWith("-"))
                    {
                        source = args[0];
                        binary = Path.GetFileNameWithoutExtension(args[0]) + ".lex";
                        flags.Add(args[1]);
                    }
                    else
                    {
                        source = args[0];
                        binary = args[1];
                    }
                }
                else if (args.Length > 2)
                {
                    if (args[1].StartsWith("-"))
                    {
                        source = args[0];
                        binary = Path.GetFileNameWithoutExtension(args[0]) + ".lex";
                        flags.Add(args[1]);
                    }
                    else
                    {
                        source = args[0];
                        binary = args[1];
                    }
                    for (int i = 2; i < args.Length; i++)
                    {
                        flags.Add(args[i]);
                    }
                }
                if(flags.Contains("-silent"))
                    Console.SetOut(TextWriter.Null);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("███╗   ██╗███████╗██╗   ██╗████████╗██████╗ ██╗███╗   ██╗ ██████╗ \n████╗  ██║██╔════╝██║   ██║╚══██╔══╝██╔══██╗██║████╗  ██║██╔═══██╗\n██╔██╗ ██║█████╗  ██║   ██║   ██║   ██████╔╝██║██╔██╗ ██║██║   ██║\n██║╚██╗██║██╔══╝  ██║   ██║   ██║   ██╔══██╗██║██║╚██╗██║██║   ██║\n██║ ╚████║███████╗╚██████╔╝   ██║   ██║  ██║██║██║ ╚████║╚██████╔╝\n╚═╝  ╚═══╝╚══════╝ ╚═════╝    ╚═╝   ╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝ ╚═════╝ ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Intermediate Language Assembler");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(source + " => " + binary);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                List<string> code = new List<string>();
                if (File.Exists(source))
                    code = new List<string>(File.ReadAllLines(source));
                else if (File.Exists(Path.Combine(IncludePath, source)))
                    code = new List<string>(File.ReadAllLines(Path.Combine(IncludePath, source)));
                else
                {
                    RageQuit(NError.SourceMissing, "Cannot open source file: " + source);
                }
                int pc = 0;
                int vi = 0;
                Dictionary<string, int> vars = new Dictionary<string, int>();
                Dictionary<string, int> labels = new Dictionary<string, int>();
                Dictionary<string, string> defines = new Dictionary<string, string>();
                List<byte> pcode = new List<byte>();
                includes.Add(source);
                foreach (string s in code)
                {
                    if (s.StartsWith("#include") && !includes.Contains(s.Split(' ')[1]))
                    {
                        Include(s.Split(' ')[1]);
                    }
                }
                includes.Remove(source);
                foreach (string s in includes)
                {
                    string[] includedCode = new string[0];
                    if (File.Exists(s))
                        includedCode = File.ReadAllLines(s);
                    else if (File.Exists(Path.Combine(IncludePath, s)))
                        includedCode = File.ReadAllLines(Path.Combine(IncludePath, s));
                    else
                    {
                        RageQuit(NError.IncludeMissing, "Cannot open included file '" + s + "'");
                    }
                    foreach(string ic in includedCode)
                    {
                        code.Add(ic);
                    }
                }
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].StartsWith("#include"))
                    {
                        code.RemoveAt(i);
                        i -= 1;
                    }
                }
                foreach (string s in code)
                {
                    if (s.StartsWith("#define"))
                    {
                        string name = s.Split(' ')[1];
                        string cnt = s.Remove(0, 9 + name.Length);
                        if (!defines.ContainsKey(name))
                        {
                            if (cnt.StartsWith("\"")) cnt = cnt.Remove(0, 1).Remove(cnt.Length - 1, 1);
                            defines.Add(name, cnt);
                        }
                    }
                }
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].StartsWith("#define"))
                    {
                        code.RemoveAt(i);
                        i -= 1;
                    }
                }


                for (int i = 0; i < code.Count; i++)
                {
                    foreach (KeyValuePair<string, string> def in defines)
                    {
                        code[i] = Replace(code[i], def.Key, def.Value);
                    }
                }
                code = code.Where(s => s != "").ToList();
                pcode.Add((byte)'N');
                pcode.Add((byte)'E');
                pcode.Add((byte)'X');
                pc = 0;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Detecting executed code");
                Dictionary<KeyValuePair<string, int>, Dictionary<string, int>> extmtds = new Dictionary<KeyValuePair<string, int>, Dictionary<string, int>>();
                List<string> lnximports = new List<string>();
                int obi = 0;
                foreach (string s in code)
                {
                    if (s.StartsWith("link"))
                    {
                        string mdf = Path.GetFileNameWithoutExtension(s.Split(' ')[1]) + ".lmd";
                        string[] mdl = null;
                        if (File.Exists(mdf))
                            mdl = File.ReadAllLines(mdf);
                        else if (File.Exists(Path.Combine(LibPath, mdf)))
                            mdl = File.ReadAllLines(Path.Combine(LibPath, mdf));
                        else
                        {
                            RageQuit(NError.LibraryMissing, "Cannot open linkable module descriptor for library '" + mdf + "'");
                        }
                        lnximports.Add(s.Split(' ')[1]);
                        Dictionary<string, int> mtds = new Dictionary<string, int>();
                        foreach (string m in mdl)
                        {
                            mtds.Add(m.Split(':')[0], int.Parse(m.Split(':')[1]));
                        }
                        extmtds.Add(new KeyValuePair<string, int>(s.Split(' ')[1], obi), mtds);
                        obi += 1;
                    }
                }
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].StartsWith("extcall"))
                    {
                        string sym = code[i].Split(' ')[1];
                        int si = 0;
                        int oi = 0;
                        bool found = false;
                        foreach (var v in extmtds)
                        {
                            if (v.Value.ContainsKey(sym))
                            {
                                si = v.Value[sym];
                                oi = v.Key.Value;
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            RageQuit(NError.SymbolMissing, "Symbol not found: " + sym);
                        }
                        code[i] = "extcall " + oi + " " + si;
                    }
                }
                Dictionary<string, List<string>> sections = new Dictionary<string, List<string>>();
                bool sec = true;
                for (int i = 0; i < code.Count; i++)
                {
                    string lt = "";
                    if (code[i].StartsWith(":"))
                    {
                        lt = code[i].Remove(0, 1);
                        sec = true;
                        List<string> ts = new List<string>();
                        while (sec)
                        {
                            i += 1;
                            if (code[i].StartsWith(":"))
                            {
                                i -= 1;
                                break;
                            }
                            if (code[i] != "" && !code[i].StartsWith(";"))
                                ts.Add(code[i]);
                            if (code[i].StartsWith("ret"))
                                sec = false;
                        }
                        if (!sections.ContainsKey(lt))
                            sections.Add(lt, ts);
                        else RageQuit(NError.AlreadyDefinedLabel, "Label " + lt + " is already defined!");
                    }
                }
                List<string> executedSections = new List<string>();
                List<string> linkedSections = new List<string>();
                if (flags.Contains("-genRelocTable"))
                {
                    //executedSections.Add("main");
                    foreach (KeyValuePair<string, List<string>> kvp in sections)
                    {
                        executedSections.Add(kvp.Key);
                    }
                }
                else
                {
                    executedSections.Add("main");
                    foreach (KeyValuePair<string, List<string>> kvp in sections)
                    {
                        foreach (string cl in kvp.Value)
                        {
                            string[] spl = cl.Split(' ');
                            if (spl.Length > 1)
                            {
                                string lbl = spl[1].Replace(":", "");
                                if (cl.StartsWith("jmp") || cl.StartsWith("call") || cl.StartsWith("goto") || cl.StartsWith("jz") || cl.StartsWith("jnz") || cl.StartsWith("jeq") || cl.StartsWith("jne") || cl.StartsWith("jlt") || cl.StartsWith("jgt") || cl.StartsWith("jle") || cl.StartsWith("jge") || cl.StartsWith("movl"))
                                {
                                    if (!executedSections.Contains(lbl))
                                    {
                                        executedSections.Add(lbl);
                                    }
                                }
                            }
                        }
                    }
                }
                List<string> executedCode = new List<string>();
                pc = 0;
                foreach (string s in executedSections)
                {
                    labels[s] = pc;
                    try
                    {
                        foreach (string cl in sections[s])
                        {
                            executedCode.Add(cl);
                            pc += 1;
                        }
                    }
                    catch
                    {
                        RageQuit(NError.LabelMissing, "Could not find label '" + s + "'");
                    }
                }
                if (flags.Contains("-cleanCode"))
                    File.WriteAllLines("cc.ns", executedCode.ToArray());
                if (flags.Contains("-genRelocTable"))
                {
                    Console.WriteLine("Creating symbol table");
                    foreach (string s in code)
                    {
                        if (s.StartsWith("#exlink"))
                        {
                            linkedSections.Add(s.Split(' ')[1]);
                        }
                    }
                    pcode.Add((byte)'L');
                    pcode.AddRange(BitConverter.GetBytes(linkedSections.Count));
                    int lksi = 0;
                    foreach (string s in linkedSections)
                    {
                        pcode.AddRange(BitConverter.GetBytes(lksi));
                        pcode.AddRange(BitConverter.GetBytes(labels[s]));
                        lksi += 1;
                    }
                    lksi = 0;
                    if (flags.Contains("-genModuleFile"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        List<string> modl = new List<string>();
                        foreach (string s in linkedSections)
                        {
                            modl.Add(s + ":" + lksi);
                            lksi += 1;
                        }
                        Console.WriteLine("Writing module descriptor file " + Path.GetFileNameWithoutExtension(binary) + ".lmd");
                        File.WriteAllLines(Path.GetFileNameWithoutExtension(binary) + ".lmd", modl.ToArray());
                    }
                }
                else pcode.Add((byte)'E');
                pc = 0;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Assembling:");
                Console.ResetColor();
                foreach (string s in executedCode)
                {
                    string[] arg = s.Split(' ');
                    string op = arg[0].ToLower();
                    if (op == "nop")
                    {
                        pcode.Add((byte)OpCode.NOP);
                    }
                    else if (op == "and")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.AND);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "or")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.OR);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "xor")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.XOR);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "shl")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.SHL);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "shr")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.SHR);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "not")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.NOT);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                    }
                    else if (op == "str")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.ST);
                        if (s[5 + arg[1].Length] == '"')
                        {
                            string val = "";
                            for (int i = 6 + arg[1].Length; i < s.Length - 1; i++)
                            {
                                if (i == s.Replace("\\0", "\0").Replace("\\n", "\n").Length)
                                {
                                    val = val.Remove(val.Length - 1, 1);
                                    break;
                                }
                                val += s.Replace("\\0", "\0").Replace("\\n", "\n")[i];
                            }
                            pcode.AddRange(BitConverter.GetBytes(4 + val.Length));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(Encoding.ASCII.GetBytes(val));
                        }
                        else
                        {
                            int value = 0;
                            if (!int.TryParse(s.Remove(0, 5 + arg[1].Length), out value))
                            {
                                value = int.Parse(s.Remove(0, 7 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                            }
                            pcode.AddRange(BitConverter.GetBytes(8));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(BitConverter.GetBytes(value));
                        }
                    }
                    else if (op == "stb")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.STB);
                        byte value = 0;
                        if (!byte.TryParse(s.Remove(0, 5 + arg[1].Length), out value))
                        {
                            value = byte.Parse(s.Remove(0, 7 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.Add(value);
                    }
                    else if (op == "pushb")
                    {
                        pcode.Add((byte)OpCode.PUSHB);
                        byte value = 0;
                        if (!byte.TryParse(s.Remove(0, 6), out value))
                        {
                            value = byte.Parse(s.Remove(0, 8), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.Add(value);
                    }
                    else if (op == "string" || op == "tostr")
                    {
                        pcode.Add((byte)OpCode.TOSTR);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "clr")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        int vkey = vars[arg[1]];
                        if (vkey < 256)
                        {
                            pcode.Add((byte)OpCode.CLRB);
                            pcode.Add((byte)vkey);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.CLR);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        }
                    }
                    else if (op == "mov")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.MOV);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "movl")
                    {
                        if (labels.ContainsKey(arg[1].Replace(":", "")))
                        {
                            if (!vars.ContainsKey(arg[2]))
                            {
                                vars.Add(arg[2], vi);
                                vi += 1;
                            }
                            pcode.Add((byte)OpCode.MOVL);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                            pcode.AddRange(BitConverter.GetBytes(labels[arg[1].Replace(":", "")]));
                        }
                        else
                        {
                            RageQuit(NError.InvalidLabel, "Invalid label '" + arg[1] + "'");
                        }
                    }
                    else if (op == "movpc")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.MOVPC);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                    }
                    else if (op == "split")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        int value = 0;
                        if (!int.TryParse(arg[3], out value))
                        {
                            value = int.Parse(s.Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.Add((byte)OpCode.SPLIT);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(value));
                    }
                    else if (op == "concat")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        int vk1 = vars[arg[1]];
                        int vk2 = vars[arg[2]];
                        if (vk1 < 256 && vk2 < 256)
                        {
                            pcode.Add((byte)OpCode.CONCATB);
                            pcode.Add((byte)vk1);
                            pcode.Add((byte)vk2);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.CONCAT);
                            pcode.AddRange(BitConverter.GetBytes(vk1));
                            pcode.AddRange(BitConverter.GetBytes(vk2));
                        }
                    }
                    else if (op == "integer")
                    {
                        pcode.Add((byte)OpCode.PARSE);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "index")
                    {
                        pcode.Add((byte)OpCode.INDEX);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[3]))
                        {
                            vars.Add(arg[3], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[3]]));
                    }
                    else if (op == "inst")
                    {
                        pcode.Add((byte)OpCode.INSERT);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[3]))
                        {
                            vars.Add(arg[3], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[3]]));
                    }
                    else if(op == "vac")
                    {
                        pcode.Add((byte)OpCode.VAC);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                    }
                    else if(op == "vad")
                    {
                        pcode.Add((byte)OpCode.VAD);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "vpf")
                    {
                        pcode.Add((byte)OpCode.VPF);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "vap")
                    {
                        pcode.Add((byte)OpCode.VAP);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "vade")
                    {
                        pcode.Add((byte)OpCode.VADE);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                    }
                    else if (op == "var")
                    {
                        pcode.Add((byte)OpCode.VAR);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "vai")
                    {
                        pcode.Add((byte)OpCode.VAI);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[3]))
                        {
                            vars.Add(arg[3], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[3]]));
                    }
                    else if (op == "vadi")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.VADI);
                        if (s[6 + arg[1].Length] == '"')
                        {
                            string val = "";
                            for (int i = 7 + arg[1].Length; i < s.Length - 1; i++)
                            {
                                if (i == s.Replace("\\0", "\0").Replace("\\n", "\n").Length)
                                {
                                    val = val.Remove(val.Length - 1, 1);
                                    break;
                                }
                                val += s.Replace("\\0", "\0").Replace("\\n", "\n")[i];
                            }
                            pcode.AddRange(BitConverter.GetBytes(4 + val.Length));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(Encoding.ASCII.GetBytes(val));
                        }
                        else
                        {
                            int value = 0;
                            if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                            {
                                value = int.Parse(s.Remove(0, 8 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                            }
                            pcode.AddRange(BitConverter.GetBytes(8));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(BitConverter.GetBytes(value));
                        }
                    }
                    else if(op == "val")
                    {
                        pcode.Add((byte)OpCode.VAL);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "vas")
                    {
                        pcode.Add((byte)OpCode.VAS);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[3]))
                        {
                            vars.Add(arg[3], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[3]]));
                    }
                    else if (op == "size")
                    {
                        pcode.Add((byte)OpCode.SIZE);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "append")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        int vk1 = vars[arg[1]];
                        int vk2 = vars[arg[2]];
                        if (vk1 < 256 && vk2 < 256)
                        {
                            pcode.Add((byte)OpCode.APPENDB);
                            pcode.Add((byte)vk1);
                            pcode.Add((byte)vk2);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.APPEND);
                            pcode.AddRange(BitConverter.GetBytes(vk1));
                            pcode.AddRange(BitConverter.GetBytes(vk2));
                        }
                    }
                    else if (op == "pushblk")
                    {
                        pcode.Add((byte)OpCode.PUSHBLK);
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[3]))
                        {
                            vars.Add(arg[3], vi);
                            vi += 1;
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[3]]));
                    }
                    else if (op == "add")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.ADD);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "sub")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.SUB);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "mul")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.MUL);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "div")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.DIV);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                    }
                    else if (op == "inc")
                    {
                        pcode.Add((byte)OpCode.INC);
                        int value = 0;
                        if (!int.TryParse(s.Remove(0, 5 + arg[1].Length), out value))
                        {
                            value = int.Parse(s.Remove(0, 6 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(value));
                    }
                    else if (op == "dec")
                    {
                        pcode.Add((byte)OpCode.DEC);
                        int value = 0;
                        if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                        {
                            value = int.Parse(s.Remove(0, 5 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(value));
                    }
                    else if (op == "imul")
                    {
                        pcode.Add((byte)OpCode.IMUL);
                        int value = 0;
                        if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                        {
                            value = int.Parse(s.Remove(0, 6 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(value));
                    }
                    else if (op == "idiv")
                    {
                        pcode.Add((byte)OpCode.IDIV);
                        int value = 0;
                        if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                        {
                            value = int.Parse(s.Remove(0, 6 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        pcode.AddRange(BitConverter.GetBytes(value));
                    }
                    else if (op == "cmp")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        int vk1 = vars[arg[1]];
                        int vk2 = vars[arg[2]];
                        if (vk1 < 256 && vk2 < 256)
                        {
                            pcode.Add((byte)OpCode.CMPB);
                            pcode.Add((byte)vk1);
                            pcode.Add((byte)vk2);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.CMP);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        }
                    }
                    else if (op == "cz")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        int vkey = vars[arg[1]];
                        if (vkey < 256)
                        {
                            pcode.Add((byte)OpCode.CZB);
                            pcode.Add((byte)vkey);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.CZ);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        }
                    }
                    else if (op == "cmpi")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        if (s[6 + arg[1].Length] == '"')
                        {
                            pcode.Add((byte)OpCode.CMPS);
                            string val = "";
                            for (int i = 7 + arg[1].Length; i < s.Length - 1; i++)
                            {
                                if (i == s.Replace("\\0", "\0").Replace("\\n", "\n").Length)
                                {
                                    val = val.Remove(val.Length - 1, 1);
                                    break;
                                }
                                val += s.Replace("\\0", "\0").Replace("\\n", "\n")[i];
                            }
                            pcode.AddRange(BitConverter.GetBytes(4 + val.Length));
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                            pcode.AddRange(Encoding.ASCII.GetBytes(val));
                        }
                        else
                        {
                            int value = 0;
                            if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                            {
                                value = int.Parse(s.Remove(0, 8 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                            }
                            int vkey = vars[arg[1]];
                            if (vkey < 256 && value < 256)
                            {
                                pcode.Add((byte)OpCode.CMPIB);
                                pcode.Add((byte)vkey);
                                pcode.Add((byte)value);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.CMPI);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                                pcode.AddRange(BitConverter.GetBytes(value));
                            }
                        }
                    }
                    else if (op == "jmp" || op == "goto" || op == "call")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int addr = labels[arg[1]];
                            if (addr < 256)
                            {
                                pcode.Add((byte)OpCode.SJ);
                                pcode.Add((byte)addr);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JMP);
                                pcode.AddRange(BitConverter.GetBytes(addr));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jeq")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJE);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JEQ);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jne")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJNE);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JNE);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jle")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJLE);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JLE);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jge")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJGE);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JGE);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jlt")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJL);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JLT);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jgt")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJG);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JGT);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jz")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJZ);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JZ);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "jnz")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(arg[1]))
                        {
                            int vkey = labels[arg[1]];
                            if (vkey < 256)
                            {
                                pcode.Add((byte)OpCode.SJNZ);
                                pcode.Add((byte)vkey);
                            }
                            else
                            {
                                pcode.Add((byte)OpCode.JNZ);
                                pcode.AddRange(BitConverter.GetBytes(vkey));
                            }
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "emit")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        pcode.Add((byte)OpCode.EMIT);
                        pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                    }
                    else if (op == "ret")
                    {
                        pcode.Add((byte)OpCode.RET);
                    }
                    else if (op == "lj")
                    {
                        arg[1] = arg[1].Replace(":", "");
                        if (labels.ContainsKey(args[1]))
                        {
                            pcode.Add((byte)OpCode.LJ);
                            pcode.AddRange(BitConverter.GetBytes(labels[arg[1]]));
                        }
                        else RageQuit(NError.LabelMissing, "Label not found: " + arg[1]);
                    }
                    else if (op == "ints")
                    {
                        pcode.Add((byte)OpCode.INTS);
                        byte interrupt = 0;
                        if (!byte.TryParse(arg[1], out interrupt))
                        {
                            interrupt = byte.Parse(arg[1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        if (s[6 + arg[1].Length] == '"')
                        {
                            string val = "";
                            for (int i = 7 + arg[1].Length; i < s.Length - 1; i++)
                            {
                                if (i == s.Replace("\\0", "\0").Replace("\\n", "\n").Length)
                                {
                                    val = val.Remove(val.Length - 1, 1);
                                    break;
                                }
                                val += s.Replace("\\0", "\0").Replace("\\n", "\n")[i];
                            }
                            pcode.AddRange(BitConverter.GetBytes(1 + val.Length));
                            pcode.Add(interrupt);
                            pcode.AddRange(Encoding.ASCII.GetBytes(val));
                        }
                        else
                        {
                            int value = 0;
                            if (!int.TryParse(s.Remove(0, 6 + arg[1].Length), out value))
                            {
                                value = int.Parse(s.Remove(0, 6 + arg[1].Length), System.Globalization.NumberStyles.HexNumber);
                            }
                            pcode.AddRange(BitConverter.GetBytes(5));
                            pcode.Add(interrupt);
                            pcode.AddRange(BitConverter.GetBytes(value));
                        }
                    }
                    else if (op == "int" && arg.Length == 2)
                    {
                        pcode.Add((byte)OpCode.INTS);
                        pcode.AddRange(BitConverter.GetBytes(1));
                        byte interrupt = 0;
                        if (!byte.TryParse(arg[1], out interrupt))
                        {
                            interrupt = byte.Parse(arg[1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.Add(interrupt);
                    }
                    else if (op == "int" && arg.Length > 2)
                    {
                        if (!vars.ContainsKey(arg[2]))
                        {
                            vars.Add(arg[2], vi);
                            vi += 1;
                        }
                        byte interrupt = 0;
                        if (!byte.TryParse(arg[1], out interrupt))
                        {
                            interrupt = byte.Parse(arg[1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        int vark = vars[arg[2]];
                        if (vark < 256)
                        {
                            pcode.Add((byte)OpCode.INTB);
                            pcode.Add(interrupt);
                            pcode.Add((byte)vark);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.INT);
                            pcode.Add(interrupt);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[2]]));
                        }
                    }
                    else if(op == "bits")
                    {
                        pcode.Add((byte)OpCode.BITS);
                        byte bits = 0;
                        if (!byte.TryParse(arg[1], out bits))
                        {
                            bits = byte.Parse(arg[1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                        }
                        pcode.Add(bits);
                    }
                    else if (op == "break")
                    {
                        pcode.Add((byte)OpCode.BREAK);
                    }
                    else if (op == "push")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        int vkey = vars[arg[1]];
                        if (vkey < 256)
                        {
                            pcode.Add((byte)OpCode.VPUSHB);
                            pcode.Add((byte)vkey);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.PUSH);
                            pcode.AddRange(BitConverter.GetBytes(vars[arg[1]]));
                        }
                    }
                    else if (op == "pop")
                    {
                        if (!vars.ContainsKey(arg[1]))
                        {
                            vars.Add(arg[1], vi);
                            vi += 1;
                        }
                        int vark = vars[arg[1]];
                        if (vark < 256)
                        {
                            pcode.Add((byte)OpCode.POPB);
                            pcode.Add((byte)vark);
                        }
                        else
                        {
                            pcode.Add((byte)OpCode.POP);
                            pcode.AddRange(BitConverter.GetBytes(vark));
                        }
                    }
                    else if (op == "spop")
                    {
                        pcode.Add((byte)OpCode.SPOP);
                    }
                    else if (op == "spush" || op == "ldstr")
                    {
                        pcode.Add((byte)OpCode.SPUSH);
                        string val = "";
                        int pushval = 0;
                        if (s[6] == '"')
                        {
                            for (int i = 7; i < s.Length - 1; i++)
                            {
                                if (i == s.Replace("\\0", "\0").Replace("\\n", "\n").Length)
                                {
                                    val = val.Remove(val.Length - 1, 1);
                                    break;
                                }
                                val += s.Replace("\\0", "\0").Replace("\\n", "\n")[i];
                            }
                            pcode.AddRange(BitConverter.GetBytes(val.Length));
                            pcode.AddRange(Encoding.ASCII.GetBytes(val));
                        }
                        else
                        {
                            if (!int.TryParse(arg[1], out pushval))
                            {
                                pushval = int.Parse(arg[1].Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                            }
                            pcode.AddRange(BitConverter.GetBytes(4));
                            pcode.AddRange(BitConverter.GetBytes(pushval));
                        }
                    }
                    else if (op == "top")
                    {
                        pcode.Add((byte)OpCode.TOP);
                        pcode.AddRange(BitConverter.GetBytes(int.Parse(arg[1])));
                    }
                    else if (op == "link")
                    {
                        pcode.Add((byte)OpCode.LINK);
                        pcode.Add((byte)arg[1].Length);
                        pcode.AddRange(Encoding.ASCII.GetBytes(arg[1]));
                    }
                    else if (op == "extcall")
                    {
                        pcode.Add((byte)OpCode.EXTCALL);
                        pcode.AddRange(BitConverter.GetBytes(int.Parse(arg[1])));
                        pcode.AddRange(BitConverter.GetBytes(int.Parse(arg[2])));
                    }
                    else if (op == "halt" || op == "leave")
                    {
                        pcode.Add((byte)OpCode.HALT);
                    }
                    else if (!op.StartsWith(":") && !op.StartsWith(";"))
                    {
                        RageQuit(NError.InvalidTerm, "Invalid term '" + op + "'");
                    }
                    if (labels.ContainsValue(pc))
                    {
                        Console.WriteLine(labels.FirstOrDefault(x => x.Value == pc).Key);
                    }
                    pc += 1;
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Writing NEX executable");
                File.WriteAllBytes(binary, pcode.ToArray());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Operation completed in " + sw.ElapsedMilliseconds + "ms!");
                Console.ResetColor();
                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                RageQuit(NError.CompilerError, "Compiler error: " + ex.ToString());
            }
        }
        static string Replace(string input, string from, string to)
        {
            if (input == null) return null;
            return Regex.Replace(input, "\\b" + Regex.Escape(from) + "\\b", to);
        }
        public static void Include(string file)
        {
            if(!includes.Contains(file)) includes.Add(file);
            string[] c = null;
            if (File.Exists(file))
                c = File.ReadAllLines(file);
            else if (File.Exists(Path.Combine(IncludePath, file)))
                c = File.ReadAllLines(Path.Combine(IncludePath, file));
            else
            {
                RageQuit(NError.IncludeMissing, "Cannot open included file '" + file + "'");
            }
            foreach (string line in c)
            {
                if(line.StartsWith("#include") && !includes.Contains(line.Split(' ')[1]))
                {
                    includes.Add(line.Split(' ')[1]);
                    Include(line.Split(' ')[1]);
                }
            }
        }
        public static void RageQuit(NError ne, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR NIL" + -(int)ne + ": " + message);
            Console.ResetColor();
            Environment.Exit((int)ne);
        }
    }
    enum NError
    {
        SourceMissing = -1,
        IncludeMissing = -2,
        SymbolMissing = -3,
        LabelMissing = -4,
        InvalidLabel = -5,
        InvalidTerm = -6,
        LibraryMissing = -7,
        CompilerError = -8,
        InvalidUsage = -9,
        AlreadyDefinedLabel = -10,
    }
    enum OpCode
    {
        NOP = 0x01,
        AND = 0x11,
        OR = 0x12,
        XOR = 0x13,
        SHL = 0x14,
        SHR = 0x15,
        NOT = 0x16,
        ST = 0x20,
        TOSTR = 0x21,
        CLR = 0x22,
        MOV = 0x23,
        CONCAT = 0x24,
        PARSE = 0x25,
        SPLIT = 0x26,
        INDEX = 0x27,
        SIZE = 0x28,
        APPEND = 0x29,
        PUSHBLK = 0x2A,
        STB = 0x2B,
        PUSHB = 0x2C,
        CONCATB = 0x2D,
        APPENDB = 0x2E,
        CLRB = 0x2F,
        MOVL = 0x30,
        ADD = 0x40,
        SUB = 0x41,
        MUL = 0x42,
        DIV = 0x43,
        INC = 0x44,
        DEC = 0x45,
        IMUL = 0x46,
        IDIV = 0x47,
        CMP = 0x50,
        CZ = 0x51,
        CMPS = 0x52,
        CMPI = 0x53,
        CMPB = 0x54,
        CZB = 0x55,
        CMPIB = 0x56,
        INSERT = 0x57,
        VAC = 0x58,  // array create
        VAI = 0x59,  // array index get
        VAD = 0x5A,  // array add
        VAR = 0x5B,  // array remove
        VADE = 0x5C, // array deallocate
        VAP = 0x5D,  // array append
        VPF = 0x5E,  // array push front
        VADI = 0x5F, // array add immediate value
        JMP = 0x60,
        JEQ = 0x61,
        JNE = 0x62,
        JLE = 0x63,
        JGE = 0x64,
        JLT = 0x65,
        JGT = 0x66,
        JZ = 0x67,
        JNZ = 0x68,
        RET = 0x69,
        EMIT = 0x6A,
        MOVPC = 0x6B,
        LJ = 0x6C,
        SJ = 0x6D,
        SJE = 0x6E,
        SJNE = 0x6F,
        SJLE = 0x70,
        SJGE = 0x71,
        SJL = 0x72,
        SJG = 0x73,
        SJZ = 0x74,
        SJNZ = 0x75,
        VAL = 0x76,
        VAS = 0x77,
        INTS = 0x80,
        INT = 0x81,
        BREAK = 0x82,
        INTB = 0x83,
        BITS = 0x84,
        PUSH = 0x90,
        POP = 0x91,
        POPA = 0x92,
        SPUSH = 0x93,
        TOP = 0x94,
        SPOP = 0x95,
        POPB = 0x96,
        VPUSHB = 0x97,
        LINK = 0x98,
        EXTCALL = 0x99,
        HALT = 0xB0
    }
}
