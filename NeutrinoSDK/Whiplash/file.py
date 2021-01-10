class Person:
	num_of_nipples = 2
	species = "Homo sapiens"
	def __init__(self, name, age, kill_mtd):
		self.name = name
		self.age = int(age)
		self.alive = True
		self.suicide = kill_mtd

def hang_me(self):
	nm = self.name
	msg = " is going to hang himself today."
	!('concat hang_me!msg hang_me!nm')
	print(nm)
	self.alive = False
	
p = Person("Lorinet", "16", hang_me)
print(p.name)
print(str(p.age));
print(str(Person.num_of_nipples))
print(str(4 - 5))
Person.num_of_nipples -= 1
print(str(Person.num_of_nipples))
print(Person.species)
p.suicide()
print(str(p.alive))