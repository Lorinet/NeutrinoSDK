# This Python file uses the following encoding: utf-8
import sys
import os
import webbrowser
import re


from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
from PyQt5 import uic

class Element:
    def __init__(self):
        self.properties = {}

    @staticmethod
    def with_id(id):
        el = Element()
        el.properties = { "ID": id }
        return el
    
    @staticmethod
    def with_id_type(id, type):
        el = Element()
        el.properties = { "ID": id, "Type": type }
        if type in ["Label", "Button", "CheckBox", "TextBox", "TextField", "TextBuffer", "ListView"]:
            el.properties["Position X"] = "0"
            el.properties["Position Y"] = "0"
            el.properties["Width"] = "0"
            el.properties["Height"] = "0"
        if type in ["Label", "Button", "CheckBox", "TextBox", "TextField", "TextBuffer"]:
            el.properties["Text"] = ""
            el.properties["Font"] = "Helvetica 8"
        if type in ["Button", "CheckBox"]:
            el.properties["Selectable"] = "1"
        if type in ["Label", "TextField", "TextBuffer"]:
            el.properties["Border"] = "0"
        if type in ["CheckBox"]:
            el.properties["Checked"] = "0"
        if type == "WindowInfo":
            el.properties["Position X"] = "-1"
            el.properties["Position Y"] = "-1"
            el.properties["Width"] = "128"
            el.properties["Height"] = "64"
            el.properties["Title"] = "Window"
            el.properties["TitleBar"] = "1"
            el.properties["MaximizeButton"] = "1"
            el.properties["Hidden"] = "0"
            el.properties["Maximized"] = "0"
            el.properties["StickyDraw"] = "0"
            el.properties["WakeOnInteraction"] = "0"
        if type == "ListView":
            el.properties["Items"] = ""
        return el
        
    def serialize(self):
        ser = ""
        for key in self.properties:
            ser += key + ":" + self.properties[key].replace("\\", "\\\\").replace("\n", "\\n").replace(":", "\\:").replace("|", "\\|") + ";"
        return ser
        
    @staticmethod
    def deserialize(code: str):
        el = Element()
        prop = ""
        val = ""
        pastProp = False
        escape = False
        for i in range(len(code)):
            if code[i] == '\\':
                if escape:
                    escape = False
                else:
                    escape = True
                    continue
            if pastProp:
                if code[i] == ';':
                    if escape:
                        escape = False
                    else:
                        pastProp = False
                        el.properties[prop] = val
                        prop = ""
                        val = ""
                        continue
                if escape:
                    escape = False
                    if code[i] == 'n':
                        val += "\n"
                val += code[i]
            else:
                if code[i] == ':':
                    if escape:
                        escape = False
                    else:
                        pastProp = True
                        continue
                if escape:
                    escape = False
                prop += code[i]
        return el
    
    def getProperty(self, prop):
        if prop in self.properties:
            return self.properties[prop]
        return ""
    
    def getPropertyInt(self, prop):
        if prop in self.properties:
            return int(self.properties[prop])
        return -1
    
    def setProperty(self, prop, val):
        self.properties[prop] = val

class DrawCanvas(QWidget):
    def __init__(self):
        super(DrawCanvas, self).__init__()
    
    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setBrush(Qt.SolidPattern)
        pen = QPen()
        pen.setColor(Qt.green)
        pen.setWidth(10)
        painter.setPen(pen)
        painter.drawRect(self.rect())


class Designer(QMainWindow):
    def __init__(self):
        super(Designer, self).__init__()
        uic.loadUi("form.ui", self)
        self.actionNew = self.findChild(QAction, "actionNew")
        self.actionOpen = self.findChild(QAction, "actionOpen")
        self.actionSave = self.findChild(QAction, "actionSave")
        self.actionSave_As = self.findChild(QAction, "actionSave_As")
        self.actionExit = self.findChild(QAction, "actionExit")
        self.actionUndo = self.findChild(QAction, "actionUndo")
        self.actionRedo = self.findChild(QAction, "actionRedo")
        self.actionZoom = self.findChild(QAction, "actionZoom")
        self.actionZoom.setCheckable(True)
        self.actionZoom.setChecked(False)
        self.actionElementBoundaries = self.findChild(QAction, "actionElement_Boundaries")
        self.actionElementBoundaries.setCheckable(True)
        self.actionElementBoundaries.setChecked(True)
        self.actionDocumentation = self.findChild(QAction, "actionDocumentation")
        self.actionAbout = self.findChild(QAction, "actionAbout")
        self.codeEdit = self.findChild(QPlainTextEdit, "codeEdit")
        self.codeEdit.setReadOnly(True)
        self.addNewElement = self.findChild(QToolButton, "addNewElement")
        self.removeSelectedElement = self.findChild(QToolButton, "removeSelectedElement")
        self.addNewProperty = self.findChild(QToolButton, "addNewProperty")
        self.removeSelectedProperty = self.findChild(QToolButton, "removeSelectedProperty")
        self.elementSelector = self.findChild(QComboBox, "elementSelector")
        self.listView = self.findChild(QListWidget, "listView")
        self.propertyTable = self.findChild(QTableWidget, "propertyTable")
        self.propertyTable.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)
        self.propertyTable.setSelectionMode(QAbstractItemView.SingleSelection)
        self.designTab = self.findChild(QWidget, "designTab")
        self.show()

