using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFSTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Neutrino File System Deployment Image Manipulation Tool");
            if (args.Length > 1)
            {
                try
                {
                    NeutrinoFileSystem nfs = new NeutrinoFileSystem(args[1]);
                    if (args[0] == "/c" && args.Length > 2)
                    {
                        Console.WriteLine("Creating file system...");
                        for (int i = 2; i < args.Length; i++)
                        {
                            nfs.CreateFile(args[i], File.ReadAllBytes(args[i]));
                            Console.WriteLine(args[i] + " => " + args[1]);
                        }
                        nfs.Commit();
                        Console.WriteLine("File system image created!");
                    }
                    else if (args[0] == "/lc")
                    {
                        Console.WriteLine(nfs.Files.Items.Count + " file system entries:");
                        foreach (NtrFile f in nfs.Files.Items)
                        {
                            Console.WriteLine(f.Name);
                        }
                    }
                    else if (args[0] == "/a" && args.Length > 2)
                    {
                        nfs.CreateFile(args[2], File.ReadAllBytes(args[2]));
                        Console.WriteLine(args[2] + " => " + args[1]);
                        nfs.Commit();
                        Console.WriteLine("The operation completed successfully.");
                    }
                    else if (args[0] == "/d" && args.Length > 2)
                    {
                        nfs.Delete(args[2]);
                        Console.WriteLine(args[2] + " x");
                        nfs.Commit();
                        Console.WriteLine("The operation completed successfully.");
                    }
                    else if (args[0] == "/u" && args.Length > 2)
                    {
                        nfs.WriteFile(args[2], File.ReadAllBytes(args[2]));
                        Console.WriteLine(args[2] + " => " + args[1]);
                        nfs.Commit();
                        Console.WriteLine("The operation completed successfully.");
                    }
                    else if (args[0] == "/w")
                    {
                        nfs.Files.Clear();
                        nfs.Commit();
                        Console.WriteLine("The operation completed successfully.");
                    }
                    else if (args[0] == "/x" && args.Length > 2)
                    {
                        File.WriteAllBytes(args[2], nfs.ReadFile(args[2]));
                        Console.WriteLine(args[2] + " <= " + args[1]);
                        nfs.Commit();
                        Console.WriteLine("The operation completed successfully.");
                    }
                    if (args[0] == "/xa")
                    {
                        Console.WriteLine("Extracting files...");
                        for (int i = 0; i < nfs.Files.Items.Count; i++)
                        {
                            if (!Directory.Exists(Directory.GetParent(nfs.Files.Items[i].Name).ToString())) Directory.CreateDirectory(Directory.GetParent(nfs.Files.Items[i].Name).ToString());
                            File.WriteAllBytes(nfs.Files.Items[i].Name, nfs.ReadFile(nfs.Files.Items[i].Name));
                            Console.WriteLine(nfs.Files.Items[i].Name + " <= " + args[1]);
                        }
                        Console.WriteLine("Files extracted successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            else Console.WriteLine("Usage: nfsdimp </c|/lc|/a|/d|/u|/w|/x|/xa> <image.nim> [file1 file2 ...]\n/c - create new file system image\n/lc - list contents of file system image\n/a - add file to file system image\n/d - delete file from file system image\n/u - update file\n/w - wipe file system image\n/x - extract file\n/xa - extract all files");
        }
    }
    class NeutrinoFileSystem
    {
        public FileCollection Files { get; set; }
        public string ImagePath { get; set; }
        public NeutrinoFileSystem(string fs)
        {
            ImagePath = fs;
            if (File.Exists(fs))
            {
                List<byte> bin = new List<byte>(File.ReadAllBytes(fs));
                if (bin[0] == (byte)'N' && bin[1] == (byte)'F' && bin[2] == (byte)'S')
                {
                    bin.RemoveAt(0);
                    bin.RemoveAt(0);
                    bin.RemoveAt(0);
                    int offset = BitConverter.ToInt32(bin.ToArray(), 0);
                    int pos = 4;
                    Files = new FileCollection();
                    while (pos < offset)
                    {
                        string fname = "";
                        while (bin[pos] != 0)
                        {
                            fname += (char)bin[pos];
                            pos += 1;
                        }
                        pos += 1;
                        int org = BitConverter.ToInt32(bin.ToArray(), pos);
                        pos += 4;
                        int end = BitConverter.ToInt32(bin.ToArray(), pos);
                        pos += 4;
                        byte[] file = new byte[end - org];
                        for (int i = org + offset; i < end + offset; i++)
                        {
                            file[i - offset - org] = bin[i];
                        }
                        Files.Add(fname, file.ToArray());
                    }
                }
                else Console.WriteLine("Invalid or corrupt filesystem image!");
            }
            else
            {
                Files = new FileCollection();
            }
        }
        public bool FileExists(string path)
        {
            return Files.ContainsKey(path);
        }
        public void CreateFile(string name)
        {
            Files.Add(name, new byte[0]);
        }
        public void CreateFile(string name, byte[] contents)
        {
            Files.Add(name, contents);
        }
        public void WriteFile(string name, byte[] contents)
        {
            if (Files.ContainsKey(name))
            {
                Files[name] = contents;
            }
        }
        public byte[] ReadFile(string name)
        {
            if (Files.ContainsKey(name))
            {
                return Files[name];
            }
            else throw new Exception("File " + name + " not found!");
        }
        public string ReadAllText(string name)
        {
            return Encoding.ASCII.GetString(ReadFile(name));
        }
        public void Delete(string file)
        {
            if (Files.ContainsKey(file))
            {
                Files.Remove(file);
            }
            else throw new Exception("File " + file + " not found!");
        }

        public void CreateDirectory(string path)
        {
            string p = path;
            if (!p.EndsWith("\\")) p += "\\";
            Files.Add(p, new byte[0]);
        }
        public void Commit()
        {
            List<byte> bin = new List<byte>();
            bin.Add((byte)'N');
            bin.Add((byte)'F');
            bin.Add((byte)'S');
            int offset = 0;
            for (int i = 0; i < Files.Items.Count; i++)
            {
                offset += Files.Items[i].Name.Length + 9;
            }
            bin.AddRange(BitConverter.GetBytes(offset));
            offset = 4;
            for (int i = 0; i < Files.Items.Count; i++)
            {
                bin.AddRange(Encoding.ASCII.GetBytes(Files.Items[i].Name + "\0"));
                bin.AddRange(BitConverter.GetBytes(offset));
                offset += Files.Items[i].Contents.Length;
                bin.AddRange(BitConverter.GetBytes(offset));
            }
            for (int i = 0; i < Files.Items.Count; i++)
            {
                bin.AddRange(Files.Items[i].Contents);
            }
            File.WriteAllBytes(ImagePath, bin.ToArray());
        }
    }
    class NtrFile
    {
        public string Name { get; set; }
        public byte[] Contents { get; set; }
        public NtrFile(string name, byte[] contents)
        {
            Name = name;
            Contents = contents;
        }
    }
    class NtrDirectory
    {
        public List<NtrDirectory> Directories { get; set; }
        public List<NtrFile> Files { get; set; }
        public string Name { get; set; }
        public NtrDirectory(string name, List<NtrDirectory> dirs, List<NtrFile> files)
        {
            Name = name;
            Directories = dirs;
            Files = files;
        }
    }
    class FileCollection
    {
        public List<NtrFile> Items { get; set; }
        public byte[] this[string index]
        {
            get
            {
                foreach (NtrFile ps in Items)
                    if (ps.Name == index)
                        return ps.Contents;
                throw new ArgumentOutOfRangeException();
            }
            set
            {
                for (int i = 0; i < Items.Count; i++)
                    if (Items[i].Name == index)
                    {
                        Items[i].Contents = value;
                        return;
                    }
                throw new ArgumentOutOfRangeException();
            }
        }
        public FileCollection()
        {
            Items = new List<NtrFile>();
        }
        public bool ContainsKey(string key)
        {
            foreach (NtrFile p in Items)
            {
                if (p.Name == key) return true;
            }
            return false;
        }
        public void Add(string key, byte[] value)
        {
            Items.Add(new NtrFile(key, value));
        }
        public void Remove(string key)
        {
            int index = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Name == key)
                {
                    index = i;
                    break;
                }
            }
            Items.RemoveAt(index);
        }
        public void Clear()
        {
            Items.Clear();
        }
    }
}
