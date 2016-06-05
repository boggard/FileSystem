using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Cryptography;

namespace FileSystem
{
    class Program
    {
        const int TotalSize = 100;
        static void createFile()
        {
            long length =  TotalSize * 1024 * 1024;
            FileStream fsystem = new FileStream("filesystem.bin", FileMode.OpenOrCreate);
            byte[] array = new byte[length];
            for (long i = 0; i < array.Length; i++)
                array[i] = 0;
            using (fsystem)
            {
                fsystem.Seek(0, SeekOrigin.Begin);
                fsystem.Write(array, 0, array.Length);
            }
        }
        private static void playSound(string path)
        {
            System.Media.SoundPlayer player =
                new System.Media.SoundPlayer();
            player.SoundLocation = path;
            player.Load();
            player.Play();
        }
        public static string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        static void Main(string[] args)
        {
           // createFile();
           // FileSystem.Formating(100,4096);
            FileSystem system = new FileSystem();
            ProcInteraction processes = new ProcInteraction(system);
          //  system.Create('f', 30, "users");
            string comand;
            string[] elements=new string[3];
            string path = "";
            while (true)
            {
                Console.WriteLine("Введите логин:");
                char[] login = Console.ReadLine().ToCharArray();
                char[] pass=new char[20];
                string password = "";
                Console.WriteLine("Введите пароль:");
                while(true)
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if(key.Key == ConsoleKey.Enter) {
                        Console.Write('\n');
                        break;
                    } else if (key.Key == ConsoleKey.Backspace)
                    {
                        password = password.Remove(password.Length - 1);
                        Console.Write(" ");
                        Console.Write("\b");
                    }
                    else
                    {
                        password += key.KeyChar;
                        Console.Write("\b");
                        Console.Write("*");
                    }
                }
                if (system.LogIn(login, password,false)||new string(login)=="go")
                {
                    Console.WriteLine("Вы вошли под логином " + new string(login).ToUpper());
                    //playSound("D:/2_1_2.wav");
                    // Console.WriteLine(system.current_user);
                    //break;
                }
                else
                {
                    Console.WriteLine("Вы же не забыли пароль?");
                    continue;
                }
                
                while(true)
                { 
                    Console.Write(path+'>');
                    comand = Console.ReadLine();
                    elements = comand.Split(' ');
                    MD5.Create();
                    if (elements[0] == "create")
                    {
                        if (elements[1] == "-file")
                        {
                            char[][] info = new char[4096][];
                            int i;
                             for (i = 0; i < 4096; i++)
                             {
                                 info[i] = Console.ReadLine().ToCharArray();
                                 if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                                     break;
                             }
                             Array.Resize<char[]>(ref info, i + 1);
                           
                            
                            system.Write(info, system.Create('f', 31, elements[2]));
                        }
                        if (elements[1] == "-dir")
                            system.Create('d', 33, elements[2]);
                        if (elements[1] == "-user")
                        {
                            Console.WriteLine("Введите логин");
                            MD5 md5Hash = MD5.Create();
                            char[][] info = new char[2][];
                            string[] user_info = new string[2];
                            user_info[0] = Console.ReadLine();
                            Console.WriteLine("Введите пароль");
                            while (true)
                            {
                                ConsoleKeyInfo key = Console.ReadKey();
                                if (key.Key == ConsoleKey.Enter)
                                {
                                    Console.Write('\n');
                                    break;
                                }
                                else if (key.Key == ConsoleKey.Backspace)
                                {
                                    user_info[1] = user_info[1].Remove(password.Length - 1);
                                    Console.Write(" ");
                                    Console.Write("\b");
                                }
                                else
                                {
                                    user_info[1] += key.KeyChar;
                                    Console.Write("\b");
                                    Console.Write("*");
                                }
                            }
                            user_info[1] = GetMd5Hash(md5Hash, user_info[1]);
                            info[0] = user_info[0].ToCharArray();
                            info[1] = user_info[1].ToCharArray();
                            system.AddWrite(info, "users");
                        }
                    }
                    if(elements[0]=="remove")
                    {
                        if (elements[1] == "-file"|| elements[1] == "-dir")
                        {
                            system.Remove(elements[2]);
                        }    
                        if (elements[1]=="-user")
                        {
                            system.RemoveUser(elements[2].ToCharArray());
                        }                  
                    }
                    if(elements[0]=="write")
                    {
                        char[][] info = new char[4096][];
                        int i;
                        for (i = 0; i < 4096; i++)
                        {
                            info[i] = Console.ReadLine().ToCharArray();
                            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                                break;
                        }
                        Array.Resize<char[]>(ref info, i + 1);
                        system.AddWrite(info,elements[1]);
                    }
                    if (elements[0] == "read")
                    {
                        char[] info = system.Read(elements[1]);
                        char[] st = new char[info.Length];
                        int j = 0;
                        for (int i = 0; i < info.Length; i++)
                        {
                            if (info[i] == '.')
                            {
                                Array.Copy(info, j, st, 0, i - j);
                                Array.Resize<char>(ref st, i - j);
                                j = i + 1;
                                Console.WriteLine(st);
                            }
                            Array.Resize<char>(ref st, info.Length);
                        }
                        if (st[0]=='\0')
                            Console.WriteLine(info);
                    }
                    if(elements[0]=="rename")
                    {
                        system.Rename(elements[1], elements[2]);
                    }
                    if (elements[0] == "cd")
                    {
                        if (elements.Length != 1)
                        {
                            int temp=system.dir_inodeid;
                            path += "/" + new string(system.gotoDir(elements[1]));
                            if (temp == system.dir_inodeid)
                                path = path.Remove(path.Length-1);
                        }
                        else
                            path = new string(system.gotoDir(""));
                    }
                    if(elements[0]=="move")
                    {
                        system.Move(elements[2].ToCharArray(), elements[1].ToCharArray());
                    }
                    if(elements[0]=="list")
                    {
                        Console.WriteLine("Файл           | Размер   | Права доступа | Владелец | Дата и время создания |");
                        Console.WriteLine("------------------------------------------------------------------------------");
                        for (int i = 0; i < system.List().Length; i++)
                        {
                            Console.WriteLine(system.List()[i]);                           
                        }
                        Console.WriteLine("------------------------------------------------------------------------------");

                    }
                    if(elements[0]=="chmod")
                    {
                        system.ChangeChmod(elements[2], (short)Convert.ToInt32(elements[1]));
                    }
                    if(elements[0]=="ps")
                    {
                        if (elements[1] == "-list")
                        {
                            processes.List();
                        }
                        if(elements[1]=="-work")
                        {
                            processes.GoWork();
                        }
                    }
                    if (elements[0] == "exit")
                    {
                        system.current_user = 0;
                        break;
                    }
                    if(elements[0]=="bb")
                    {
                        return;
                    }
                    
            }
            }
        }
    }
}