class AddElement(QDialog):
    def __init__(self):
        super(AddElement, self).__init__()
        uic.loadUi("addElement.ui", self)
        self.idBox = self.findChild(QSpinBox, "idBox")
        self.typeBox = self.findChild(QComboBox, "typeBox")
        self.buttonBox = self.findChild(QDialogButtonBox, "buttonBox")
        self.buttonBox.accepted.connect(self.accept)
        self.buttonBox.rejected.connect(self.reject)
        self.show()

class AddProperty(QDialog):
    def __init__(self):
        super(AddProperty, self).__init__()
        uic.loadUi("addProperty.ui", self)
        self.propertyBox = self.findChild(QLineEdit, "propertyBox")
        self.valueBox = self.findChild(QLineEdit, "valueBox")
        self.buttonBox = self.findChild(QDialogButtonBox, "buttonBox")
        self.buttonBox.accepted.connect(self.accept)
        self.buttonBox.rejected.connect(self.reject)
        self.show()

class FontPicker(QDialog):
    def __init__(self):
        super(FontPicker, self).__init__()
        uic.loadUi("fontPicker.ui", self)
        self.fontBox = self.findChild(QComboBox, "fontBox")
        self.buttonBox = self.findChild(QDialogButtonBox, "buttonBox")
        self.buttonBox.accepted.connect(self.accept)
        self.buttonBox.rejected.connect(self.reject)
        self.show()

class TextEdit(QDialog):
    def __init__(self):
        super(TextEdit, self).__init__()
        uic.loadUi("textEdit.ui", self)
        self.editField = self.findChild(QPlainTextEdit, "editField")
        self.editPropertyLabel = self.findChild(QLabel, "editPropertyLabel")
        self.buttonBox = self.findChild(QDialogButtonBox, "buttonBox")
        self.buttonBox.accepted.connect(self.accept)
        self.buttonBox.rejected.connect(self.reject)
        self.show()

class ListEdit(QDialog):
    def __init__(self):
        super(ListEdit, self).__init__()
        uic.loadUi("listEdit.ui", self)
        self.editField = self.findChild(QPlainTextEdit, "editField")
        self.buttonBox = self.findChild(QDialogButtonBox, "buttonBox")
        self.buttonBox.accepted.connect(self.accept)
        self.buttonBox.rejected.connect(self.reject)
        self.show()

class About(QDialog):
    def __init__(self):
        super(About, self).__init__()
        uic.loadUi("about.ui", self)
        self.closeButton = self.findChild(QPushButton, "closeButton")
        self.closeButton.clicked.connect(self.accept)
        self.show()



app = QApplication(sys.argv)

window = Designer()

def about():
    aboutDialog = About()
    aboutDialog.exec_()

def documentation():
    webbrowser.open_new("https://lorinet.github.io/neutrinosdk/documentation")

elements = [
    Element.deserialize("ID:0;Type:WindowInfo;Position X:-1;Position Y:-1;Width:128;Height:64;Title:Window;TitleBar:1;MaximizeButton:1;Hidden:0;Maximized:0;StickyDraw:0;WakeOnInteraction:0;"),
    Element.deserialize("ID:1;Position X:5;Position Y:2;Type:Label;Font:Helvetica 8;Text:Hello world;Checked:1;")
    ]

selection = 0
currentFile = ""
modified = False

def update_preview():
    window.designTab.update()

def load_elements():
    global elements
    viewList = []
    for el in elements:
        viewList.append(el.getProperty("Type") + " " + el.getProperty("ID"))
    window.propertyTable.setRowCount(0)
    window.listView.clear()
    window.listView.addItems(viewList)
    window.elementSelector.clear()
    window.elementSelector.addItems(viewList)

