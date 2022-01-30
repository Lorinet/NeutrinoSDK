class nada:
    boobs = 2
    def __init__(self, message):
        self.message = message
    def printMessage(self, man):
        print(self.message)
        print(man)

def alterFun(self, man):
    print("HACKED")

m = nada("aaa")

n = nada(" you")
n.printMessage = alterFun
n.printMessage("dog")

m.printMessage("good")
print(str(nada.boobs))