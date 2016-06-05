using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;

namespace FileSystem
{
    class FileSystem
    {
        public const string system_path="filesystem.bin";
        public int dir_inodeid;
        public int file_count;
        public short current_user;
        public short[] mods_read = new short[]{ 1, 3 };
        public short[] mods_write = new short[] { 2, 3};
        
        public struct SharedMemory
        {
            public short id;
            public short size;
            public int inode_id;
        }
        public struct SuperBlock
        {
           // public string filesystem_type;
            public int datablocks_count;
            public int rootdir_size;
            public int inodearray_size;
            public int fattable_size;
            public int freeblocks_count;
            public int freeinode_count;
            public int users_count;
            public int offset_fattable;
            public int offset_inodearray;
            public int offset_rootdir;
            public int offset;
        }
        public struct Inode
        {
            public char file_type;
            public int inode_id;
            public short user_id;
            public short chmod;
            public int file_size;
            public int blockadress_1;
            public DateTime filecreate_time;
            public DateTime filemodify_time;
            public DateTime inodemodify_time;
        }
        public struct Dir
        {
            public int inode_id;
            public char[] file_name;
        }
        public Dir[] rootdir;
        public short[] FatTable;
        public SuperBlock sup_block;
        public Inode[] inode;
        public List<SharedMemory> memory;
        public FileSystem()
        {
            Encoding enc8 = Encoding.Default;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                sup_block = ReadStruct<SuperBlock>(br);
                FatTable = new short[sup_block.datablocks_count];
                inode = new Inode[sup_block.datablocks_count];
                file_count = sup_block.rootdir_size / 19;
                rootdir = new Dir[file_count];//подумать
                fsystem.Seek(sup_block.offset_fattable, SeekOrigin.Begin);
                for (int i = 0; i < sup_block.datablocks_count; i++)
                    FatTable[i] = (short)br.ReadInt32();
                fsystem.Seek(sup_block.offset_inodearray, SeekOrigin.Begin);
                for (int i = 0; i < sup_block.datablocks_count; i++)
                    inode[i] = ReadStruct<Inode>(br);
                fsystem.Seek(sup_block.offset_rootdir, SeekOrigin.Begin);
                for (int i = 0; i < rootdir.Length; i++)
                {
                    byte[] mass = new byte[15];
                    rootdir[i].inode_id = br.ReadInt32();
                    rootdir[i].file_name = new char[15];
                    mass = br.ReadBytes(rootdir[i].file_name.Length);
                    rootdir[i].file_name = enc8.GetChars(mass);
                    //rootdir[i] = ReadStruct<Dir>(br);
                }
                memory = new List<SharedMemory>();
                dir_inodeid = 0;
                
            }
        }
        public static void WriteStruct<T>(BinaryWriter Writer, T obj) where T : struct
        {
            int rawsize = Marshal.SizeOf(typeof(T));
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(obj, buffer, false);
            byte[] rawdatas = new byte[rawsize];
            Marshal.Copy(buffer, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(buffer);
            Writer.Write(rawdatas);
        }
        public static T ReadStruct<T>(BinaryReader reader) where T : struct
        {
            byte[] rawData = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            var returnObject = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return returnObject;
        }
        public static void Formating(int TotalSize,int BlockSize)
        {
            SuperBlock sup_block = new SuperBlock();
            //sup_block.filesystem_type = new char[5];
            //sup_block.filesystem_type = "VarFS";
            sup_block.rootdir_size = BlockSize;
            sup_block.datablocks_count = TotalSize*1024*1024 / BlockSize;
            sup_block.inodearray_size = sup_block.datablocks_count * Marshal.SizeOf<Inode>();
            sup_block.fattable_size = sup_block.datablocks_count * Marshal.SizeOf(new short());
            sup_block.freeblocks_count = sup_block.fattable_size;
            sup_block.freeinode_count = sup_block.inodearray_size;
            sup_block.users_count = 0;
            sup_block.offset_fattable = Marshal.SizeOf(sup_block);
            sup_block.offset_inodearray = sup_block.offset_fattable + sup_block.fattable_size;
            sup_block.offset_rootdir = sup_block.offset_inodearray + sup_block.inodearray_size;
            sup_block.offset = sup_block.offset_rootdir + sup_block.rootdir_size;
            Inode[] inode = new Inode[sup_block.datablocks_count];
            Dir[] rootdir = new Dir[sup_block.rootdir_size / 19];//подумать
            short[] fattable = new short[sup_block.datablocks_count];
           /* inode[0].file_type = 'd';
            inode[0].inode_id = 0;
            inode[0].user_id = 0;
            inode[0].chmod = 777;
            //fattable[0] = -1;
            inode[0].blockadress_1 = -1;
            inode[0].file_size = 0;
            inode[0].filecreate_time = DateTime.Now;
            inode[0].inodemodify_time = DateTime.Now;
            inode[0].filemodify_time = DateTime.Now;*/
            for(int i = 0; i < inode.Length; i++)
            {
                if (i == 0)
                {
                    inode[i].file_type = 'd';
                    inode[i].inode_id = i;
                    inode[i].user_id = 0;
                    inode[i].chmod = 44;
                    inode[i].blockadress_1 = -1;
                    inode[i].file_size=0;
                    inode[i].filecreate_time = DateTime.Now;
                    inode[i].inodemodify_time = DateTime.Now;
                    inode[i].filemodify_time = DateTime.Now;
                }
                else
                {
                    inode[i].inode_id = i;
                    inode[i].blockadress_1 = 0;
                    inode[i].file_size = 0;
                    inode[i].filecreate_time = DateTime.Now;
                    inode[i].inodemodify_time = DateTime.Now;
                    inode[i].filemodify_time = DateTime.Now;
                }
            }
            for(int i = 0; i < fattable.Length; i++)
            {
                if (i == 0)
                    fattable[i] = -1;
                else
                    fattable[i] = 0;
            }
            for(int i = 0; i < rootdir.Length; i++)
            {
                rootdir[i].inode_id = -1;
                rootdir[i].file_name = new char[15];
            }
           // Console.WriteLine(rootdir[0].file_name.Length);
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate,FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                FileSystem.WriteStruct<SuperBlock>(br, sup_block);
                fsystem.Seek(sup_block.offset_fattable, SeekOrigin.Begin);
                foreach (short i in fattable)
                {
                    br.Write(i);
                }
                foreach (Inode i in inode)
                {
                    FileSystem.WriteStruct<Inode>(br, i);
                }
                foreach (Dir i in rootdir)//подмать
                {
                    //FileSystem.WriteStruct<Dir>(br, i);
                    br.Write(i.inode_id);
                    br.Write(i.file_name);
                }
            }
            /* fsystem = new FileStream("D:/FileSystem/filesystem.bin", FileMode.Open, FileAccess.ReadWrite);
             using (BinaryReader br = new BinaryReader(fsystem))
             {
                 fsystem.Seek(sup_block.offset_fattable, SeekOrigin.Begin);
                short z=(short)br.ReadInt32(); 
               // SuperBlock aup_block = FileSystem.ReadStruct<SuperBlock>(br);
                Console.WriteLine(z);
             }       */    
        }
        public int Create(char type,short chm,string name)
        {
            long adress;
            FileStream fsystem;
            Dir[] dir;
            Encoding enc8 = Encoding.Default;
            for (int i=0;i<inode.Length;i++)
            {
                if(inode[i].blockadress_1==0)
                {
                    short fat_index,file_id=0;
                    inode[i].file_type = type;
                    inode[i].inode_id = i;
                    inode[i].user_id=current_user;
                    inode[i].chmod = chm;
                    if (type == 'd')
                        inode[i].file_size = sup_block.rootdir_size;
                    for (fat_index=0;fat_index<FatTable.Length;fat_index++)
                        if(FatTable[fat_index]==0)
                        {
                            FatTable[fat_index] = -1;
                            inode[i].blockadress_1 = fat_index;
                            sup_block.freeblocks_count--;
                            sup_block.freeinode_count--;
                            break;
                        }
                    inode[i].filecreate_time = DateTime.Now;
                    inode[i].inodemodify_time = DateTime.Now;
                    dir = new Dir[file_count];
                    if (dir_inodeid == 0)
                        adress = sup_block.offset_rootdir;
                    else
                        adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
                    fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
                    using (BinaryReader br = new BinaryReader(fsystem))
                    {
                        fsystem.Seek(adress, SeekOrigin.Begin);
                        for (int j = 0; j < dir.Length; j++)
                        {
                            byte[] mass = new byte[15];
                            dir[j].inode_id = br.ReadInt32();
                            dir[j].file_name = new char[15];
                            mass = br.ReadBytes(dir[j].file_name.Length);
                            dir[j].file_name = enc8.GetChars(mass);
                            mass = new byte[5];
                        }
                    }
                    for (short j = 0; j < dir.Length; j++)
                    {
                        name = name.PadRight(dir[j].file_name.Length);
                        if (new string(dir[j].file_name) == name)
                        {
                            Console.WriteLine("В этой директории уже есть файл с таким именем"); 
                            return -1;
                        }
                    }
                        for (short j = 0; j < dir.Length; j++)
                        if (dir[j].inode_id == -1)
                        {
                            name = name.PadRight(dir[j].file_name.Length);
                            dir[j].inode_id = i;
                            dir[j].file_name = name.ToCharArray();
                            file_id = j;
                            break;
                        }
                    if (dir_inodeid == 0)
                        rootdir = dir;
                    fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    using (BinaryWriter br = new BinaryWriter(fsystem))
                    {
                        fsystem.Seek(sup_block.offset_fattable+2*fat_index*Marshal.SizeOf(FatTable[fat_index]), SeekOrigin.Begin);
                        //br.Write(FatTable[fat_index]);
                        br.Write(FatTable[fat_index]);
                        fsystem.Seek(sup_block.offset_inodearray + i * Marshal.SizeOf<Inode>(),SeekOrigin.Begin);
                        FileSystem.WriteStruct<Inode>(br, inode[i]);
                        adress += file_id*sup_block.rootdir_size/file_count;
                        fsystem.Seek(adress, SeekOrigin.Begin);
                        br.Write(dir[file_id].inode_id);
                        br.Write(dir[file_id].file_name);
                        // Console.WriteLine(fsystem.Position);
                        if (inode[i].file_type == 'd')
                        {
                            adress = sup_block.offset + inode[inode[i].inode_id].blockadress_1 * sup_block.rootdir_size;
                            fsystem.Seek(adress, SeekOrigin.Begin);
                            for (int j = 0; j < rootdir.Length; j++)
                            {
                                br.Write(-1);
                                br.Write(new char[15]);
                            }
                        }
                    }                  
                    return inode[i].inode_id;
                }               
            }
            return -1;
            
        }
        public void Write(char[][] inf,int inode_id)
        {
            if(inode_id==-1)
            {
                return;
            }
            /*тут нужно дописать код для случая когда размер файла > 4096 и записать измененную Fat в файл*/
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {                
                int i, fat_index = -1,fat_lastindex=inode[inode_id].blockadress_1,str=0,curr_size=0;
                bool more=false;
                long adress= sup_block.offset + inode[inode_id].blockadress_1 * sup_block.rootdir_size;
                while (true)
                {
                    fsystem.Seek(adress, SeekOrigin.Begin);
                    for (i = str; i < inf.Length; i++)
                    {
                        br.Write(inf[i]);
                        br.Write('.');
                        if (curr_size + inf[i].Length + 1 > sup_block.rootdir_size)
                        {
                            more = true;
                            str = i;
                            break;
                        }
                        more = false;
                        inode[inode_id].file_size += inf[i].Length + 1;
                        curr_size += inf[i].Length + 1;
                    }
                    if (more)
                    {
                        for (short j = 0; j < FatTable.Length; j++)
                            if (FatTable[j] == 0)
                            {
                                FatTable[j] = -1;
                                if (FatTable[inode[inode_id].blockadress_1] == -1)
                                    FatTable[inode[inode_id].blockadress_1] = j;
                                else
                                {
                                    FatTable[fat_index] = j;
                                    fat_lastindex = fat_index;
                                }
                                fat_index = j;
                                sup_block.freeblocks_count--;
                                break;
                            }
                        fsystem.Seek(sup_block.offset_fattable + 2 * fat_lastindex * Marshal.SizeOf(FatTable[fat_lastindex]), SeekOrigin.Begin);
                        br.Write(FatTable[fat_lastindex]);
                        br.Write(-1);
                        adress = sup_block.offset + FatTable[fat_lastindex] * sup_block.rootdir_size;
                        curr_size = 0;
                    }
                    else
                        break;
                }
            }           
            inode[inode_id].filemodify_time = DateTime.Now;
            inode[inode_id].inodemodify_time = DateTime.Now;
            fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(sup_block.offset_inodearray + inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                FileSystem.WriteStruct<Inode>(br, inode[inode_id]);

            }
            
        }
        public void AddWrite(char[][] inf,string fname)//походу тут по пизде идет
        {
            if(fname=="users")
            {
                if(LogIn(inf[0],"",true))
                {
                    Console.WriteLine("Пользователь с таким именем уже существует");
                    return;
                }
            }
            long adress;
            int inode_id = -1;
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                    mass = new byte[5];
                }
            }
            for (short j = 0; j < dir.Length; j++)//подумать
                if (new string(dir[j].file_name) == new string(fname.PadRight(dir[j].file_name.Length).ToCharArray()))
                {
                    inode_id = dir[j].inode_id;
                    break;
                }
            if (inode_id != -1)
            {
                if ((current_user == inode[inode_id].user_id && !mods_write.Contains<short>((short)(inode[inode_id].chmod / 10))) || (current_user != inode[inode_id].user_id && !mods_write.Contains<short>((short)(inode[inode_id].chmod % 10)))&&current_user!=0)
                {                   
                    Console.WriteLine("У вас нет необходимых прав доступа для записи в этот файл");
                    return;
                }
                adress = inode[inode_id].blockadress_1 * sup_block.rootdir_size;
                fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using (BinaryWriter br = new BinaryWriter(fsystem))
                {
                    fsystem.Seek(sup_block.offset + adress+ inode[inode_id].file_size, SeekOrigin.Begin);
                    if (fname == "users")
                    {      
                        br.Write(sup_block.users_count.ToString().ToCharArray());
                        sup_block.users_count++;
                        br.Write('.');
                        inode[inode_id].file_size += sup_block.users_count.ToString().ToCharArray().Length+1;                       
                    }
                    int i, str = 0, curr_size, fat_index = -1,fat_lastindex = inode[inode_id].blockadress_1;
                    curr_size = inode[inode_id].file_size - (inode[inode_id].file_size / sup_block.rootdir_size) * sup_block.rootdir_size;
                    bool more=false;
                    int block=inode[inode_id].blockadress_1;
                    while(FatTable[block]!=-1)
                        block = FatTable[block];
                    fat_index = block;
                    adress = sup_block.offset + block * sup_block.rootdir_size+ curr_size;
                    while (true)
                    {
                        fsystem.Seek(adress, SeekOrigin.Begin);
                        for (i = str; i < inf.Length; i++)
                        {
                            br.Write(inf[i]);
                            br.Write('.');
                            if (curr_size + inf[i].Length + 1 > sup_block.rootdir_size)
                            {
                                more = true;
                                str = i;
                                break;
                            }
                            more = false;
                            inode[inode_id].file_size += inf[i].Length + 1;
                            curr_size += inf[i].Length + 1;
                        }
                        if (more)
                        {
                            for (short j = 0; j < FatTable.Length; j++)
                                if (FatTable[j] == 0)
                                {
                                    FatTable[j] = -1;
                                    if (FatTable[inode[inode_id].blockadress_1] == -1)
                                        FatTable[inode[inode_id].blockadress_1] = j;
                                    else
                                    {
                                        FatTable[fat_index] = j;
                                        fat_lastindex = fat_index;
                                    }
                                    fat_index = j;
                                    sup_block.freeblocks_count--;
                                    break;
                                }
                            fsystem.Seek(sup_block.offset_fattable + 2 * fat_lastindex * Marshal.SizeOf(FatTable[fat_lastindex]), SeekOrigin.Begin);
                            br.Write(FatTable[fat_lastindex]);
                            br.Write(-1);
                            adress = sup_block.offset + FatTable[fat_lastindex] * sup_block.rootdir_size;
                            curr_size = 0;
                        }
                        else
                            break;
                    }
                }
                inode[inode_id].filemodify_time = DateTime.Now;
                inode[inode_id].inodemodify_time = DateTime.Now;
                fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using (BinaryWriter br = new BinaryWriter(fsystem))
                {
                    fsystem.Seek(0, SeekOrigin.Begin);
                    FileSystem.WriteStruct<SuperBlock>(br, sup_block);
                    fsystem.Seek(sup_block.offset_inodearray + inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                    FileSystem.WriteStruct<Inode>(br, inode[inode_id]);
                }
            }
        }
        public char[] Read(string fname)
        {
            long adress;
            int inode_id=-1;
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                    mass = new byte[5];
                }
            }
            for (short j = 0; j < dir.Length; j++)//подумать
                if (new string(dir[j].file_name) == new string(fname.PadRight(dir[j].file_name.Length).ToCharArray()))
                {
                    inode_id = dir[j].inode_id;
                    break;
                }
            if (inode_id != -1)
            {
                char[] result = new char[8192];
                if ((current_user == inode[inode_id].user_id && !mods_read.Contains<short>((short)(inode[inode_id].chmod / 10))) || (current_user != inode[inode_id].user_id && !mods_read.Contains<short>((short)(inode[inode_id].chmod % 10)))&&current_user!=0)
                     return "У вас нет необходимых прав доступа для чтения этого файла".ToCharArray();
                adress = inode[inode_id].blockadress_1 * sup_block.rootdir_size;
                fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
                int block=inode[inode_id].blockadress_1,curr_count=0;
                using (BinaryReader br = new BinaryReader(fsystem))
                {
                    while (true)
                    {
                        fsystem.Seek(sup_block.offset + adress, SeekOrigin.Begin);
                        if (inode[inode_id].file_size <= sup_block.rootdir_size)
                            result = br.ReadChars(inode[inode_id].file_size);
                        else
                        {
                            int size = inode[inode_id].file_size - (inode[inode_id].file_size / sup_block.rootdir_size) * sup_block.rootdir_size;
                            Array.Copy(br.ReadChars(sup_block.rootdir_size), 0, result, curr_count, sup_block.rootdir_size);
                            curr_count = sup_block.rootdir_size;
                        }
                        if (FatTable[block] != -1)
                        {
                            adress = FatTable[block] * sup_block.rootdir_size;
                            block = FatTable[block];
                        }
                        else
                            return result;
                    }
                }
            }
            else
            {
                return "Error".ToCharArray();
            }
        }
        public char[] gotoDir(string dirname)
        {
            if (dirname == "")
            {
                dir_inodeid = 0;
                return "".ToCharArray();
            }
            long adress;
            Dir[] dir=new Dir[file_count];
            Encoding enc8 = Encoding.Default;
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int i = 0; i < dir.Length; i++)
                {
                    byte[] mass = new byte[15];
                    dir[i].inode_id = br.ReadInt32();
                    dir[i].file_name = new char[15];
                    mass = br.ReadBytes(dir[i].file_name.Length);
                    dir[i].file_name = enc8.GetChars(mass);
                    //rootdir[i] = ReadStruct<Dir>(br);
                }

            }
            char[] result = new char[dir[0].file_name.Length];
            foreach (Dir d in dir)
            {
                char[] name = dirname.PadRight(d.file_name.Length).ToCharArray();
                if (new string(d.file_name)==new string(name))
                {
                    dir_inodeid = d.inode_id;
                    result= d.file_name;
                    break;
                }                   
            }
            for(int i=0; i<result.Length; i++)
            {
                if(result[i]==32||result[i]=='\0')
                {
                    Array.Resize<char>(ref result, i);
                    break;
                }
            }
            return result;

        }
        public bool LogIn(char[] login,string password,bool for_add)
        {
            char[] info = Read("users");
            char[] st = new char[info.Length];
            int j = 0,charact=0;
            short cur_us=0;
            bool id = false;
            MD5 md5Hash = MD5.Create();
            password=Program.GetMd5Hash(md5Hash, password);
            for (int i = 0; i < info.Length; i++)
            {
                if (info[i] == '.')
                {
                    Array.Copy(info, j, st, 0, i - j);
                    Array.Resize<char>(ref st, i - j);
                    j = i + 1;
                    if(charact==0)
                    {
                        cur_us = (short)Convert.ToInt32(new string(st));
                        charact = 1;
                        continue;
                    }
                    if (charact==1)
                    {
                        if (new string(login) == new string(st))
                        {                            
                            id = true;
                        }
                        charact = 2;
                        continue;
                    }
                    if (charact==2)
                    {
                        if ((password == new string(st)||for_add) && id)
                        {
                            if(!for_add)                            
                                current_user = cur_us;
                            return true;
                        }
                        else
                            charact = 0;
                        continue;
                    }
                }
                Array.Resize<char>(ref st, info.Length);
            }
            return false;
        }
        public string FindUser(short user_id)
        {
            short curr_us = current_user;
            current_user = 0;
            char[] info = Read("users");
            current_user = curr_us;
            char[] u_n=new char[info.Length];
            bool id = true, log = false, pas = false; ;
            for (int i = 0; i < info.Length; i++)
            {
                if (!pas && log && info[i] == '.')
                {
                    pas = true;
                    log = false;
                    continue;
                }
                if (pas && info[i] == '.')
                {
                    //id = true;
                    pas = false;
                    continue;
                }
                if(!pas && !log && !id && info[i] == '.')
                {
                    id = true;
                    continue;
                }
                if (id && info[i].ToString() == user_id.ToString())
                {
                    int j = i + 2, ind = 0;
                    while (true)
                    {
                        if (info[j] == '.')
                            break;
                        u_n[ind] = info[j];
                        ind++; j++;
                    }
                    Array.Resize<char>(ref u_n, ind + 1);
                    return new string(u_n);
                }
                else
                {
                    if (id)
                    {
                        id = false;
                        log = true;
                    }
                }      
            }
            return "";
        }
        public void Remove(string fname)
        {
            long adress;
            int inode_id = -1;
            int remove_indx=-1;
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                }
            }
            for (short j = 0; j < dir.Length; j++)//подумать
                if (new string(dir[j].file_name) == new string(fname.PadRight(dir[j].file_name.Length).ToCharArray()))
                {
                    inode_id = dir[j].inode_id;
                    remove_indx = j;
                    break;
                }
            if (inode_id != -1)
            {
                if ((current_user == inode[inode_id].user_id && !mods_write.Contains<short>((short)(inode[inode_id].chmod / 10))) || (current_user != inode[inode_id].user_id && !mods_write.Contains<short>((short)(inode[inode_id].chmod % 10)))&&current_user!=0)
                {
                    Console.WriteLine("У вас нет необходимых прав доступа для удаления этого файла");
                    return;
                }
                dir[remove_indx].inode_id = -1;
                dir[remove_indx].file_name = new char[15];
                if (inode[inode_id].file_type == 'd')
                    RemoveDir(inode_id);     
                fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using (BinaryWriter br = new BinaryWriter(fsystem))
                {
                    fsystem.Seek(adress, SeekOrigin.Begin);
                    for (short j = 0; j < dir.Length; j++)//подумать
                    {
                        br.Write(dir[j].inode_id);
                        br.Write(dir[j].file_name);
                    }
                    adress = inode[inode_id].blockadress_1 * sup_block.rootdir_size;
                    fsystem.Seek(sup_block.offset + adress, SeekOrigin.Begin);
                    if (inode[inode_id].file_type == 'f')
                    {
                        for (int i = 0; i < inode[inode_id].file_size; i++)
                        {
                            br.Write('\0');
                        }
                    }
                    else
                    {
                        for(int i=0;i<sup_block.rootdir_size;i++)
                        {
                            br.Write('\0');
                        }
                    }
                    FatTable[inode[inode_id].blockadress_1] = 0;
                    fsystem.Seek(sup_block.offset_fattable + 2 * inode[inode_id].blockadress_1 * Marshal.SizeOf(FatTable[inode[inode_id].blockadress_1]), SeekOrigin.Begin);
                    br.Write(FatTable[inode[inode_id].blockadress_1]);
                    inode[inode_id].chmod = 0;
                    inode[inode_id].user_id = 0;
                    inode[inode_id].file_type = '\0';
                    inode[inode_id].blockadress_1 = 0;
                    inode[inode_id].file_size = 0;
                    inode[inode_id].filecreate_time = DateTime.Now;
                    inode[inode_id].inodemodify_time = DateTime.Now;
                    inode[inode_id].filemodify_time = DateTime.Now;
                    fsystem.Seek(sup_block.offset_inodearray + inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                    FileSystem.WriteStruct<Inode>(br, inode[inode_id]);
                }
                
            }
        }
        public void RemoveDir(int inode_id)
        {
            Dir[] dir = ReadDir(inode_id);
            int temp = dir_inodeid;
            for(int i=0;i<dir.Length;i++)
            {
                if(dir[i].inode_id!=-1)
                {
                    dir_inodeid = inode_id;
                    Remove(new string(dir[i].file_name));
                }              
            }
            dir_inodeid = temp;
        }
        public void RemoveUser(char[] login)
        {

            char[] info = Read("users");
            int inode_id = 1;
            char[][] result = new char[1][];
            char[] st = new char[info.Length];
            int j = 0, charact = 0;
            int cur_us = 0,n=0;
            MD5 md5Hash = MD5.Create();
            int i = 0;
            for (i = 0; i < info.Length; i++)
            {
                if (info[i] == '.')
                {
                    Array.Copy(info, j, st, 0, i - j);
                    Array.Resize<char>(ref st, i - j);
                    j = i + 1;
                    if (charact == 1)
                    {
                        if (new string(login) == new string(st))
                        {
                            int m;
                            for(m=cur_us;m< i+1;m++)
                            {
                                info[m] = '\0';
                            }
                            while(true)
                            {
                                if (info[m] == '.')
                                {
                                    info[m] = '\0';
                                    break;
                                }
                                else
                                    info[m] = '\0';
                                m++;
                            }
                            result[0] = new char[info.Length];
                            for(m=0;m<info.Length;m++)
                            {
                                if(info[m]!='\0')
                                {
                                    result[0][n] = info[m];
                                    n++;
                                }
                            }
                            break;
                        }
                        charact++;
                        continue;
                    }
                    if (charact == 0)
                    {
                        cur_us = i-1;
                        charact++;
                    }
                    if(charact==2)
                        charact = 0;
                }
                Array.Resize<char>(ref st, info.Length);
            }
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                sup_block.users_count--;
                fsystem.Seek(sup_block.offset + inode[inode_id].blockadress_1 * sup_block.rootdir_size, SeekOrigin.Begin);
                for (j = 0; j < result.Length; j++)
                {
                    br.Write(result[j]);
                    inode[inode_id].file_size -= result[j].Length-n;
                }
            }
            inode[inode_id].filemodify_time = DateTime.Now;
            fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                FileSystem.WriteStruct<SuperBlock>(br, sup_block);
                fsystem.Seek(sup_block.offset_inodearray + inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                FileSystem.WriteStruct<Inode>(br, inode[inode_id]);
            }
        }
        public string[] List()
        {
            long adress;
            int string_count = 0;
            string[] list = new string[file_count];
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                    mass = new byte[5];
                }
            }
            for (short j = 0; j < dir.Length; j++)//подумать
            {
                if (dir[j].inode_id != -1)
                {
                    list[string_count] += new string(dir[j].file_name) + "|" + inode[dir[j].inode_id].file_size.ToString().PadRight(10)+"|"+inode[dir[j].inode_id].chmod.ToString().PadRight(15)+"|"+ FindUser(inode[dir[j].inode_id].user_id)+"    |"+ inode[dir[j].inode_id].filecreate_time;
                    string_count++;
                }
            }
            Array.Resize<string>(ref list, string_count);
            return list;
        }
        public void Move(char[] path,char[] fname)
        {
            int file_inode_id = -1;
            long adress;
            Dir[] dir = new Dir[file_count];
            dir = ReadDir(dir_inodeid);
            for (short j = 0; j < dir.Length; j++)
            {
                if (new string(dir[j].file_name) == new string(fname).PadRight(dir[j].file_name.Length))
                {
                    file_inode_id = dir[j].inode_id;
                    dir[j].inode_id = -1;
                    dir[j].file_name = new char[15];
                    break;
                }
            }
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (short j = 0; j < dir.Length; j++)//подумать
                {
                    br.Write(dir[j].inode_id);
                    br.Write(dir[j].file_name);
                }
            }
            int dir_count=1;
            for (int i = 1; i < path.Length; i++)
                if (path[i] == '/')
                    dir_count++;
            char[][] dirs=new char[dir_count][];
            int from = 1,file_id;
            for(int i=1,j=0;i<path.Length;i++)
            {
                if (path[i] == '/'||i==path.Length-1)
                {
                    dirs[j] = new char[i - from+1];
                    Array.Copy(path, from, dirs[j], 0, i - from+1);
                    j++;
                    from = i + 1;
                }
            }
            Encoding enc8 = Encoding.Default;
            int inode_id = FindDir(0, dirs, 0);
            dir = ReadDir(inode_id);
            for (short j = 0; j < dir.Length; j++)
            {
                if (new string(dir[j].file_name) == new string(fname))
                {
                    Console.WriteLine("В этой директории уже есть файл с таким именем");
                    return;
                }
            }
            for (short j = 0; j < dir.Length; j++)
                if (dir[j].inode_id == -1)
                {
                    dir[j].inode_id = file_inode_id;
                    //Array.Resize<char>(ref fname, dir[j].file_name.Length);
                    dir[j].file_name = new string(fname).PadRight(dir[j].file_name.Length).ToCharArray();
                    file_id = j;
                    break;
                }
            if (inode_id == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[inode_id].blockadress_1 * sup_block.rootdir_size;
            fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (short j = 0; j < dir.Length; j++)//подумать
                {
                    br.Write(dir[j].inode_id);
                    br.Write(dir[j].file_name);
                }
            }
            }
        public Dir[] ReadDir(int inode_id)
        {
            long adress;
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (inode_id == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[inode_id].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                }
            }
            return dir;
        }
        public int FindDir(int inode_id,char[][] dirs,int index)
        {
            if (index == dirs.Length)
                return inode_id;
            long adress;
            Encoding enc8 = Encoding.Default;
            Dir[] dir = new Dir[file_count];
            if (inode_id == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[inode_id].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (int j = 0; j < dir.Length; j++)
                {
                    byte[] mass = new byte[15];
                    dir[j].inode_id = br.ReadInt32();
                    dir[j].file_name = new char[15];
                    mass = br.ReadBytes(dir[j].file_name.Length);
                    dir[j].file_name = enc8.GetChars(mass);
                }
            }
            for (short j = 0; j < dir.Length; j++)//подумать
                if (new string(dir[j].file_name) == new string(dirs[index]).PadRight(dir[j].file_name.Length))
                {
                    return FindDir(dir[j].inode_id,dirs,index+1);
                }
            return - 1;

        }
        public void Rename(string oldname,string newname)
        {
            long adress;
            Dir[] dir = ReadDir(dir_inodeid);
            for (short j = 0; j < dir.Length; j++)//подумать
                if (new string(dir[j].file_name) == new string(oldname.PadRight(dir[j].file_name.Length).ToCharArray()))
                {
                    dir[j].file_name = newname.PadRight(dir[j].file_name.Length).ToCharArray();
                }
            if (dir_inodeid == 0)
                adress = sup_block.offset_rootdir;
            else
                adress = sup_block.offset + inode[dir_inodeid].blockadress_1 * sup_block.rootdir_size;
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for (short j = 0; j < dir.Length; j++)//подумать
                {
                    br.Write(dir[j].inode_id);
                    br.Write(dir[j].file_name);
                }
            }
        }
        public void ChangeChmod(string fname,short chmod)
        {
            Dir[] dir = ReadDir(dir_inodeid);
            int i;
            for(i=0;i<dir.Length;i++)
            {
                if (new string(dir[i].file_name) == new string(fname.PadRight(dir[i].file_name.Length).ToCharArray()))
                {
                    inode[dir[i].inode_id].chmod = chmod;
                    inode[dir[i].inode_id].inodemodify_time = DateTime.Now;
                    break;
                }
            }
            long adress = sup_block.offset_inodearray + dir[i].inode_id * Marshal.SizeOf<Inode>();
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                WriteStruct<Inode>(br, inode[dir[i].inode_id]);
            }
        }
        public void CreateShare(short s_id,short s_size)
        {
            if (memory.Capacity-1 < s_id)
            {
                for (int i = 0; i < inode.Length; i++)
                {
                    if (inode[i].blockadress_1 == 0)
                    {
                        SharedMemory mem = new SharedMemory();
                        mem.id = s_id;
                        mem.size = s_size;
                        mem.inode_id = i;
                        memory.Add(mem);
                        inode[i].file_type = 'f';
                        inode[i].inode_id = i;
                        inode[i].user_id = current_user;
                        inode[i].chmod = 44;
                        inode[i].file_size = 0;
                        for (int fat_index = 0; fat_index < FatTable.Length; fat_index++)
                            if (FatTable[fat_index] == 0)
                            {
                                FatTable[fat_index] = -1;
                                inode[i].blockadress_1 = fat_index;
                                sup_block.freeblocks_count--;
                                sup_block.freeinode_count--;
                                break;
                            }
                        inode[i].filecreate_time = DateTime.Now;
                        inode[i].inodemodify_time = DateTime.Now;
                        inode[i].filemodify_time = DateTime.Now;
                        FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        using (BinaryWriter br = new BinaryWriter(fsystem))
                        {
                            FileSystem.WriteStruct<SuperBlock>(br, sup_block);
                            fsystem.Seek(sup_block.offset_inodearray + inode[i].inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                            FileSystem.WriteStruct<Inode>(br, inode[i]);
                        }
                        break;
                    }
                    
                }
                
            }
        }
        public void InsertShare(short s_id, int item,int index)
        {
            long adress=sup_block.offset+sup_block.rootdir_size*inode[memory[s_id].inode_id].blockadress_1+ index;
            FileStream fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                br.Write(item);
                inode[memory[s_id].inode_id].file_size += Marshal.SizeOf<int>() ;
                inode[memory[s_id].inode_id].filemodify_time = DateTime.Now;
                inode[memory[s_id].inode_id].inodemodify_time = DateTime.Now;
                fsystem.Seek(sup_block.offset_inodearray + memory[s_id].inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                FileSystem.WriteStruct<Inode>(br, inode[memory[s_id].inode_id]);
            }
           /* Console.WriteLine("Записано"+item);
            Thread.Sleep(2000);*/
        }
        public int GetShare(short s_id,int index)
        {
            int item;
            long adress = sup_block.offset + sup_block.rootdir_size * inode[memory[s_id].inode_id].blockadress_1 + index;
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {          
                fsystem.Seek(adress, SeekOrigin.Begin);
                item=br.ReadInt32();
            }
            fsystem = new FileStream(system_path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (BinaryWriter br = new BinaryWriter(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                br.Write(0);
                inode[memory[s_id].inode_id].file_size -=  Marshal.SizeOf<int>();
                inode[memory[s_id].inode_id].filemodify_time = DateTime.Now;
                inode[memory[s_id].inode_id].inodemodify_time = DateTime.Now;
                fsystem.Seek(sup_block.offset_inodearray + memory[s_id].inode_id * Marshal.SizeOf<Inode>(), SeekOrigin.Begin);
                FileSystem.WriteStruct<Inode>(br, inode[memory[s_id].inode_id]);
            }
            /*Console.WriteLine("Удалено "+item);
            Thread.Sleep(2000);*/
            return item;
        }
        public void PrintShare(short s_id)
        {
            long adress = sup_block.offset + sup_block.rootdir_size * inode[memory[s_id].inode_id].blockadress_1 + inode[memory[s_id].inode_id].file_size - Marshal.SizeOf<int>();
            FileStream fsystem = new FileStream(system_path, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fsystem))
            {
                fsystem.Seek(adress, SeekOrigin.Begin);
                for(int i=0;i< inode[memory[s_id].inode_id].file_size;i+=Marshal.SizeOf<int>())
                Console.WriteLine(br.ReadInt32());
            }
        }
       

    }
}
