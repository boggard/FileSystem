using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystem
{
    class Process
    {
            public int id;
            public int pri;
            public int uid;
            public char stat;
            public char[] name;
            public Thread proc;
    }
}
