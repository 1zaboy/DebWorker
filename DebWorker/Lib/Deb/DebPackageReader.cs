using DebWorker.Lib.Ar;
using DebWorker.Lib.Common;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DebWorker.Lib.Deb
{
    public static class DebPackageReader
    {
        public static void unTAR(Stream streamTar, string directoryPath)
        {
            var reader = ReaderFactory.Open(streamTar);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    ExtractionOptions opt = new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    };
                    Directory.CreateDirectory(directoryPath);
                    reader.WriteEntryToDirectory(directoryPath, opt);
                }
            }
        }

        public static Stream GetPayloadStream(Stream stream)
        {
            using (ArFile archive = new ArFile(stream, leaveOpen: true))
            {
                while (archive.Read())
                {
                    if (archive.FileName.StartsWith("data.tar."))
                    {
                        var ext = Path.GetExtension(archive.FileName);
                        if (ext == ".gz")
                        {
                            return new GZipDecompressor(archive.Open(), false);
                        }

                        if (ext == ".xz")
                        {
                            var payload = new MemoryStream();
                            using (var xz = new XZNET.XZInputStream(archive.Open()))
                            {
                                xz.CopyTo(payload);
                                payload.Seek(0, SeekOrigin.Begin);
                                return payload;
                            }
                        }

                        throw new InvalidDataException("Don't know how to decompress " + archive.FileName);
                    }
                }

                throw new InvalidDataException("data.tar.?? not found");
            }
        }
    }
}
