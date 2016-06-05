using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystem
{
    class Semaphore
    {
        public short value;
        public short max_value;
        public Semaphore(short val,short max)
        {
            value = val;
            max_value = max;
        }
        public bool down()
        {
            Object obj = new Object();
            lock(obj)
            {
                if (value > 0)
                {
                    value--;
                    return true;
                }
                else
                    return false;
            }
        }
        public bool up()
        {
            Object obj = new Object();
            lock (obj)
            {
                if (value < max_value)
                {
                    value++;
                    return true;
                }
                else
                    return false;
            }
        }
    }
}
