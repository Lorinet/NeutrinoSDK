import os
import sys
import dis

methcntr = 0
meth = {}
stack = []
pycode = None

def process_code(code, label):
    global methcntr
    global meth
    global stack
    global pycode
    meth[label] = []
    def m_push(val):
        stack.append(str(val))
        meth[label].append('push ' + str(val))

    def m_spush(val):
        stack.append(str(val))
        if isinstance(val, str):
            meth[label].append('spush "' + str(val) + '"')
        else:
            meth[label].append('spush ' + str(val))
        
    def m_leap(val):
        meth[label].append('leap ' + str(val))

    def m_jsp(index):
        meth[label].append('jsp ' + str(index))

    def m_pop(name):
        stack.pop()
        meth[label].append('pop ' + str(name))
    
    def m_pushl(name):
        stack.append(str(name))
        meth[label].append('pushl ' + str(name))
    
    def m_spop():
        stack.pop()
        meth[label].append('spop')
    
    def m_ret():
        meth[label].append('ret')
    
    def m_ldloc(name):
        stack.append(str(name))
        meth[label].append('ldloc ' + str(name))
        
    def m_stloc(name):
        try:
            stack.pop()
        except:
            pass
        meth[label].append('stloc ' + str(name))
    
    def rm_last_line():
        del meth[label][len(meth[label]) - 1]
    
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
                m_push(i.argval)
            elif i.opname == "LOAD_CONST" and i.argval != None:
                m_spush(i.argval)
            elif i.opname == "CALL_FUNCTION":
                m_jsp(i.argval)
            elif i.opname == "MAKE_FUNCTION":
                rm_last_line() # there was the label name pushed on the stack, we won't need that anymore
                mn = stack.pop()
                meth[mn] = meth[str(methcntr)]
                del meth[str(methcntr)]
                m_pushl(mn)
            elif i.opname == "STORE_NAME" and i.argval != None:
                m_pop(i.argval)
            elif i.opname == "LOAD_NAME" and i.argval != None:
                m_push(i.argval)
            elif i.opname == "POP_TOP":
                # m_spop() this is used only for returnless functions, so nada.
                pass
            elif i.opname == "RETURN_VALUE":
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
    for m in meth.keys():
        asm.append(":" + m)
        asm.extend(meth[m])
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