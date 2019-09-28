﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EliteFiles.Tests.Internal
{
    internal sealed class TestFolder : IDisposable
    {
        private readonly DirectoryInfo _di;

        public TestFolder()
            : this(null)
        {
        }

        public TestFolder(string templatePath)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _di = Directory.CreateDirectory(path);

            if (templatePath != null)
            {
                var srcDir = new DirectoryInfo(templatePath);
                Copy(srcDir, _di);
            }
        }

        public string Name => _di.FullName;

        public string Resolve(string relativePath)
        {
            return Path.Combine(Name, relativePath);
        }

        public string ReadText(string path)
        {
            return File.ReadAllText(Resolve(path));
        }

        public void WriteText(string path, string contents, bool append = false)
        {
            using (var sw = new StreamWriter(Resolve(path), append))
            {
                sw.Write(contents);
            }
        }

        public void DeleteFile(string path)
        {
            File.Delete(Resolve(path));
        }

        public void DeleteFolder(string path)
        {
            Directory.Delete(Resolve(path), true);
        }

        public void Dispose()
        {
            _di?.Delete(true);
        }

        private static void Copy(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var subdir in source.EnumerateDirectories())
            {
                Copy(subdir, target.CreateSubdirectory(subdir.Name));
            }

            foreach (var file in source.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name));
            }
        }
    }
}
