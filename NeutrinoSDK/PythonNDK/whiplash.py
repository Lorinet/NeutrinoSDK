import re
import sys
import os.path

BLOCK_METHOD = 0
BLOCK_WHILE = 1
BLOCK_IF = 2
BLOCK_FOR = 3

TOKEN_NAME = 0
TOKEN_CALL = 1
TOKEN_ASSIGNMENT = 2
TOKEN_VOID = 3
TOKEN_LITERAL = 4

class Token:
    tokens = []
    name = ""
    text = ""
    child_index = 0
    negate = False
    token_type = TOKEN_NAME
    def __init__(self):
        pass
    def __init__(self, t):
        pass

class Block:
    name = ""
    block_type = BLOCK_METHOD
    condition = Token()
    if_count = 0
    while_count = 0
    for_count = 0
    def __init__(self, n, t):
        self.name = n
        self.block_type = t
    def __init__(self, n, t, c):
        self.name = n
        self.block_type = t
        self.condition = c
    def to_str(self):
        return self.name

def peek(l):
    return l[len(l) - 1]

version = "0.11f"
tab = 0
prev_tab = 0
retp = False
il = []
reg = {}
meth = []
var = []
methcode = {}
importDirectories = "C:\\Neutrino\\ndk\\whiplash\\"

def error(err, lnu, fil):
    print("Error: " + err)
    print("At line " + str(lnu) + " of file " + fil)
    exit(-1)

def get_var_name(var):
    pass

def process_statement(stmt):
    pass

def parse_line(s, lnu, fil):
    lin = s
    for g in reg.keys():
        m = re.search(reg[g], lin)
        if m:
            if g == "import":
                ipa = "..\\" + m.group(0) + ".py"
                if not os.path.exists(ipa):
                    ipa = importDirectories + m.group(0) + ".py"
                if not os.path.exists(ipa):
                    error("cannot find file: " + m.group(0), lnu, fil)
            elif g == "include":
                il.append("#include " + m.group(0) + ".ns")
            elif g == "def":
                if m.group(0) in methcode:
                    error("redefinition of function " + m.group(0), lnu, fil)
                meth.append(Block(m.group(0), BLOCK_METHOD))
                methcode[m.group(0)] = []
                if m.group(0) == "main":
                    methcode[m.group(0)].append("link whiprt.lnx")
                cargs = re.split(m.group(1), "\\s*[,]\\s*")
                for ax in reversed(range(0, len(cargs) - 1)):
                    if cargs[ax].strip() != "":
                        methcode[peek(meth).name].append("pop " + peek(meth).name + "!" + cargs[ax])
                        var.append(peek(meth).name + "!" + cargs[ax])
            elif g == "inline_il":
                methcode[peek(meth).name].append(m.group(0))
            elif g == "if":
                nm = peek(meth).name + "!&!cond"
                if nm not in var:
                    var.append(nm)
                nm = "_if_" + peek(meth).name + "@" + str(peek(meth).if_count)
                process_statement("&!cond = " + m.group(0))
                methcode[peek(meth).name].append("mov " + peek(meth).name + "!&!cond " + peek(meth).name + "!&!orig_cond")
                methcode[peek(meth).name].append("cmpi " + peek(meth).name + "!&!cond 1")
                methcode[peek(meth).name].append("jeq " + nm)
                meth[len(meth) - 1].if_count += 1
                meth.append(Block(nm, BLOCK_IF, Token(m.group(0))))
                methcode[nm] = []
            elif g == "elif":
                nm = peek(meth).name + "!&!cond"
                if nm not in var:
                    var.append(nm)
                nm = "_elif_" + peek(meth).name + "@" + str(peek(meth).if_count)
                process_statement("&!cond = !&!orig_cond && " + m.group(0))
                methcode[peek(meth).name].append("cmpi " + peek(meth).name + "!&!cond 1")
                methcode[peek(meth).name].append("jeq " + nm)
                meth[len(meth) - 1].if_count += 1
                meth.append(Block(nm, BLOCK_IF, Token(m.group(0))))
                methcode[nm] = []
                methcode[nm].append("str " + meth[len(meth) - 2] + "!&!orig_cond 1")
            elif g == "else":
                nm = "_else_" + peek(meth).name + "@" + str(peek(meth).if_count);
                methcode[peek(meth).name].append("cmpi " + peek(meth).name + "!&!orig_cond 1")
                methcode[peek(meth).name].append("jne " + nm)
                meth[len(meth) - 1].if_count += 1
                meth.append(Block(nm, BLOCK_IF, Token("")))
                methcode[nm] = []
            elif g == "while":
                nm = peek(meth).name + "!&!cond"
                if nm not in var:
                    var.append(nm)
                nm = "_while_" + peek(meth).name + "@" + str(peek(meth).while_count)
                methcode[peek(meth).name].append("jmp " + nm)
                meth[len(meth) - 1].while_count += 1
                meth.append(Block(nm. BLOCK_WHILE, Token(m.group(0))))
                methcode[nm] = []
                process_statement("&!cond = " + m.group(0))
                methcode[peek(meth).name][len(methcode[peek(meth).name]) - 1] = "pop " + nm + "!&!cond"
                methcode[peek(meth).name].append("cmpi " + nm + "!&!cond 1")
                methcode[peek(meth).name].append("ljne &__ret_func")
            elif g == "return":
                if peek(meth).block_type == BLOCK_METHOD:
                    methcode[peek(meth).name].append("ret")
                    meth.pop()
                    retp = True
                else:
                    methcode[peek(meth).name].append("lj &__ret_func")
            elif g == "return_val":
                tkr = parse_line(m.group(0), lnu, fil)
                if tkr:
                    if tkr.token_type == TOKEN_LITERAL:
                        methcode[peek(meth).name].append("spush " + tkr.text)
                    elif tkr.token_type == TOKEN_NAME:
                        methcode[peek(meth).name].append("push " + get_var_name(tkr.name))
                if peek(meth).block_type == BLOCK_METHOD:
                    

    return Token()

