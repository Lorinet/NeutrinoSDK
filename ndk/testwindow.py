# testwindow View Layout
# Generated using Neutrino UI Designer
# import testwindow
def testwindow_create_view():
	text = "ID:0;Type:WindowInfo;Position X:-1;Position Y:-1;Width:128;Height:64;Title:Window;TitleBar:1;MaximizeButton:0;Hidden:0;Maximized:0;StickyDraw:0;WakeOnInteraction:0;|ID:1;Type:Label;Position X:5;Position Y:5;Width:0;Height:0;Text:Hey sweetie;Font:Helvetica 14;Border:0;|"
	id = WMCreateWindow(text)
	WMSetActiveWindow(id)
	WMUpdateView()
	return id
