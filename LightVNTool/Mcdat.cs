using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightVNTool
{
    public class Mcdat
    {
        //d6c5fKI3GgBWpZF3Tz6ia3kF0
        private static readonly byte[] KEY = { 0x64, 0x36, 0x63, 0x35, 0x66, 0x4B, 0x49, 0x33,
                                               0x47, 0x67, 0x42, 0x57, 0x70, 0x5A, 0x46, 0x33,
                                               0x54, 0x7A, 0x36, 0x69, 0x61, 0x33, 0x6B, 0x46, 0x30 };
        private Dictionary<string, string> FileNameList = new Dictionary<string, string>();

        public void Unpack(string indir)
        {
            bool IfRecoverName = true;

            var outDir = Path.Combine(indir, "output");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            if (!Directory.Exists(indir))
            {
                Console.WriteLine($"Not Found {indir}");
                return;
            }

            string NameListPath = Path.Combine(indir, "0.mcdat");
            if (File.Exists(NameListPath))
            {
                var decNameList = XorZeroMcdat(File.ReadAllBytes(NameListPath));
                File.WriteAllBytes(Path.Combine(outDir, "0.mcdat.json"), decNameList);
                FileNameList = ParseJson(decNameList);
            }
            else
            {
                IfRecoverName = false;
                Console.WriteLine($"Failed to read 0.mcdat. FileName will not recover");
            }

            var mcdatFiles = Directory.GetFiles(indir, "*.mcdat");
            int Count = 1;

            foreach (var mc in mcdatFiles)
            {
                var name = Path.GetFileName(mc);
                string outPath;

                var relativePath = FileNameList.FirstOrDefault(f => f.Value == name).Key;
                if (IfRecoverName && relativePath != null)
                {
                    outPath = Path.Combine(outDir, relativePath);
                }
                else
                {
                    outPath = Path.Combine(outDir, name);
                }

                Console.WriteLine($"[{Count}/{mcdatFiles.Length}] Processing {name}");
                Count++;

                if (name.Equals("0.mcdat"))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                byte[] decFileData = XorMcdat(File.ReadAllBytes(mc));
                File.WriteAllBytes(outPath, decFileData);

            }
        }

        private byte[] XorZeroMcdat(byte[] encdata)
        {
            var buffer = new byte[encdata.Length + 1];
            Array.Copy(encdata, buffer, encdata.Length);

            int idx_j = encdata.Length;
            int idx_i = 0;

            for (int i = 0; i < encdata.Length; i++)
            {
                byte stream = KEY[i % KEY.Length];
                buffer[idx_i] ^= stream;
                buffer[idx_j] ^= stream;
                idx_j--; idx_i++;
            }
            var decdata = new byte[encdata.Length];
            Array.Copy(buffer, decdata, encdata.Length);
            return decdata;
        }

        private byte[] XorMcdat(byte[] buffer)
        {
            byte[] reversedKey = KEY.Reverse().ToArray();

            if (buffer.Length < 100)
            {
                if (buffer.Length > 0)
                    return XorZeroMcdat(buffer);
                return buffer;
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    buffer[i] ^= KEY[i % KEY.Length];
                }

                int index = buffer.Length - 99;
                for (int i = 0; i < 99; i++)
                {
                    buffer[i + index] ^= reversedKey[i % reversedKey.Length];
                }
            }

            return buffer;
        }

        private Dictionary<string, string> ParseJson(byte[] data)
        {

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var fileMap = JsonSerializer.Deserialize<Dictionary<string, string>>(data, options);
            var result = new Dictionary<string, string>();
            if (fileMap == null)
            {
                throw new InvalidDataException("Json Parse Failed");
            }
            foreach (var item in fileMap)
            {
                string encFileName = Path.GetFileName(item.Value);
                result[item.Key] = encFileName;
            }
            return result;
        }

    }
}
