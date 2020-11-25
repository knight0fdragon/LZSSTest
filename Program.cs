using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LZSSTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileToEncode = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "testdecode.chr")).ToList();
            var encodedTest = LZSS.Compress(fileToEncode.ToList());


            var fileToVerify = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "SUB_DISP.MAP")).ToList();
            var verifyTest = fileToVerify.GetRange(0xE600, 0x1093);

            var verified = true;
            if (encodedTest.Count != verifyTest.Count)
            {
                Console.WriteLine($"Encoded size({encodedTest.Count}) does not match verified size({verifyTest.Count}).");
                verified = false;
            }
            for (var i = 0; i < Math.Min(encodedTest.Count, verifyTest.Count); i++)
            {
                if (encodedTest[i] != verifyTest[i])
                {
                    Console.WriteLine($"[{i}]: Encoded byte({encodedTest[i]}) does not match verified byte({verifyTest[i]}).");
                    verified = false;
                    break;
                }
            }
            if(!verified)
            {
                Console.WriteLine("Error compressing");
            }
            else
            {
                Console.WriteLine("Compressing successful.");
            }
            verified = true;
            var decodedTest = LZSS.Decompress(encodedTest);
            if (decodedTest.Count != fileToEncode.Count)
            {
                Console.WriteLine($"Decoded size({encodedTest.Count}) does not match verified size({fileToEncode.Count}).");
                verified = false;
            }
            for (var i = 0; i < Math.Min(decodedTest.Count, fileToEncode.Count); i++)
            {
                if (decodedTest[i] != fileToEncode[i])
                {
                    Console.WriteLine($"[{i}]: Decoded byte({decodedTest[i]}) does not match verified byte({fileToEncode[i]}).");
                    verified = false;
                   // break;
                }
            }
            if (!verified)
            {
                Console.WriteLine("Error decompressing.");
            }
            else
            {
                Console.WriteLine("Decompressing successful.");

            }

            Console.ReadKey();
        }
    }
}
