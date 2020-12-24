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
importDirectories = "C:\\Neutrino\\ndk\\whiplash"

def parse_line(s, lnu, fil):
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
