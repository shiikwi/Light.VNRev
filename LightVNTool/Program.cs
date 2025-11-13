using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightVNTool
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: LightVNTool.exe -u <Unpack mcdat files folder>");
                    Console.WriteLine("LightVNTool.exe -p <Repack output folder>");
                    return;
                }

                string mode = args[0];
                string inDir = args[1];

                var mcdat = new Mcdat();
                if (mode == "-u")
                {
                    mcdat.Unpack(inDir);
                }
                else if (mode == "-p")
                {
                    mcdat.Repack(inDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }

}
