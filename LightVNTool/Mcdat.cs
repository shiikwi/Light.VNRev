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

                if (IfRecoverName && FileNameList.TryGetValue(name, out var relativePath))
                {
                    outPath = Path.Combine(outDir, relativePath);
                }
                else
                {
                    outPath = Path.Combine(outDir, name);
                }

                if (!name.Equals("0.mcdat"))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    byte[] decFileData = XorMcdat(File.ReadAllBytes(mc));
                    File.WriteAllBytes(outPath, decFileData);
                }
                Console.WriteLine($"[{Count}/{mcdatFiles.Length}] Unpack {name}");
                Count++;
            }
        }

        public void Repack(string inDir)
        {
            bool ProcessName = true;

            var outDir = Path.Combine(Directory.GetParent(inDir)?.FullName ?? inDir, "Newmcdat");
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string NameListPath = Path.Combine(inDir, "0.mcdat.json");
            if (File.Exists(NameListPath))
            {
                var jsonData = File.ReadAllBytes(NameListPath);
                var filemap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                FileNameList = filemap.ToDictionary(kvp => kvp.Key, kvp => Path.GetFileName(kvp.Value));
                var encData = XorZeroMcdat(jsonData);
                File.WriteAllBytes(Path.Combine(outDir, "0.mcdat"), encData);
            }
            else
            {
                ProcessName = false;
            }

            int count = 1;
            var filesList = Directory.GetFiles(inDir, "*", SearchOption.AllDirectories)
                                     .Where(f => !Path.GetFileName(f).Equals("0.mcdat.json", StringComparison.OrdinalIgnoreCase));

            foreach (var filepath in filesList)
            {
                string relativePath = Path.GetRelativePath(inDir, filepath).Replace('\\', '/').ToLower();
                string FileName;

                if (ProcessName && FileNameList.TryGetValue(relativePath, out var name))
                {
                    FileName = name;
                }
                else
                {
                    FileName = Path.GetFileName(filepath);
                }

                var data = File.ReadAllBytes(filepath);
                var encdata = XorMcdat(data);
                if (FileName.Equals("0.mcdat")) encdata = XorZeroMcdat(data);
                File.WriteAllBytes(Path.Combine(outDir, FileName), encdata);

                Console.WriteLine($"[{count}/{filesList.Count()}] Repack {FileName}");
                count++;

            }
        }

        public void MakePatch(string inDir)
        {
            var outDir = Path.Combine(Directory.GetParent(inDir)?.FullName ?? inDir, "Patch");
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string NameListPath = Path.Combine(inDir, "0.mcdat.json");
            if (File.Exists(NameListPath))
            {
                var jsonData = File.ReadAllBytes(NameListPath);
                var filemap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                FileNameList = filemap.ToDictionary(kvp => kvp.Key, kvp => Path.GetFileName(kvp.Value));
            }
            else
            {
                Console.WriteLine($"Cannot Read 0.mcdat.json, Make Patch Failed");
                return;
            }

            int count = 0;
            Dictionary<string, string> PatchNameList = new Dictionary<string, string>();
            var filesList = Directory.GetFiles(inDir, "*", SearchOption.AllDirectories)
                         .Where(f => !Path.GetFileName(f).Equals("0.mcdat.json", StringComparison.OrdinalIgnoreCase));

            foreach (var filepath in filesList)
            {
                string relativePath = Path.GetRelativePath(inDir, filepath).Replace('\\', '/').ToLower();
                string FileName;
                if (FileNameList.TryGetValue(relativePath, out var name))
                {
                    FileName = name;
                }
                else
                {
                    FileName = $"Patch{count}.mcdat";
                }

                PatchNameList[relativePath] = "Patch/" + FileName;
                var data = File.ReadAllBytes(filepath);
                var encdata = XorMcdat(data);
                File.WriteAllBytes(Path.Combine(outDir, FileName), encdata);
                Console.WriteLine($"Patch {relativePath}");
                count++;
            }

            var PatchJson = JsonSerializer.SerializeToUtf8Bytes(PatchNameList, new JsonSerializerOptions { WriteIndented = true });
            var encPatchData = XorZeroMcdat(PatchJson);
            File.WriteAllBytes(Path.Combine(outDir, "0.mcdat"), encPatchData);
            Console.WriteLine($"Make Patch {count} files");
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
                result[encFileName] = item.Key;
            }
            return result;
        }

    }
}
