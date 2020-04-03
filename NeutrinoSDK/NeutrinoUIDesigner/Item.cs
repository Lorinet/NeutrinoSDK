using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeutrinoUIDesigner
{
    class Item
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public Item(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }
    class Element
    {
        public Dictionary<string, string> Properties { get; set; }
        public Element()
        {
            Properties = new Dictionary<string, string>();
        }
        public Element(Dictionary<string, string> p)
        {
            Properties = p;
        }
        public string GetProperty(string p)
        {
            if (Properties.ContainsKey(p)) return Properties[p];
            else return "";
        }
        public int GetPropertyInt(string p)
        {
            int x;
            if (Properties.ContainsKey(p))
            {
                if (int.TryParse(Properties[p], out x))
                {
                    return x;
                }
            }
            return -1;
        }
        public void SetProperty(string p, string v)
        {
            if (Properties.ContainsKey(p)) Properties[p] = v;
            else Properties.Add(p, v);
        }
        public string Serialize()
        {
            string ser = "";
            foreach(KeyValuePair<string, string> p in Properties)
            {
                ser += p.Key + ":" + p.Value + ";";
            }
            return ser;
        }
        public static string SerializeView(List<Element> e)
        {
            string ser = "";
            foreach(Element l in e)
            {
                ser += l.Serialize() + '|';
            }
            return ser;
        }
        public static Element Deserialize(string s)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            string pname = "", pvalue = "";
            pname += s[0];
            bool addval = false;
            for(int i = 1; i < s.Length; i++)
            {
                if (s[i] == ':' && s[i - 1] != '\\') addval = true;
                else if (s[i] == ';' && s[i - 1] != '\\')
                {
                    addval = false;
                    p.Add(pname, pvalue);
                    pname = "";
                    pvalue = "";
                }
                else if (!addval) pname += s[i];
                else if (addval) pvalue += s[i];
            }
            return new Element(p);
        }
        public static List<Element> DeserializeView(string s)
        {
            List<string> elementser = new List<string>();
            string cur = "";
            cur += s[0];
            for(int i = 1; i < s.Length; i++)
            {
                if (s[i] == '|' && s[i - 1] != '\\')
                {
                    elementser.Add(cur);
                    cur = "";
                }
                else cur += s[i];
            }
            List<Element> e = new List<Element>();
            foreach(string t in elementser)
            {
                e.Add(Deserialize(t));
            }
            return e;
        }
    }
}
