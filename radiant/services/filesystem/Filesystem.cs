﻿using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.Listing;
using Cosmos.System.FileSystem.VFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace radiant.services.filesystem
{
    public class Filesystem
    {
        static CosmosVFS fs;

        public static void Init()
        {
            fs = new CosmosVFS();
            VFSManager.RegisterVFS(fs);
        }

        public static List<DirectoryEntry> GetListing(string dir)
        {
            var directory_list = fs.GetDirectoryListing(dir);
            return directory_list.Where(f => !f.mName.EndsWith(".private")).ToList();
        }

        public static string ReadFile(string path)
        {
            if (path.EndsWith(".private"))
                return "";
            if (Path.IsPathRooted(path))
                return File.ReadAllText(path);
            else
                return File.ReadAllText(Path.Join(Kernel.PWD, path));
        }

        public static void CreateFile(string path)
        {
            if (path.EndsWith(".private"))
            {
                Console.WriteLine("Access denied");
                return;
            }
            if (Path.IsPathRooted(path))
                File.Create(path);
            else
                File.Create(Path.Join(Kernel.PWD, path));
        }

        public static void WriteFile(string path, string contents)
        {
            if (path.EndsWith(".private"))
            {
                Console.WriteLine("Access denied");
                return;
            }
            if (Path.IsPathRooted(path))
                File.WriteAllText(path, contents);
            else
                File.WriteAllText(Path.Join(Kernel.PWD, path), contents);
        }

        public static void CreateNecessarySystemFiles()
        {
            string directoryPath = @"0:\radiant";
            string usersPath = @"0:\radiant\users";
            string configFilePath = @"0:\radiant\config.cfg";

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!Directory.Exists(usersPath))
                Directory.CreateDirectory(usersPath);

            if (!File.Exists(configFilePath))
            {
                Byte[] defconfig = new UTF8Encoding(true).GetBytes("PCName = radiant");
                using (FileStream fs = File.Create(configFilePath))
                {
                    fs.Write(defconfig, 0, defconfig.Length);
                }
            }
        }
    }
}
