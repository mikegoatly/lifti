using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace PerformanceProfiling
{
    public static class WikipediaDataLoader
    {
        public static IList<(string name, string text)> Load(Type resourceRelativeType)
        {
            using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceRelativeType, "WikipediaPages.dat"))
            {
                using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    using (var reader = new BinaryReader(zipStream))
                    {
                        var wikipediaData = new List<(string name, string text)>();
                        for (var i = 0; i < 200; i++)
                        {
                            var fileName = reader.ReadString();
                            var contents = reader.ReadString();

                            wikipediaData.Add(("Originally downloaded from Wikipedia - http://en.wikipedia.com/wiki/pages/" + fileName, contents));
                        }

                        return wikipediaData;
                    }
                }
            }
        }
    }
}