def select_element(index):
    global selection
    global elements
    selection = index
    window.codeEdit.document().setPlainText(elements[selection].serialize())
    window.propertyTable.setRowCount(0)
    for prop in elements[selection].properties:
        ri = window.propertyTable.rowCount()
        window.propertyTable.insertRow(ri)
        window.propertyTable.setItem(ri, 0, QTableWidgetItem(prop))
        window.propertyTable.setItem(ri, 1, QTableWidgetItem(elements[selection].getProperty(prop)))
    update_preview()

def find_vacant_id():
    global elements
    ix = 0
    while True:
        found = True
        for el in elements:
            if el.getProperty("ID") == str(ix):
                found = False
        if found:
            return ix
        ix += 1

def list_select():
    ix = window.listView.currentRow()
    window.elementSelector.setCurrentIndex(ix)
    select_element(ix)

def combo_select(index):
    window.listView.setCurrentRow(index)
    select_element(index)

def font_select():
    global selection
    global elements
    global modified
    fontDialog = FontPicker()
    if fontDialog.exec():
        elements[selection].setProperty("Font", str(fontDialog.fontBox.currentText()))
        modified = True

def add_new_property():
    global selection
    global elements
    global modified
    addProperty = AddProperty()
    if addProperty.exec():
        elements[selection].setProperty(str(addProperty.propertyBox.text()), str(addProperty.valueBox.text()))
        modified = True
        select_element(selection)

def remove_property():
    global selection
    global elements
    global modified
    row = window.propertyTable.currentIndex().row()
    if row > -1:
        del elements[selection].properties[window.propertyTable.item(row, 0).text()]
        modified = True
        select_element(selection)
    else:
        QMessageBox.warning(window, "Notice", "No property selected.", QMessageBox.OK)

def add_new_element():
    global elements
    global modified
    addElement = AddElement()
    addElement.idBox.setValue(find_vacant_id())
    if addElement.exec():
        elements.append(Element.with_id_type(str(addElement.idBox.value()), str(addElement.typeBox.currentText())))
        modified = True
        load_elements()

def remove_element():
    global selection
    global elements
    global modified
    if elements[selection].getProperty("Type") == "WindowInfo":
        QMessageBox.warning(window, "Notice", "WindowInfo elements cannot be removed.", QMessageBox.OK)
    else:
        del elements[selection]
        modified = True
    load_elements()
    
def text_edit(prop):
    global selection
    global elements
    global modified
    textEdit = TextEdit()
    textEdit.editPropertyLabel.text = "Edit " + prop
    textEdit.editField.document().setPlainText(elements[selection].getProperty(prop))
    if textEdit.exec():
        elements[selection].setProperty(prop, str(textEdit.editField.toPlainText()))
        modified = True

def list_edit(prop):
    global selection
    global elements
    global modified
    listEdit = ListEdit()
    listEdit.editField.document().setPlainText(elements[selection].getProperty(prop).replace(',', '\n').replace('\\\n', '\\,'))
    if listEdit.exec():
        elements[selection].setProperty(prop, str(listEdit.editField.toPlainText()).replace('\n', ',') + ',')
        modified = True

def deserialize_view(code):
    global selection
    global elements
    selection = 0
    elements.clear()
    cure = ""
    escape = False
    for i in range(len(code)):
        if code[i] == '\\':
            if escape:
                escape = False
            else:
                escape = True
                cure += '\\'
                continue
        elif code[i] == '|' and not escape:
            elements.append(Element.deserialize(cure))
            cure = ""
        else:
            escape = False
            cure += code[i]
    load_elements()

def serialize_view():
    global elements
    ser = ""
    for el in elements:
        ser += el.serialize() + '|'
    return ser

def save_as():
    global currentFile
    global modified
    currentFile = ""
    modified = True
    save_file()

def save_file():
    global currentFile
    global modified
    if currentFile == "":
        currentFile, fltr = QFileDialog.getSaveFileName(window, "Save File", "", "Python source (*.py);;Neutrino IL source (*.ns);;Neutrino UI Design File (*.udf)")
        selectedExt = re.search('\((.+?)\)', fltr).group(1).replace('*', '')
        if not currentFile.endswith(selectedExt):
            currentFile += selectedExt
    content = ""
    if currentFile.endswith(".udf"):
        content = serialize_view()
    elif currentFile.endswith(".ns"):
        name = currentFile[0:-3]
        content = "; " + name + " View Layout\n\n:" + name + "_CreateView\nspush \"" + serialize_view() + "\"\nleap WMCreateWindow\npop __" + name + "_hwnd ; Do not modify the handle variable!\npush __" + name + "_hwnd\nleap WMSetActiveWindow\nleap WMUpdateView\nret\n\n:" + name + "_DestroyView\npush __" + name + "_hwnd\nleap WMDestroyWindow\nret\n\n; Auto-generated with Neutrino UI Design Tool\n; #include " + name + ".ns\n"
    with open(currentFile, "w") as f:
        f.write(content)
    modified = False

