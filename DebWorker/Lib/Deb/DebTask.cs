﻿using DebWorker.Lib.Archive;
using DebWorker.Lib.Models;
using DebWorker.Lib.Tar;
using DebWorker.XZNET;
using Microsoft.Build.Framework;

namespace DebWorker.Lib.Deb
{
    public class DebTask
    {
        public string PublishDir { get; set; }
        public string DebPath { get; set; }
        public string DebTarPath { get; set; }
        public string DebTarXzPath { get; set; }
        public string Prefix { get; set; }
        public string Version { get; set; }
        public string PackageName { get; set; }
        public ITaskItem[] Content { get; set; }
        public string Maintainer { get; set; }
        public string Description { get; set; }
        public string DebPackageArchitecture { get; set; }

        public bool Execute()
        {
            using (var targetStream = File.Open(DebPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var tarStream = File.Open(DebTarPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                ArchiveBuilder archiveBuilder = new ArchiveBuilder();

                var archiveEntries = archiveBuilder.FromDirectory(
                    PublishDir,
                    Prefix,
                    Content);

                EnsureDirectories(archiveEntries);

                archiveEntries = archiveEntries
                    .OrderBy(e => e.TargetPathWithFinalSlash, StringComparer.Ordinal)
                    .ToList();

                TarFileCreator.FromArchiveEntries(archiveEntries, tarStream);
                tarStream.Position = 0;

                using (var tarXzStream = File.Open(DebTarXzPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var xzStream = new XZOutputStream(tarXzStream))
                {
                    tarStream.CopyTo(xzStream);
                }

                using (var tarXzStream = File.Open(DebTarXzPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var pkg = DebPackageCreator.BuildDebPackage(
                        archiveEntries,
                        PackageName,
                        Description,
                        Maintainer,
                        Version,
                        DebPackageArchitecture,
                        null);

                    DebPackageCreator.WriteDebPackage(
                        archiveEntries,
                        tarXzStream,
                        targetStream,
                        pkg);
                }

                return true;
            }
        }

        internal static void EnsureDirectories(List<ArchiveEntry> entries, bool includeRoot = true)
        {
            var dirs = new HashSet<string>(entries.Where(x => x.Mode.HasFlag(LinuxFileMode.S_IFDIR))
                .Select(d => d.TargetPathWithoutFinalSlash));

            var toAdd = new List<ArchiveEntry>();

            string GetDirPath(string path)
            {
                path = path.TrimEnd('/');
                if (path == string.Empty)
                {
                    return "/";
                }

                if (!path.Contains("/"))
                {
                    return string.Empty;
                }

                return path.Substring(0, path.LastIndexOf('/'));
            }

            void EnsureDir(string dirPath)
            {
                if (dirPath == string.Empty || dirPath == ".")
                {
                    return;
                }

                if (!dirs.Contains(dirPath))
                {
                    if (dirPath != "/")
                    {
                        EnsureDir(GetDirPath(dirPath));
                    }

                    dirs.Add(dirPath);
                    toAdd.Add(new ArchiveEntry()
                    {
                        Mode = LinuxFileMode.S_IXOTH | LinuxFileMode.S_IROTH | LinuxFileMode.S_IXGRP |
                               LinuxFileMode.S_IRGRP | LinuxFileMode.S_IXUSR | LinuxFileMode.S_IWUSR |
                               LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFDIR,
                        Modified = DateTime.Now,
                        Group = "root",
                        Owner = "root",
                        TargetPath = dirPath,
                        LinkTo = string.Empty,
                    });
                }
            }

            foreach (var entry in entries)
            {
                EnsureDir(GetDirPath(entry.TargetPathWithFinalSlash));
            }

            if (includeRoot)
            {
                EnsureDir("/");
            }

            entries.AddRange(toAdd);
        }
    }
}