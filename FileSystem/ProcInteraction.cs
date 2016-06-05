using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystem
{
    class ProcInteraction
    {
        public short proc_num,s_id;
        Semaphore mutex, full_count_1, full_count_2, empty_count_1, empty_count_2;
        FileSystem file_system;
        List<Process> process;
        public ProcInteraction(FileSystem system)
        {
            file_system = system;
            proc_num = 4;
            process = new List<Process>();
            for (int i = 0; i < proc_num; i++)
            {
                Process proces = new Process();
                proces.id = i;
                proces.pri = 10;
                proces.uid = 0;
                proces.stat = 'w';
                proces.name = ("process" + i.ToString()).ToCharArray() ;
                if(i<=1)
                    proces.proc = new Thread(new ParameterizedThreadStart(Producer));
                else
                    proces.proc = new Thread(new ParameterizedThreadStart(Consumer));
                proces.proc.Name = "Process" + i.ToString();
                process.Add(proces);
            }
        }
        public void List()
        {
            Console.WriteLine(" PID | PRI | UID | STAT | NAME ");
            Console.WriteLine("-------------------------------------------");
            for (int i = 0; i < proc_num; i++)
            {
                Console.Write(process[i].id);
                Console.Write("  ");
                Console.Write(process[i].pri);
                Console.Write("  ");
                Console.Write(process[i].uid);
                Console.Write("  ");
                Console.Write(process[i].stat);
                Console.Write("  ");
                Console.Write(process[i].name);
                Console.Write("  ");
                Console.WriteLine();
            }
            Console.WriteLine("-------------------------------------------");
        }
        public void Producer(object pr)
        {
            int item = 0,index;
            Process p=(Process)pr;
                //item++;
                if (p.id == 0)
                {
                index = 0;
                for(int i=0;i<3;i++)
                {
                    
                    while (!empty_count_1.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает места");
                        Thread.Sleep(2000);
                    }
                    while (!mutex.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает входа в крит. область");
                        Thread.Sleep(2000);
                    }
                    file_system.CreateShare(0, 24);
                    item = 2;
                    file_system.InsertShare(0, item, index);
                    index += 4;
                    Console.WriteLine("Записано " + item + " процессом " + new string(p.name));
                    Thread.Sleep(2000);
                    mutex.up();
                    full_count_1.up();

                }
                Console.WriteLine(new string(p.name) + " закончил работу");
            }
                else
                {
                index = 12;
                for (int i = 0; i < 3; i++)
                {
                    
                    while (!empty_count_2.down())
                    {
                       Console.WriteLine(new string(p.name) + " ожидает места");
                        Thread.Sleep(2000);
                    }
                    while (!mutex.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает входа в крит. область");
                        Thread.Sleep(2000);
                    }
                    file_system.CreateShare(0, 24);
                    item = 1;
                    file_system.InsertShare(0, item, index);
                    index += 4;
                    Console.WriteLine("Записано " + item + " процессом " + new string(p.name));
                    Thread.Sleep(2000);                   
                    mutex.up();
                    full_count_2.up();

                }
                Console.WriteLine(new string(p.name) + " закончил работу");
            }            
        }
        public void Consumer(object pr)
        {
            int index;
            Process p = (Process)pr;
            if (p.id == 2)
            {
                index = 0;
                for (int i = 0; i < 3; i++)
                {
                    
                    while (!full_count_1.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает элемент");
                        Thread.Sleep(2000);
                    }
                    while (!mutex.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает входа в крит. область");
                         Thread.Sleep(2000);
                    }                   
                    Console.WriteLine("Прочитано "+file_system.GetShare(0, index)+" процессом "+ new string(p.name));
                    index += 4;
                    Thread.Sleep(2000);                  
                    mutex.up();
                    empty_count_1.up();

                }
                Console.WriteLine(new string(p.name) + " закончил работу");
            }
            else
            {
                index = 12;
                for (int i = 0; i < 3; i++)
                {
                    
                    while (!full_count_2.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает элемент");
                        Thread.Sleep(2000);
                    }
                    while (!mutex.down())
                    {
                        Console.WriteLine(new string(p.name) + " ожидает входа в крит. область");
                        Thread.Sleep(2000);
                    }
                    Console.WriteLine("Прочитано " + file_system.GetShare(0, index) + " процессом " + new string(p.name));
                    index += 4;
                    Thread.Sleep(2000);                   
                    mutex.up();
                    empty_count_2.up();
                }
                Console.WriteLine(new string(p.name) + " закончил работу");
            }
            
        }
        public void GoWork()
        {

            mutex = new Semaphore(1, 1);
            empty_count_1 = new Semaphore(3, 3);
            empty_count_2 = new Semaphore(3, 3);
            full_count_1 = new Semaphore(0, 3);
            full_count_2 = new Semaphore(0, 3);
            s_id = 0;
            for (int i = 0; i < 4; i++)
            {
                process[i].proc.Start(process[i]);
            }
        }

    }
}