def open_file():
    global modified
    ays = QMessageBox.warning(window, "Are you sure?", "The current document has been modified.\nAre you sure to open another file and discard unsaved changes?", QMessageBox.Save | QMessageBox.Discard | QMessageBox.Cancel)
    if ays == QMessageBox.Save:
        save_file()
    elif ays == QMessageBox.Cancel:
        return
    fileName = QFileDialog.getOpenFileName(window, "Open File", "", "Python source (*.py);;Neutrino IL source (*.ns);;Neutrino UI Design File (*.udf)")[0]
    content = ""
    with open(fileName, "r") as f:
        content = f.readlines()
    if fileName.endswith(".udf"):
        deserialize_view(content[0])
    elif fileName.endswith(".ns"):
        for ln in content:
            if ln.startswith("spush"):
                deserialize_view(ln[7:-1])

def property_edit(row, col):
    global selection
    global elements
    prop = window.propertyTable.item(row, 0).text()
    if prop == "Font":
        font_select()
    elif prop == "Items":
        list_edit(prop)
    else:
        text_edit(prop)
    select_element(selection)

def get_font_size(el):
    return int(re.search("\d+", el.getProperty("Font")).group(0))

def draw_preview(event):
    global window
    global elements
    wi = elements[0]
    painter = QPainter(window.designTab)
    painter.setRenderHint(QPainter.Antialiasing)
    path = QPainterPath()
    previewScale = 1
    if window.actionZoom.isChecked():
        previewScale = 2
    qrf = QRectF(10, 10, wi.getPropertyInt("Width") * previewScale, wi.getPropertyInt("Height") * previewScale)
    path.addRoundedRect(qrf, 3 * previewScale, 3 * previewScale)
    painter.fillPath(path, QColor.fromRgb(8, 28, 12))

    pen = QPen(QColor.fromRgb(135, 255, 159), 1, Qt.SolidLine, Qt.SquareCap, Qt.MiterJoin)
    painter.setRenderHint(QPainter.Antialiasing, False)
    painter.setPen(pen)

    for el in elements:
        if el.getProperty("Type") in ["Button", "Label", "TextBox", "TextField", "TextBuffer", "CheckBox", "ListView"]:
            x = el.getPropertyInt("Position X")
            y = el.getPropertyInt("Position Y")
            w = el.getPropertyInt("Width")
            h = el.getPropertyInt("Height")
            pady = 0
            if el.getProperty("Type") == "Button":
                pady = 3 * previewScale
            fnt = QFont()
            fnt.setPixelSize(get_font_size(el) * previewScale)
            painter.setFont(fnt)
            qfo = QFontMetrics(fnt)
            if w <= 0:
                w = qfo.width(el.getProperty("Text")) / previewScale
            if h <= 0:
                h = qfo.height() / previewScale
            if window.actionElementBoundaries.isChecked() or el.getProperty("Type") == "Button":
                painter.drawRect(QRectF(10 + (x * previewScale), 10 + (y * previewScale), (w * previewScale) + (2 * pady), h * previewScale))
            painter.drawText(QPointF(10 + (x * previewScale) + pady, 10 + (y * previewScale) + (h * previewScale - previewScale)), el.getProperty("Text"))

    painter.end()

window.designTab.paintEvent = draw_preview

window.actionOpen.triggered.connect(open_file)
window.actionSave.triggered.connect(save_file)
window.actionSave_As.triggered.connect(save_as)

window.actionZoom.triggered.connect(update_preview)
window.actionElementBoundaries.triggered.connect(update_preview)

window.actionAbout.triggered.connect(about)
window.actionDocumentation.triggered.connect(documentation)

window.listView.itemSelectionChanged.connect(list_select)
window.elementSelector.currentIndexChanged.connect(combo_select)
window.propertyTable.cellDoubleClicked.connect(property_edit)

window.addNewProperty.clicked.connect(add_new_property)
window.removeSelectedProperty.clicked.connect(remove_property)
window.addNewElement.clicked.connect(add_new_element)
window.removeSelectedElement.clicked.connect(remove_element)

load_elements()

app.exec_()
