import os
import sys
import dis

methcntr = 0
meth = {}
attrcntr = 0
attrIdx = {}
declGlbl = []
stack = []
classes = []
classMeth = {}
methRecStack = []
pycode = None

def process_code(code, label, buildClass=False, initClass=False, buildClassName=""):
    global methcntr
    global meth
    global pycode
    global attrcntr
    global attrIdx
    global classes
    meth[label] = []
    methRecStack.append(label)
    makefunction = False
    buildClassNextPass = False
    glbPfx = ""

    classInitCodeObject = None

    if buildClass:
        glbPfx = "%" + label + "%"

    if len(sys.argv) > 3:
        if sys.argv[3] == "-writePyCode":
            pycode.write("\n" + label + ":\n")

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
        meth[label].append('stgl ' + glbPfx + str(name))
    
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
    
    def m_stfld(index):
        meth[label].append('ldi ' + str(index))
        meth[label].append('stfld')
    
    def m_ldfld(index):
        meth[label].append('ldi ' + str(index))
        meth[label].append('ldfld')
    
    def m_dup():
        meth[label].append('dup')
    
    def m_add():
        meth[label].append('add')

    def m_newobj():
        meth[label].append('newobj')
    
    meth[label].append("swscop " + str(methcntr))
    args = code.co_varnames[:code.co_argcount]
    for r in reversed(range(len(args))):
        if r == 0 and initClass:
            m_newobj()
        m_stloc(args[r])
    dasmr = dis.Bytecode(code)
    dasm = []
    for instr in dasmr:
        dasm.append(instr)
    ix = 0
    if buildClass:
        ix = 4
    
    while ix < len(dasm):
        i = dasm[ix]
        if str(type(i)) == "<class 'dis.Instruction'>":
            if len(sys.argv) > 3:
                if sys.argv[3] == "-writePyCode":
                    #pycode.write(str(i) + '\n')
                    pycode.write(str(i.opname) + " " + str(i.arg) + " " + str(i.argval) + "\n")
            if str(type(i.argval)) == "<class 'code'>":
                if str(i.argval.co_name) == "__init__":
                    classInitCodeObject = i.argval
                    initnm = dasm.pop(ix + 1)
                    initmkfun = dasm.pop(ix + 1)
                    initstnm = dasm.pop(ix + 1)
                    dasm.insert(len(dasm) - 2, "init_label")
                    dasm.insert(len(dasm) - 2, initnm)
                    dasm.insert(len(dasm) - 2, initmkfun)
                    dasm.insert(len(dasm) - 2, initstnm)
                    dasm.pop(ix)
                    continue
                else:
                    methcntr += 1
                    bcn = ""
                    if buildClassNextPass:
                        # this is a dirty little hack to get class name from code object. subject to change.
                        bcn = str(i.argval.co_name)
                        classes.append(str(i.argval.co_name))
                        classMeth[str(i.argval.co_name)] = []
                    process_code(i.argval, str(methcntr), buildClass=buildClassNextPass, buildClassName=bcn)
                    if buildClassNextPass:
                        # this is a dirty little hack to get class name from code object. subject to change.
                        m_pushl(str(i.argval.co_name))
                        m_leap()
                    pycode.write("\nstill " + label + ":\n")
            else:
                if i.opname == "LOAD_GLOBAL" and i.argval != None:
                    m_ldgl(i.argval)
                elif i.opname == "STORE_GLOBAL" and i.argval != None:
                    m_stgl(i.argval)
                elif i.opname == "LOAD_CONST" and i.argval != None:
                    m_ldstr(i.argval)
                elif i.opname == "CALL_FUNCTION" or i.opname == "CALL_METHOD":
                    if buildClassNextPass:
                        meth[label].pop()
                        buildClassNextPass = False
                    else:
                        if i.argval > 0:
                            m_top(i.argval)
                        m_leap()
                elif i.opname == "MAKE_FUNCTION":
                    makefunction = True
                    mk = meth[label][len(meth[label]) - 1].split(' ')[1].replace('"', '')
                    on = methRecStack[len(methRecStack) - 1]
                    meth[mk] = meth.pop(on)
                    methRecStack.pop()
                    del meth[label][len(meth[label]) - 1]
                    for cl in range(len(meth[mk])):
                        if meth[mk][cl].startswith("stgl"):
                            meth[mk][cl] = meth[mk][cl].replace("%" + on + "%", mk + ".")
                elif i.opname == "STORE_NAME" and i.argval != None:
                    if makefunction:
                        makefunction = False
                        if buildClass:
                            classMeth[buildClassName].append(i.argval)
                    else:
                        m_stgl(i.argval)
                elif i.opname == "LOAD_NAME" and i.argval != None:
                    m_ldgl(i.argval)
                elif i.opname == "POP_TOP":
                    # m_spop() this is used only for returnless functions, so nada.
                    pass
                elif i.opname == "RETURN_VALUE":
                    if initClass:
                        for cm in classMeth[buildClassName]:
                            if not cm in attrIdx:
                                attrIdx[cm] = attrcntr
                                attrcntr += 1
                            m_pushl(buildClassName + "." + cm)
                            m_ldloc("self")
                            m_stfld(attrIdx[cm])
                        m_ldloc("self")
                    m_gc()
                    m_ret()
                elif i.opname == "LOAD_FAST":
                    m_ldloc(i.argval)
                elif i.opname == "STORE_FAST":
                    m_stloc(i.argval)
                elif i.opname == "STORE_ATTR":
                    if not str(i.argval) in attrIdx:
                        attrIdx[i.argval] = attrcntr
                        attrcntr += 1
                    m_stfld(attrIdx[i.argval])
                elif i.opname == "LOAD_ATTR" or i.opname == "LOAD_METHOD":
                    vaspl = meth[label][len(meth[label]) - 1].split(' ')
                    if vaspl[0] == "ldgl" and vaspl[1] in classes:
                        vano = vaspl[1] + '.' + i.argval
                        if vano not in declGlbl:
                            declGlbl.append(vano)
                        meth[label][len(meth[label]) - 1] = "ldgl " + vano
                    else:
                        if i.opname == "LOAD_METHOD":
                            m_dup();
                        if not str(i.argval) in attrIdx:
                            attrIdx[i.argval] = attrcntr
                            attrcntr += 1
                        m_ldfld(attrIdx[i.argval])
                elif i.opname == "LOAD_BUILD_CLASS":
                    buildClassNextPass = True
                elif i.opname == "BINARY_ADD":
                    m_add()
        else:
            if i == "init_label":
                methcntr += 1
                process_code(classInitCodeObject, str(methcntr), initClass=True, buildClassName=buildClassName)
        ix += 1

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
    asm.append("")
    for m in meth.keys():
        asm.append(":" + m)
        asm.extend(meth[m])
        asm.append("")
    for li in range(len(asm)):
        if asm[li].startswith("ldgl"):
            item = asm[li].split(' ')[1]
            if item not in declGlbl:
                if item not in meth.keys():
                    asm[li] = "pushlx " + item
                else:
                    if item in classes:
                        asm[li] = "pushl " + item + ".__init__"
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
