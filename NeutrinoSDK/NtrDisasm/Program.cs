using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NtrDisasm
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                string source = args[0];
                string output = Path.GetFileNameWithoutExtension(args[0]) + ".ns";
                if(args.Length > 1)
                {
                    output = args[1];
                }
                Instruction[] Bytecode = Instruction.DecodeInstructions(File.ReadAllBytes(source));
                List<string> Code = new List<string>();
                foreach(Instruction i in Bytecode)
                {
                    if (i.OpCode == OpCode.nop || i.OpCode == OpCode.ret || i.OpCode == OpCode.halt || i.OpCode == OpCode.spop)
                        Code.Add(i.OpCode.ToString());
                    else if (i.OpCode == OpCode.Break)
                        Code.Add("break");
                    else if (i.OpCode == OpCode.tostr || i.OpCode == OpCode.mov || i.OpCode == OpCode.concat || i.OpCode == OpCode.integer || i.OpCode == OpCode.size || i.OpCode == OpCode.append || i.OpCode == OpCode.add || i.OpCode == OpCode.sub || i.OpCode == OpCode.mul || i.OpCode == OpCode.div || i.OpCode == OpCode.inc || i.OpCode == OpCode.dec || i.OpCode == OpCode.imul || i.OpCode == OpCode.idiv || i.OpCode == OpCode.cmpi || i.OpCode == OpCode.cmp)
                        Code.Add(i.OpCode.ToString() + " " + BitConverter.ToInt32(i.Parameters, 0) + " " + BitConverter.ToInt32(i.Parameters, 4));
                    else if (i.OpCode == OpCode.clr || i.OpCode == OpCode.cz || i.OpCode == OpCode.jmp || i.OpCode == OpCode.jeq || i.OpCode == OpCode.jne || i.OpCode == OpCode.jle || i.OpCode == OpCode.jge || i.OpCode == OpCode.jlt || i.OpCode == OpCode.jgt || i.OpCode == OpCode.jz || i.OpCode == OpCode.jnz || i.OpCode == OpCode.push || i.OpCode == OpCode.pop || i.OpCode == OpCode.top || i.OpCode == OpCode.emit || i.OpCode == OpCode.movpc)
                        Code.Add(i.OpCode.ToString() + " " + BitConverter.ToInt32(i.Parameters, 0));
                    else if (i.OpCode == OpCode.split || i.OpCode == OpCode.index || i.OpCode == OpCode.pushblk)
                        Code.Add(i.OpCode.ToString() + " " + BitConverter.ToInt32(i.Parameters, 0) + " " + BitConverter.ToInt32(i.Parameters, 4) + " " + BitConverter.ToInt32(i.Parameters, 8));
                    else if (i.OpCode == OpCode.INT)
                        Code.Add("int " + i.Parameters[0] + " " + BitConverter.ToInt32(i.Parameters, 1));
                    else if (i.OpCode == OpCode.pushb)
                        Code.Add("pushb " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.stb)
                        Code.Add("stb " + BitConverter.ToInt32(i.Parameters, 0) + " " + i.Parameters[4]);
                    else if (i.OpCode == OpCode.sj)
                        Code.Add("jmp " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.popb)
                        Code.Add("pop " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.intb)
                        Code.Add("int " + i.Parameters[0] + " " + i.Parameters[1]);
                    else if (i.OpCode == OpCode.vpushb)
                        Code.Add("push " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.concatb)
                        Code.Add("concat " + i.Parameters[0] + " " + i.Parameters[1]);
                    else if (i.OpCode == OpCode.appendb)
                        Code.Add("append " + i.Parameters[0] + " " + i.Parameters[1]);
                    else if (i.OpCode == OpCode.clrb)
                        Code.Add("clr " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sje)
                        Code.Add("jeq " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjne)
                        Code.Add("jne " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjl)
                        Code.Add("jlt " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjg)
                        Code.Add("jgt " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjle)
                        Code.Add("jle " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjge)
                        Code.Add("jge " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjz)
                        Code.Add("jz " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.sjnz)
                        Code.Add("jnz " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.cmpb)
                        Code.Add("cmp " + i.Parameters[0] + " " + i.Parameters[1]);
                    else if (i.OpCode == OpCode.cmpib)
                        Code.Add("cmpi " + i.Parameters[0] + " " + i.Parameters[1]);
                    else if (i.OpCode == OpCode.czb)
                        Code.Add("cz " + i.Parameters[0]);
                    else if (i.OpCode == OpCode.str || i.OpCode == OpCode.cmps)
                    {
                        string s = "";
                        for (int j = 4; j < i.Parameters.Length; j++)
                            s += (char)i.Parameters[j];
                        Code.Add(i.OpCode.ToString().Replace("cmps", "cmpi") + " " + BitConverter.ToInt32(i.Parameters, 0) + " \"" + s + "\"");
                    }
                    else if (i.OpCode == OpCode.ints)
                    {
                        string s = "";
                        for (int j = 1; j < i.Parameters.Length; j++)
                            s += (char)i.Parameters[j];
                        Code.Add("ints " + i.Parameters[0] + " \"" + s + "\"");
                    }
                    else if (i.OpCode == OpCode.ldstr)
                    {
                        string s = "";
                        for (int j = 0; j < i.Parameters.Length; j++)
                            s += (char)i.Parameters[j];
                        Code.Add(i.OpCode.ToString() + " \"" + s.Replace("\n", "\\n") + "\"");
                    }
                }
                Dictionary<string, int> labels = new Dictionary<string, int>();
                labels.Add("main", 0);
                for(int i = 0; i < Code.Count; i++)
                {
                    string s = Code[i];
                    if(s.StartsWith("jmp") || s.StartsWith("jeq") || s.StartsWith("jne") || s.StartsWith("jle") || s.StartsWith("jge") || s.StartsWith("jlt") || s.StartsWith("jgt") || s.StartsWith("jz") || s.StartsWith("jnz") || s.StartsWith("movl") || s.StartsWith("lj"))
                    {
                        int address = int.Parse(s.Split(' ')[1]);
                        if(!labels.ContainsKey("NIL_" + address))
                            labels.Add("NIL_" + address, address);
                        if(address != 0)
                            Code[i] = Code[i].Replace(address.ToString(), "NIL_" + address);
                        else
                            Code[i] = Code[i].Replace(address.ToString(), "main");
                    }
                }
                var Labels = labels.OrderBy(x => x.Value);
                int torlodas = 0;
                foreach(KeyValuePair<string, int> kvp in Labels)
                {
                    Code.Insert(kvp.Value + torlodas, ":" + kvp.Key);
                    torlodas += 1;
                }
                File.WriteAllLines(output, Code.ToArray());
            }
            else
            {
                Console.WriteLine("ntrdasm <file.lex> [output.ns]");
                Environment.Exit(-1);
            }
        }
    }
    class Instruction
    {
        public OpCode OpCode { get; set; }
        public byte[] Parameters { get; set; }
        public Instruction(byte op, byte[] param)
        {
            OpCode = (OpCode)op;
            Parameters = param;
        }
        public static Instruction[] DecodeInstructions(byte[] code)
        {
            if (code.Length < 3)
                throw new Exception("Corrupted executable!");
            string magic = "";
            magic += (char)code[0];
            magic += (char)code[1];
            magic += (char)code[2];
            if (magic != "NEX")
                throw new Exception("Corrupted executable!");
            char rti = (char)code[3];
            int codeindex = 4;
            if (rti == 'R') codeindex += BitConverter.ToInt32(code, 4) * 4;
            List<byte> bc = new List<byte>();
            for (int i = codeindex; i < code.Length; i++)
            {
                bc.Add(code[i]);
            }
            byte[] bytecode = new byte[bc.Count];
            for (int i = 0; i < bc.Count; i++)
            {
                bytecode[i] = bc[i];
            }
            List<Instruction> instr = new List<Instruction>();
            int cntr = 0;
            while (cntr < bytecode.Length)
            {
                byte op = bytecode[cntr];
                cntr += 1;
                int length;
                if (op == (byte)OpCode.str || op == (byte)OpCode.ints || op == (byte)OpCode.ldstr || op == (byte)OpCode.cmps)
                {
                    length = BitConverter.ToInt32(bytecode, cntr);
                    cntr += 4;
                }
                else if (op == (byte)OpCode.nop || op == (byte)OpCode.ret || op == (byte)OpCode.halt || op == (byte)OpCode.Break || op == (byte)OpCode.spop)
                {
                    length = 0;
                }
                else if (op == (byte)OpCode.tostr || op == (byte)OpCode.mov || op == (byte)OpCode.concat || op == (byte)OpCode.integer || op == (byte)OpCode.size || op == (byte)OpCode.append || op == (byte)OpCode.add || op == (byte)OpCode.sub || op == (byte)OpCode.mul || op == (byte)OpCode.div || op == (byte)OpCode.inc || op == (byte)OpCode.dec || op == (byte)OpCode.imul || op == (byte)OpCode.idiv || op == (byte)OpCode.cmpi || op == (byte)OpCode.cmp)
                {
                    length = 8;
                }
                else if (op == (byte)OpCode.clr || op == (byte)OpCode.cz || op == (byte)OpCode.jmp || op == (byte)OpCode.jeq || op == (byte)OpCode.jne || op == (byte)OpCode.jle || op == (byte)OpCode.jge || op == (byte)OpCode.jlt || op == (byte)OpCode.jgt || op == (byte)OpCode.jz || op == (byte)OpCode.jnz || op == (byte)OpCode.push || op == (byte)OpCode.pop || op == (byte)OpCode.top || op == (byte)OpCode.emit)
                {
                    length = 4;
                }
                else if (op == (byte)OpCode.split || op == (byte)OpCode.index || op == (byte)OpCode.pushblk)
                {
                    length = 12;
                }
                else if (op == (byte)OpCode.INT || op == (byte)OpCode.stb)
                {
                    length = 5;
                }
                else if(op == (byte)OpCode.pushb || op == (byte)OpCode.sj || op == (byte)OpCode.popb || op == (byte)OpCode.vpushb || op == (byte)OpCode.clrb || op == (byte)OpCode.sje || op == (byte)OpCode.sjne || op == (byte)OpCode.sjl || op == (byte)OpCode.sjg || op == (byte)OpCode.sjle || op == (byte)OpCode.sjge || op == (byte)OpCode.sjz || op == (byte)OpCode.sjnz || op == (byte)OpCode.czb)
                {
                    length = 1;
                }
                else if(op == (byte)OpCode.intb || op == (byte)OpCode.concatb || op == (byte)OpCode.appendb || op == (byte)OpCode.cmpb || op == (byte)OpCode.cmpib)
                {
                    length = 2;
                }
                else
                {
                    length = bytecode[cntr];
                    cntr += 1;
                }
                List<byte> parameters = new List<byte>();
                int paramindex = 0;
                while (paramindex < length)
                {
                    parameters.Add(bytecode[cntr]);
                    cntr += 1;
                    paramindex += 1;
                }
                instr.Add(new Instruction(op, parameters.ToArray()));
            }
            return instr.ToArray();
        }
    }
    enum OpCode
    {
        nop = 0x01,
        str = 0x20,
        tostr = 0x21,
        clr = 0x22,
        mov = 0x23,
        concat = 0x24,
        integer = 0x25,
        split = 0x26,
        index = 0x27,
        size = 0x28,
        append = 0x29,
        pushblk = 0x2A,
        stb = 0x2B,
        pushb = 0x2C,
        concatb = 0x2D,
        appendb = 0x2E,
        clrb = 0x2F,
        add = 0x40,
        sub = 0x41,
        mul = 0x42,
        div = 0x43,
        inc = 0x44,
        dec = 0x45,
        imul = 0x46,
        idiv = 0x47,
        cmp = 0x50,
        cz = 0x51,
        cmps = 0x52,
        cmpi = 0x53,
        cmpb = 0x54,
        czb = 0x55,
        cmpib = 0x56,
        jmp = 0x60,
        jeq = 0x61,
        jne = 0x62,
        jle = 0x63,
        jge = 0x64,
        jlt = 0x65,
        jgt = 0x66,
        jz = 0x67,
        jnz = 0x68,
        ret = 0x69,
        emit = 0x6A,
        movpc = 0x6B,
        lj = 0x6C,
        sj = 0x6D,
        sje = 0x6E,
        sjne = 0x6F,
        sjle = 0x70,
        sjge = 0x71,
        sjl = 0x72,
        sjg = 0x73,
        sjz = 0x74,
        sjnz = 0x75,
        ints = 0x80,
        INT = 0x81,
        Break = 0x82,
        intb = 0x83,
        push = 0x90,
        pop = 0x91,
        popa = 0x92,
        ldstr = 0x93,
        top = 0x94,
        spop = 0x95,
        popb = 0x96,
        vpushb = 0x97,
        halt = 0xB0
    }
}
