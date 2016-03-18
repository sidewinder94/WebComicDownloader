using System;
using System.IO;

namespace WebComicToEbook.EmbeddedResources
{
    public static class ResourcesUtils
    {
        public static Stream AsStream(this byte[] resource)
        {
            return new MemoryStream(resource);
        }

        public static String AsString(this byte[] resource)
        {
            using (var sr = new StreamReader(resource.AsStream()))
            {
                return sr.ReadToEnd();
            }
        }
    }
}