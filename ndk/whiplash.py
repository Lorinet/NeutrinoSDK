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
    # (name, end, operator): end: -1 for everything not handled by jump_forward; condition: if operator in case of comparation.
    blocks = [(label, -1, "")]

    classInitCodeObject = None

    if buildClass:
        glbPfx = "%" + label + "%"

    if len(sys.argv) > 3:
        if sys.argv[3] == "-writePyCode":
            pycode.write("\n" + label + ":\n")

    if label == 'main':
        meth[label].append('link whiprtl.lnx')

    def cbn():
        return blocks[len(blocks) - 1][0]
    
    def make_meth(m):
        global methcntr
        methcntr += 1
        meth[m[0]] = []
        blocks.append(m)

    def m_ldgl(val):
        meth[cbn()].append('ldgl ' + str(val))

    def m_ldstr(val):
        if isinstance(val, str):
            meth[cbn()].append('ldstr "' + str(val) + '"')
        elif isinstance(val, int):
            if val < 256:
                meth[cbn()].append('ldb ' + str(val))
            else:
                meth[cbn()].append('ldi ' + str(val))

    def m_leap():
        meth[cbn()].append('leap')

    def m_brp(lbl):
        meth[cbn()].append('brp ' + str(lbl))

    def m_stgl(name):
        if str(name) not in declGlbl:
            declGlbl.append(str(name))
        meth[cbn()].append('stgl ' + glbPfx + str(name))
    
    def m_pushl(name):
        meth[cbn()].append('pushl ' + str(name))
    
    def m_spop():
        meth[cbn()].append('spop')
    
    def m_ret():
        meth[cbn()].append('ret')

    def m_iret():
        meth[cbn()].append('iret')
    
    def m_gc():
        meth[cbn()].append('gc')

    def m_top(index):
        meth[cbn()].append('top ' + str(index))

    def m_ldloc(name):
        stack.append(str(name))
        meth[cbn()].append('ldloc ' + str(name))
        
    def m_stloc(name):
        meth[cbn()].append('stloc ' + str(name))
    
    def m_stfld(index):
        meth[cbn()].append('ldi ' + str(index))
        meth[cbn()].append('stfld')
    
    def m_ldfld(index):
        meth[cbn()].append('ldi ' + str(index))
        meth[cbn()].append('ldfld')
    
    def m_delfld(index):
        meth[cbn()].append('ldi ' + str(index))
        meth[cbn()].append('delfld')
    
    def m_dup():
        meth[cbn()].append('dup')
    
    def m_add():
        meth[cbn()].append('add')
    
    def m_sub():
        meth[cbn()].append('sub')
    
    def m_mul():
        meth[cbn()].append('mul')
    
    def m_div():
        meth[cbn()].append('div')
    
    def m_and():
        meth[cbn()].append('and')
    
    def m_or():
        meth[cbn()].append('or')
    
    def m_xor():
        meth[cbn()].append('xor')
    
    def m_shl():
        meth[cbn()].append('shl')
    
    def m_shr():
        meth[cbn()].append('shr')

    def m_not():
        meth[cbn()].append('not')
    
    def m_pwr():
        meth[cbn()].append('pwr')
    
    def m_mod():
        meth[cbn()].append('mod')

    def m_newobj():
        meth[cbn()].append('newobj')
    
    def m_cmp():
        meth[cbn()].append('cmp')
    
    def m_ldlen():
        meth[cbn()].append('ldlen')
    
    def m_clr():
        meth[cbn()].append('clr')

    def m_cond(operator):
        if operator == "<":
            meth[cbn()].append('iflt')
        elif operator == ">":
            meth[cbn()].append('ifgt')
        elif operator == "==":
            meth[cbn()].append('ifeq')
        elif operator == "!=":
            meth[cbn()].append('ifne')
        elif operator == "<=":
            meth[cbn()].append('ifle')
        elif operator == ">=":
            meth[cbn()].append('ifge')

    def m_cond_inv(operator):
        if operator == "<":
            meth[cbn()].append('ifge')
        elif operator == ">":
            meth[cbn()].append('ifle')
        elif operator == "==":
            meth[cbn()].append('ifne')
        elif operator == "!=":
            meth[cbn()].append('ifeq')
        elif operator == "<=":
            meth[cbn()].append('ifgt')
        elif operator == ">=":
            meth[cbn()].append('iflt')
    
    meth[cbn()].append("swscop " + str(methcntr))
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
                    process_code(i.argval, str(i.argval.co_name), buildClass=buildClassNextPass, buildClassName=bcn)
                    if buildClassNextPass:
                        # this is a dirty little hack to get class name from code object. subject to change.
                        m_pushl(str(i.argval.co_name))
                        m_leap()
                    pycode.write("\nstill " + label + ":\n")
            else:
                if (i.opname == "LOAD_NAME" or i.opname == "LOAD_GLOBAL") and i.argval != None:
                    m_ldgl(i.argval)
                elif i.opname == "STORE_GLOBAL" and i.argval != None:
                    m_stgl(i.argval)
                elif (i.opname == "DELETE_NAME" or i.opname == "DELETE_GLOBAL") and i.argval != None:
                    m_ldgl(i.argval)
                    m_clr()
                elif i.opname == "LOAD_CONST" and i.argval != None:
                    m_ldstr(i.argval)
                elif i.opname == "COMPARE_OP" and i.argval != None:
                    m_cmp()
                    m_cond(str(i.argval))
                    methcntr += 1
                    # next instruction will be POP_JUMP_IF_FALSE, this is where we get jump address after if
                    ix += 1
                    nbn = cbn() + "@" + str(methcntr) + "@if"
                    m_brp(nbn)
                    make_meth((nbn, dasm[ix].arg / 2, str(i.argval)))
                elif i.opname == "JUMP_FORWARD" and i.argval != None:
                    cn = blocks[len(blocks) - 1][2]
                    while cn == "else":
                        # it's an else block, it skips parent now
                        m_iret()
                        blocks.pop()
                        cn = blocks[len(blocks) - 1][2]
                        nbn = cbn() + "@" + str(methcntr) + "@else"
                    m_iret()
                    cn = blocks[len(blocks) - 1][2]
                    blocks.pop()
                    nbn = cbn() + "@" + str(methcntr) + "@else"
                    m_cond_inv(cn)
                    m_brp(nbn)
                    methcntr += 1
                    make_meth((nbn, ix + (i.arg / 2), "else"))
                elif i.opname == "JUMP_ABSOLUTE" and i.argval != None:
                    if i.argval / 2 < ix:
                        pass
                    else:
                        m_iret()
                        cn = blocks[len(blocks) - 1][2]
                        blocks.pop()
                        m_cond_inv(cn)
                        nbn = cbn() + "@" + str(methcntr) + "@else"
                        m_brp(nbn)
                        methcntr += 1
                        make_meth((nbn, i.arg / 2, "else"))
                elif i.opname == "CALL_FUNCTION" or i.opname == "CALL_METHOD":
                    if buildClassNextPass:
                        meth[cbn()].pop()
                        buildClassNextPass = False
                    else:
                        if i.argval > 0:
                            m_top(i.argval)
                        m_leap()
                elif i.opname == "MAKE_FUNCTION":
                    makefunction = True
                    mk = meth[cbn()][len(meth[cbn()]) - 1].split(' ')[1].replace('"', '')
                    on = methRecStack[len(methRecStack) - 1]
                    methRecStack.pop()
                    if on != mk:
                        meth[mk] = meth.pop(on)
                    del meth[cbn()][len(meth[cbn()]) - 1]
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
                elif i.opname == "DELETE_FAST":
                    m_ldloc(i.argval)
                    m_clr()
                elif i.opname == "STORE_ATTR":
                    if not str(i.argval) in attrIdx:
                        attrIdx[i.argval] = attrcntr
                        attrcntr += 1
                    m_stfld(attrIdx[i.argval])
                elif i.opname == "LOAD_ATTR" or i.opname == "LOAD_METHOD":
                    vaspl = meth[cbn()][len(meth[cbn()]) - 1].split(' ')
                    if vaspl[0] == "ldgl" and vaspl[1] in classes:
                        vano = vaspl[1] + '.' + i.argval
                        if vano not in declGlbl:
                            declGlbl.append(vano)
                        meth[cbn()][len(meth[cbn()]) - 1] = "ldgl " + vano
                    else:
                        if i.opname == "LOAD_METHOD":
                            m_dup();
                        if not str(i.argval) in attrIdx:
                            attrIdx[i.argval] = attrcntr
                            attrcntr += 1
                        m_ldfld(attrIdx[i.argval])
                elif i.opname == "DELETE_ATTR":
                    if not str(i.argval) in attrIdx:
                        attrIdx[i.argval] = attrcntr
                        attrcntr += 1
                    m_delfld(attrIdx[i.argval])
                elif i.opname == "LOAD_BUILD_CLASS":
                    buildClassNextPass = True
                elif i.opname == "INPLACE_ADD" or i.opname == "BINARY_ADD":
                    m_add()
                elif i.opname == "INPLACE_SUBTRACT" or i.opname == "BINARY_SUBTRACT":
                    m_sub()
                elif i.opname == "INPLACE_MULTIPLY" or i.opname == "BINARY_MULTIPLY":
                    m_mul()
                elif i.opname == "INPLACE_TRUE_DIVIDE" or i.opname == "BINARY_TRUE_DIVIDE" or i.opname == "INPLACE_FLOOR_DIVIDE" or i.opname == "BINARY_FLOOR_DIVIDE":
                    m_div()
                elif i.opname == "INPLACE_AND" or i.opname == "BINARY_AND":
                    m_and()
                elif i.opname == "INPLACE_OR" or i.opname == "BINARY_OR":
                    m_or()
                elif i.opname == "INPLACE_XOR" or i.opname == "BINARY_XOR":
                    m_xor()
                elif i.opname == "INPLACE_LSHIFT" or i.opname == "BINARY_LSHIFT":
                    m_shl()
                elif i.opname == "INPLACE_RSHIFT" or i.opname == "BINARY_RSHIFT":
                    m_shr()
                elif i.opname == "UNARY_NOT" or i.opname == "UNARY_INVERT":
                    m_not()
                elif i.opname == "INPLACE_POWER" or i.opname == "BINARY_POWER":
                    m_pwr()
                elif i.opname == "INPLACE_MODULO" or i.opname == "BINARY_MODULO":
                    m_mod()
                elif i.opname == "UNARY_POSITIVE":
                    pass
                elif i.opname == "UNARY_NEGATIVE":
                    m_ldstr(-1)
                    m_mul()
                elif i.opname == "ROT_TWO":
                    m_top(1)
                elif i.opname == "ROT_THREE":
                    m_top(2)
                    m_top(2)
                elif i.opname == "ROT_FOUR":
                    m_top(3)
                    m_top(3)
                    m_top(3)
                elif i.opname == "ROT_N":
                    for x in range(0, i.arg - 1):
                        m_top(i.arg - 1)
                elif i.opname == "DUP_TOP":
                    m_dup()
                elif i.opname == "DUP_TOP_TWO":
                    m_top(1)
                    m_dup()
                    m_top(2)
                    m_dup()
                    m_top(2)
                    m_top(2)
                elif i.opname == "GET_LEN":
                    m_ldlen()
        else:
            if i == "init_label":
                methcntr += 1
                process_code(classInitCodeObject, str(methcntr), initClass=True, buildClassName=buildClassName)
        ix += 1
        if blocks[len(blocks) - 1][1] > -1:
            if ix > blocks[len(blocks) - 1][1]:
                m_iret()
                blocks.pop()

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