print("Neutrino Python Compiler")

if len(sys.argv) != 2:
    print("usage: python whiplash.py <file.py> <out.ns>")
    exit(-1)

infile = sys.argv[1]
outfile = sys.argv[2]
il.append("; Compiled with Whiplash Python Compiler version " + version)
il.append("; File: " + infile)
il.append(":&__ret_func")
il.append("ret")
lines = []
with open(infile) as f:
    lines = f.readlines()
lines.append("")
reg["include"] = "^\\s*include\\s+(\\w+)\\s*$"
reg["import"] = "^\\s*import\\s+(\\w+)\\s*$"
reg["inline_il"] = "^\\s*!\\s*[(][']([#\\w\\s\\(\\)\",=<>!.]*)['][)]\\s*"
reg["def"] = "^\\s*def\\s+(\\w+)\\s*[(]((\\s*\\w+\\s*[,]{0,1}\\s*)*)[)]:$"
reg["return"] = "^\\s*return\\s*$"
reg["return_val"] = "^\\s*return\\s+(\\w+)$"
reg["return_call"] = "^\\s*return\\s*([@\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:]+)\\s*"
reg["method_call"] = "^\\s*([@\\w]+)\\s*[(]([\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:]*)[)]\\s*"
reg["assign_var_var"] = "^\\s*(\\w+)\\s*=\\s*((?![\\d]{1})\\w+)\\s*$"
reg["assign_var_call"] = "^\\s*(\\w+)\\s*=\\s*([@\\w\\s\\(\\),=<>!.\"'\\\\+\\-\\*/%:]+)\\s*"
reg["if"] = "^\\s*if\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%:]+)\\s*:\\s*$"
reg["elif"] = "^\\s*elif\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%:]+)\\s*:\\s*$"
reg["else"] = "^\\s*else\\s*:\\s*$"
reg["while"] = "^\\s*while\\s*([\\w\\s=!<>,()\"'\\\\@+\\-\\*/%]+)\\s*:\\s*$"

methcode["main"] = Block("main", BLOCK_METHOD)
for i in range(len(lines)):
    ln = lines[i]
    if ln.strip() == "" or ln.strip()[0] == '#':
        continue
    prev_tab = tab
    if ln.startswith("\t"):
        tab = len(ln.split('\t')) - 1
    else:
        ind = ""
        for id in range(len(ln)):
            if ln[id] != ' ':
                break
            ind += " "
        tab = len(ind.split('    ')) - 1
    if prev_tab > tab and not retp:
        for id in range(prev_tab - tab):
            if peek(meth).block_type == BLOCK_WHILE:
                methcode[peek(meth).name].append("lj " + peek(meth).name)
            methcode[peek(meth).name].append("ret")
            meth.pop()
    elif retp:
        retp = False
    parse_line(lines[i], i, infile)
for k in methcode:
    il.append(":" + k)
    if len(methcode[k]) > 0 and not methcode[k][len(methcode[k]) - 1].strip().startswith("ret"):
        methcode[k].append("ret")
        il.extend(methcode[k])
