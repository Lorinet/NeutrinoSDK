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
        meth[label].append(('link whiprtl.lnx', -1))

    def cbn():
        return blocks[len(blocks) - 1][0]
    
    def make_meth(m):
        global methcntr
        methcntr += 1
        meth[m[0]] = []
        blocks.append(m)
    
    def code_line(string, cl):
        meth[cbn()].append((string, cl))

    def m_ldgl(val, cl):
        code_line('ldgl ' + str(val), cl)

    def m_ldstr(val, cl):
        if isinstance(val, str):
            code_line('ldstr "' + str(val) + '"', cl)
        elif isinstance(val, int):
            if val < 256:
                code_line('ldb ' + str(val), cl)
            else:
                code_line('ldi ' + str(val), cl)

    def m_leap(cl):
        code_line('leap', cl)

    def m_br(lbl, cl):
        code_line('br ' + str(lbl), cl)
    
    def m_jmp(lbl, cl):
        code_line('jmp ' + str(lbl), cl)

    def m_stgl(name, cl):
        if str(name) not in declGlbl:
            declGlbl.append(str(name))
        code_line('stgl ' + glbPfx + str(name), cl)
    
    def m_pushl(name, cl):
        code_line('pushl ' + str(name), cl)
    
    def m_spop(cl):
        code_line('spop', cl)
    
    def m_ret(cl):
        code_line('ret', cl)

    def m_iret(cl):
        code_line('iret', cl)
    
    def m_gc(cl):
        code_line('gc', cl)

    def m_top(index, cl):
        code_line('top ' + str(index), cl)

    def m_ldloc(name, cl):
        stack.append(str(name))
        code_line('ldloc ' + str(name), cl)
        
    def m_stloc(name, cl):
        code_line('stloc ' + str(name), cl)
    
    def m_stfld(index, cl):
        code_line('ldi ' + str(index), cl)
        code_line('stfld', cl)
    
    def m_ldfld(index, cl):
        code_line('ldi ' + str(index), cl)
        code_line('ldfld', cl)
    
    def m_delfld(index, cl):
        code_line('ldi ' + str(index), cl)
        code_line('delfld', cl)
    
    def m_dup(cl):
        code_line('dup', cl)
    
    def m_add(cl):
        code_line('add', cl)
    
    def m_sub(cl):
        code_line('sub', cl)
    
    def m_mul(cl):
        code_line('mul', cl)
    
    def m_div(cl):
        code_line('div', cl)
    
    def m_and(cl):
        code_line('and', cl)
    
    def m_or(cl):
        code_line('or', cl)
    
    def m_xor(cl):
        code_line('xor', cl)
    
    def m_shl(cl):
        code_line('shl', cl)
    
    def m_shr(cl):
        code_line('shr', cl)

    def m_not(cl):
        code_line('not', cl)
    
    def m_pwr(cl):
        code_line('pwr', cl)
    
    def m_mod(cl):
        code_line('mod', cl)

    def m_newobj(cl):
        code_line('newobj', cl)
    
    def m_cmp(cl):
        code_line('cmp', cl)
    
    def m_ldlen(cl):
        code_line('ldlen', cl)
    
    def m_clr(cl):
        code_line('clr', cl)
    
    def m_tostr(cl):
        code_line('tostr', cl)
    
    def m_parseint(cl):
        code_line('parseint', cl)

    def m_cond(operator, cl):
        if operator == "<":
            code_line('iflt', cl)
        elif operator == ">":
            code_line('ifgt', cl)
        elif operator == "==":
            code_line('ifeq', cl)
        elif operator == "!=":
            code_line('ifne', cl)
        elif operator == "<=":
            code_line('ifle', cl)
        elif operator == ">=":
            code_line('ifge', cl)

    def m_cond_inv(operator, cl):
        if operator == "<":
            code_line('ifge', cl)
        elif operator == ">":
            code_line('ifle', cl)
        elif operator == "==":
            code_line('ifne', cl)
        elif operator == "!=":
            code_line('ifeq', cl)
        elif operator == "<=":
            code_line('ifgt', cl)
        elif operator == ">=":
            code_line('iflt', cl)
    
    code_line("swscop " + str(methcntr), -1)
    args = code.co_varnames[:code.co_argcount]
    for r in reversed(range(len(args))):
        if r == 0 and initClass:
            m_newobj(-1)
        m_stloc(args[r], -1)
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
                        m_pushl(str(i.argval.co_name), ix)
                        m_leap(ix)
                    pycode.write("\nstill " + label + ":\n")
            else:
                if (i.opname == "LOAD_NAME" or i.opname == "LOAD_GLOBAL") and i.argval != None:
                    m_ldgl(i.argval, ix)
                elif i.opname == "STORE_GLOBAL" and i.argval != None:
                    m_stgl(i.argval, ix)
                elif (i.opname == "DELETE_NAME" or i.opname == "DELETE_GLOBAL") and i.argval != None:
                    m_ldgl(i.argval, ix)
                    m_clr(ix)
                elif i.opname == "LOAD_CONST" and i.argval != None:
                    m_ldstr(i.argval, ix)
                elif i.opname == "COMPARE_OP" and i.argval != None:
                    m_cmp(ix)
                    m_cond(str(i.argval), ix)
                    methcntr += 1
                    # next instruction will be POP_JUMP_IF_FALSE, this is where we get jump address after if
                    ix += 1
                    nbn = cbn() + "@" + str(methcntr) + "@if"
                    m_br(nbn, ix)
                    make_meth((nbn, dasm[ix].arg / 2 - 1, str(i.argval)))
                elif i.opname == "JUMP_FORWARD" and i.argval != None:
                    cn = blocks[len(blocks) - 1][2]
                    while cn == "else":
                        # it's an else block, it skips parent now
                        m_iret(ix)
                        blocks.pop()
                        cn = blocks[len(blocks) - 1][2]
                        nbn = cbn() + "@" + str(methcntr) + "@else"
                    m_iret(ix)
                    cn = blocks[len(blocks) - 1][2]
                    blocks.pop()
                    nbn = cbn() + "@" + str(methcntr) + "@else"
                    m_cond_inv(cn, ix)
                    m_br(nbn, ix)
                    methcntr += 1
                    make_meth((nbn, ix + (i.arg / 2), "else"))
                elif i.opname == "JUMP_ABSOLUTE" and i.argval != None:
                    if i.argval / 2 < ix:
                        foundindex = False
                        for x in meth[blocks[len(blocks) - 2][0]]:
                            if x[1] == i.argval / 2:
                                foundindex = True
                                break
                        if foundindex:
                            cl = i.argval / 2
                            bn = blocks[len(blocks) - 2][0]
                            methcntr += 1
                            nbn = bn + '@' + str(methcntr) + '@while_cond'
                            si = 0
                            for jz in range(len(meth[bn])):
                                if meth[bn][jz][1] == cl:
                                    si = jz
                                    #blocks.insert(len(blocks) - 1, (nbn, 0, "while_cond"))
                                    meth[nbn] = []
                                    break
                            for hua in range(len(meth[bn]) - si):
                                meth[nbn].append((meth[bn][si][0], meth[bn][si][1]))
                                del meth[bn][si]
                            laspl = meth[nbn][len(meth[nbn]) - 1][0].split(' ')
                            meth[nbn][len(meth[nbn]) - 1] = ("jmp " + laspl[1], meth[nbn][len(meth[nbn]) - 1][1])
                            meth[nbn].append(("iret", -1))
                            meth[bn].append(("br " + nbn, -1))
                            m_jmp(nbn, ix)
                        else:
                            m_iret(ix)
                            cn = blocks[len(blocks) - 1][2]
                            blocks.pop()
                            m_cond_inv(cn, ix)
                            nbn = cbn() + "@" + str(methcntr) + "@else"
                            m_br(nbn, ix)
                            methcntr += 1
                            make_meth((nbn, blocks[len(blocks) - 1][1] - 1, "else"))
                    else:
                        m_iret(ix)
                        cn = blocks[len(blocks) - 1][2]
                        blocks.pop()
                        m_cond_inv(cn, ix)
                        nbn = cbn() + "@" + str(methcntr) + "@else"
                        m_br(nbn, ix)
                        methcntr += 1
                        make_meth((nbn, i.arg / 2, "else"))
                elif i.opname == "CALL_FUNCTION" or i.opname == "CALL_METHOD":
                    if buildClassNextPass:
                        meth[cbn()].pop()
                        buildClassNextPass = False
                    else:
                        clbm = meth[cbn()][len(meth[cbn()]) - (i.argval + 1)][0].split(' ')
                        if clbm[0] == "ldgl" and clbm[1] in ("str", "int"):
                            if clbm[1] == "str":
                                m_tostr(ix)
                            elif clbm[1] == "int":
                                m_parseint(ix)
                        else:
                            if i.argval > 0:
                                m_top(i.argval, ix)
                            m_leap(ix)
                elif i.opname == "MAKE_FUNCTION":
                    makefunction = True
                    mk = meth[cbn()][len(meth[cbn()]) - 1][0].split(' ')[1].replace('"', '')
                    on = methRecStack[len(methRecStack) - 1]
                    methRecStack.pop()
                    if on != mk:
                        meth[mk] = meth.pop(on)
                    del meth[cbn()][len(meth[cbn()]) - 1]
                    for cl in range(len(meth[mk])):
                        if meth[mk][cl][0].startswith("stgl"):
                            meth[mk][cl][0] = meth[mk][cl][0].replace("%" + on + "%", mk + ".")
                elif i.opname == "STORE_NAME" and i.argval != None:
                    if makefunction:
                        makefunction = False
                        if buildClass:
                            classMeth[buildClassName].append(i.argval)
                    else:
                        m_stgl(i.argval, ix)
                elif i.opname == "POP_TOP":
                    # m_spop() this is used only for returnless functions, so nada.
                    pass
                elif i.opname == "RETURN_VALUE":
                    if initClass:
                        for cm in classMeth[buildClassName]:
                            if not cm in attrIdx:
                                attrIdx[cm] = attrcntr
                                attrcntr += 1
                            m_pushl(buildClassName + "." + cm, ix)
                            m_ldloc("self", ix)
                            m_stfld(attrIdx[cm], ix)
                        m_ldloc("self", ix)
                    m_gc(ix)
                    m_ret(ix)
                elif i.opname == "LOAD_FAST":
                    m_ldloc(i.argval, ix)
                elif i.opname == "STORE_FAST":
                    m_stloc(i.argval, ix)
                elif i.opname == "DELETE_FAST":
                    m_ldloc(i.argval, ix)
                    m_clr(ix)
                elif i.opname == "STORE_ATTR":
                    if not str(i.argval) in attrIdx:
                        attrIdx[i.argval] = attrcntr
                        attrcntr += 1
                    m_stfld(attrIdx[i.argval], ix)
                elif i.opname == "LOAD_ATTR" or i.opname == "LOAD_METHOD":
                    vaspl = meth[cbn()][len(meth[cbn()]) - 1][0].split(' ')
                    if vaspl[0] == "ldgl" and vaspl[1] in classes:
                        vano = vaspl[1] + '.' + i.argval
                        if vano not in declGlbl:
                            declGlbl.append(vano)
                        meth[cbn()][len(meth[cbn()]) - 1][0] = "ldgl " + vano
                    else:
                        if i.opname == "LOAD_METHOD":
                            m_dup(ix);
                        if not str(i.argval) in attrIdx:
                            attrIdx[i.argval] = attrcntr
                            attrcntr += 1
                        m_ldfld(attrIdx[i.argval], ix)
                elif i.opname == "DELETE_ATTR":
                    if not str(i.argval) in attrIdx:
                        attrIdx[i.argval] = attrcntr
                        attrcntr += 1
                    m_delfld(attrIdx[i.argval], ix)
                elif i.opname == "LOAD_BUILD_CLASS":
                    buildClassNextPass = True
                elif i.opname == "INPLACE_ADD" or i.opname == "BINARY_ADD":
                    m_add(ix)
                elif i.opname == "INPLACE_SUBTRACT" or i.opname == "BINARY_SUBTRACT":
                    m_sub(ix)
                elif i.opname == "INPLACE_MULTIPLY" or i.opname == "BINARY_MULTIPLY":
                    m_mul(ix)
                elif i.opname == "INPLACE_TRUE_DIVIDE" or i.opname == "BINARY_TRUE_DIVIDE" or i.opname == "INPLACE_FLOOR_DIVIDE" or i.opname == "BINARY_FLOOR_DIVIDE":
                    m_div(ix)
                elif i.opname == "INPLACE_AND" or i.opname == "BINARY_AND":
                    m_and(ix)
                elif i.opname == "INPLACE_OR" or i.opname == "BINARY_OR":
                    m_or(ix)
                elif i.opname == "INPLACE_XOR" or i.opname == "BINARY_XOR":
                    m_xor(ix)
                elif i.opname == "INPLACE_LSHIFT" or i.opname == "BINARY_LSHIFT":
                    m_shl(ix)
                elif i.opname == "INPLACE_RSHIFT" or i.opname == "BINARY_RSHIFT":
                    m_shr(ix)
                elif i.opname == "UNARY_NOT" or i.opname == "UNARY_INVERT":
                    m_not(ix)
                elif i.opname == "INPLACE_POWER" or i.opname == "BINARY_POWER":
                    m_pwr(ix)
                elif i.opname == "INPLACE_MODULO" or i.opname == "BINARY_MODULO":
                    m_mod(ix)
                elif i.opname == "UNARY_POSITIVE":
                    pass
                elif i.opname == "UNARY_NEGATIVE":
                    m_ldstr(-1)
                    m_mul(ix)
                elif i.opname == "ROT_TWO":
                    m_top(1, ix)
                elif i.opname == "ROT_THREE":
                    m_top(2, ix)
                    m_top(2, ix)
                elif i.opname == "ROT_FOUR":
                    m_top(3, ix)
                    m_top(3, ix)
                    m_top(3, ix)
                elif i.opname == "ROT_N":
                    for x in range(0, i.arg - 1):
                        m_top(i.arg - 1, ix)
                elif i.opname == "DUP_TOP":
                    m_dup(ix)
                elif i.opname == "DUP_TOP_TWO":
                    m_top(1, ix)
                    m_dup(ix)
                    m_top(2, ix)
                    m_dup(ix)
                    m_top(2, ix)
                    m_top(2, ix)
                elif i.opname == "GET_LEN":
                    m_ldlen(ix)
        else:
            if i == "init_label":
                methcntr += 1
                process_code(classInitCodeObject, str(methcntr), initClass=True, buildClassName=buildClassName)
        ix += 1
        while ix > blocks[len(blocks) - 1][1] and blocks[len(blocks) - 1][1] > -1:
            m_iret(ix)
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
        for ln in meth[m]:
            asm.append(ln[0])
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
