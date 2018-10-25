using PakLib;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PakExplorer.Models
{
    public sealed class VirtualFileSystem
    {
        public static VirtualFileSystem CreateGraph(PakEntryTable entries)
        {
            var graph = new VirtualFileSystem();

            foreach (PakEntryMetadata entry in entries)
            {
                var currentFolder = graph.Root;
                string[] segments = entry.FileName.Split('/');
                var path = new StringBuilder();
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    path.Append("/" + segments[i]);
                    if (!currentFolder.Subfolders.ContainsKey(segments[i]))
                    {
                        currentFolder.Subfolders.Add(segments[i], new VirtualFolder(path.ToString()));
                    }
                    currentFolder = currentFolder.Subfolders[segments[i]];
                }
                currentFolder.Files.Add(segments[segments.Length - 1], entry);
            }

            return graph;
        }

        private VirtualFileSystem()
        {
            Root = new VirtualFolder("");
        }

        public VirtualFolder Root { get; }

        public VirtualFolder GetFolder(string path)
        {
            VirtualFolder currentFolder = Root;

            string[] segments = path.Split('/');
            for (int i = 1; i < segments.Length; i++)
            {
                currentFolder = currentFolder.Subfolders[segments[i]];
            }

            return currentFolder;
        }
    }

    public sealed class VirtualFolder
    {
        public VirtualFolder(string path)
        {
            Subfolders = new Dictionary<string, VirtualFolder>();
            Files = new Dictionary<string, PakEntryMetadata>();
            Path = path;
        }

        public string Path { get; }
        public IDictionary<string, VirtualFolder> Subfolders { get; }
        public IDictionary<string, PakEntryMetadata> Files { get; }
    }
}
