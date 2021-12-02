using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //put our banana shell code here
            byte[] buf = new byte[5] { 0xfc, 0x48, 0x83, 0xe4, 0xf0 };

            // encode with substitution key 2
            // bitwise AND operation with 0xFF to keep the modified value within the 0 - 255 range(single byte) in case the increased byte value exceeds 0xFF
            byte[] encoded = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                encoded[i] = (byte)(((uint)buf[i] + 2) & 0xFF);
            }
            //converting the byte array into a string with the StringBuilder, print out to screen, so we can copy and paste
            StringBuilder hex = new StringBuilder(encoded.Length * 2);
            foreach (byte b in encoded)
            {
                hex.AppendFormat("0x{0:x2}, ", b);
            }
            Console.WriteLine("The payload is: " + hex.ToString());
        }
    }
}
