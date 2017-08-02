using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SideWaffleTasks
{
    public class AddToZipTask : Task {
        public string ZipFilePath { get; set; }
        public ITaskItem[] FilesToAdd { get; set; }
        public string RootFolder { get; set; }

        public string Compression { get; set; } = System.IO.Compression.CompressionLevel.Optimal.ToString();

        public override bool Execute() {
            Log.LogMessage(MessageImportance.High, "AddToZipTask2 called", null);

            CompressionLevel level = CompressionLevel.Optimal;
            if (!string.IsNullOrWhiteSpace(Compression)) {
                try {
                    CompressionLevel result = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), Compression);
                    level = result;
                }
                catch (Exception ex) {
                    Log.LogWarning("Unable to parse compression level [{0}]. Error [{1}]", Compression, ex.ToString());
                }
            }

            string parentDir = Path.GetDirectoryName(ZipFilePath);
            if (!Directory.Exists(parentDir)) {
                Directory.CreateDirectory(parentDir);
            }
            // System.Diagnostics.Debugger.Launch();
            using (ZipArchive zip = ZipFile.Open(ZipFilePath, System.IO.Compression.ZipArchiveMode.Update)) {
                Uri rootFolderUri = new Uri(RootFolder);
                // add each input file to the zip
                foreach (var file in FilesToAdd) {
                    // get the relative path of the file to to add
                    string filePath = file.GetMetadata("FullPath");
                    Uri fileUri = new Uri(filePath);
                    string relpath = Uri.UnescapeDataString(
                                            rootFolderUri.MakeRelativeUri(fileUri)
                                                .ToString()
                                                .Replace('/', Path.DirectorySeparatorChar)
                                            );

                    Log.LogMessage("Update zip: [{0}], add file: [{1}], relpath: [{2}]", ZipFilePath, filePath, relpath);

                    var entriesToDelete = new List<ZipArchiveEntry>();
                    // if the file is already in the zip remove it and add again
                    if (zip.Entries != null) {
                        foreach (var entry in zip.Entries) {
                            if (entry.FullName.Equals(relpath, StringComparison.OrdinalIgnoreCase)) {
                                entriesToDelete.Add(entry);
                            }
                        }
                    }
                    foreach(var entry in entriesToDelete) {
                        entry.Delete();
                    }

                    ZipFileExtensions.CreateEntryFromFile(zip, filePath, relpath, level);
                }
            }
            return true;
        }

        private CompressionLevel GetCompressionLevel() {
            CompressionLevel level = CompressionLevel.Optimal;
            try {
                CompressionLevel result = (CompressionLevel) Enum.Parse(typeof(CompressionLevel), Compression);
                level = result;
            }
            catch(Exception ex) {
                Log.LogWarning("Unable to parse compression level [{0}]. Error [{1}]", Compression, ex.ToString());
            }
            return level;
        }
    }
}
