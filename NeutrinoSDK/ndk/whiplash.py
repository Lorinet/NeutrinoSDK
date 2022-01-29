import os
import sys
import dis

methcntr = 0
meth = {}
declGlbl = []
stack = []
pycode = None

def process_code(code, label):
    global methcntr
    global meth
    global pycode
    meth[label] = []
    makefunction = False

    if label == 'main':
        meth[label].append('link whiprtl.lnx')

    def m_ldgl(val):
        meth[label].append('ldgl ' + str(val))

    def m_ldstr(val):
        if isinstance(val, str):
            meth[label].append('ldstr "' + str(val) + '"')
        elif isinstance(val, int):
            if val < 256:
                meth[label].append('ldb ' + str(val))
            else:
                meth[label].append('ldi ' + str(val))

    def m_leap():
        meth[label].append('leap')

    def m_br(index):
        meth[label].append('br ' + str(index))

    def m_stgl(name):
        if str(name) not in declGlbl:
            declGlbl.append(str(name))
        meth[label].append('stgl ' + str(name))
    
    def m_pushl(name):
        meth[label].append('pushl ' + str(name))
    
    def m_spop():
        meth[label].append('spop')
    
    def m_ret():
        meth[label].append('ret')
    
    def m_gc():
        meth[label].append('gc')

    def m_top(index):
        meth[label].append('top ' + str(index))

    def m_ldloc(name):
        stack.append(str(name))
        meth[label].append('ldloc ' + str(name))
        
    def m_stloc(name):
        meth[label].append('stloc ' + str(name))
    
    def m_add():
        meth[label].append('add')
    
    meth[label].append("swscop " + str(methcntr))
    args = code.co_varnames[:code.co_argcount]
    for r in reversed(range(len(args))):
        m_stloc(args[r])
    dasm = dis.Bytecode(code)
    for i in dasm:
        if len(sys.argv) > 3:
            if sys.argv[3] == "-writePyCode":
                pycode.write(str(i) + '\n')
        if str(type(i.argval)) == "<class 'code'>":
            methcntr += 1
            process_code(i.argval, str(methcntr))
        else:
            if i.opname == "LOAD_GLOBAL" and i.argval != None:
                m_ldgl(i.argval)
            elif i.opname == "STORE_GLOBAL" and i.argval != None:
                m_stgl(i.argval)
            elif i.opname == "LOAD_CONST" and i.argval != None:
                m_ldstr(i.argval)
            elif i.opname == "CALL_FUNCTION":
                m_top(i.argval)
                m_leap()
            elif i.opname == "MAKE_FUNCTION":
                makefunction = True
                meth[meth[label][len(meth[label]) - 1].split(' ')[1].replace('"', '')] = meth.pop(str(methcntr))
                del meth[label][len(meth[label]) - 1]
            elif i.opname == "STORE_NAME" and i.argval != None:
                if makefunction:
                    makefunction = False
                else:
                    m_stgl(i.argval)
            elif i.opname == "LOAD_NAME" and i.argval != None:
                m_ldgl(i.argval)
            elif i.opname == "POP_TOP":
                # m_spop() this is used only for returnless functions, so nada.
                pass
            elif i.opname == "RETURN_VALUE":
                m_gc()
                m_ret()
            elif i.opname == "LOAD_FAST":
                m_ldloc(i.argval)
            elif i.opname == "STORE_FAST":
                m_stloc(i.argval)
            elif i.opname == "BINARY_ADD":
                m_add()


def compile_file(file, out):
    global asm
    global pycode
    if len(sys.argv) > 3:
        if sys.argv[3] == "-writePyCode":
            print("writing [pycode.txt]...")
            pycode = open('pycode.txt', 'w')
    src = ""
    with open(file, 'r') as f:
        src = f.read()
    bytecode = compile(src, file, "exec")
    process_code(bytecode, "main")
    asm = []
    asm.append("; " + file)
    asm.append("; Compiled with Whiplash version 0.2")
    for m in meth.keys():
        asm.append(":" + m)
        asm.extend(meth[m])
    for li in range(len(asm)):
        if asm[li].startswith("ldgl"):
            item = asm[li].split(' ')[1]
            if item not in declGlbl:
                if item not in meth.keys():
                    asm[li] = "pushlx " + item
                else:
                    asm[li] = "pushl " + item
    for i in range(len(asm)):
        asm[i] += '\n'
    print("writing [" + out + "]...")
    with open(out, 'w') as f:
        f.writelines(asm)
    if len(sys.argv) > 3:
        if sys.argv[3] == "-writePyCode":
            pycode.close()

print("Whiplash Python Compiler for NeutrinoOS")

if len(sys.argv) >= 3:
    print("compiling [" + sys.argv[1] + "]...")
    compile_file(sys.argv[1], sys.argv[2])
else:
    print("usage: " + sys.argv[0] + " " + "<file.py> <out.ns>")
