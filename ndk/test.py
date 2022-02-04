class Lad:
    def __init__(self, y):
        self.y = y
class Dude:
    def __init__(self, x):
        self.x = Lad(x)
    def haveFun(self):
        if self.x.y == 2:
            print("Smart")
            y = 4
            if y > 5:
                print("Fuck everything")
            elif y == 4:
                if 2 == 3:
                    print("Omfg")
                elif 4 == 5:
                    print("Gfom")
                else:
                    print("Fuck u")
                print("I am smaaart")
            else:
                print("Do not fuck everything!")
        else:
            print("Dumb as a rock")
d = Dude(2)
d.haveFun()