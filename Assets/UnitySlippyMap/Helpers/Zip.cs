using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;



namespace UnitySlippyMap
{

    /// <summary>
    /// Added to handle decompression of gzipped strings in MbTiles Utf grid data. 
    /// </summary>
    public class Zip
    {
        public static byte[] DecompressGzipBytes(byte[] Bytes)
        {

            ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream stream =
                new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(new MemoryStream(Bytes));
           
            MemoryStream memory = new MemoryStream();

            byte[] writeData = new byte[4096];
            int size;

            while (true)
            {
                size = stream.Read(writeData, 0, writeData.Length);
                if (size > 0)
                {
                    memory.Write(writeData, 0, size);
                }
                else break;
            }

            stream.Close();

            return memory.ToArray();

        }

        public static string DecompressGzipJsonBytes(byte[] bytes)
        {

            char[] _chars = Encoding.UTF8.GetChars(Zip.DecompressGzipBytes(bytes));
            
            return new string(_chars);

        }

        public static byte[] CompressJsonString(string text)
        {
            if (text == null)
                return null;

            using (Stream memOutput = new MemoryStream())
            {
                using (GZipOutputStream zipOut = new GZipOutputStream(memOutput))
                {
                    using (StreamWriter writer = new StreamWriter(zipOut))
                    {
                        writer.Write(text);

                        writer.Flush();
                        zipOut.Finish();

                        byte[] bytes = new byte[memOutput.Length];
                        memOutput.Seek(0, SeekOrigin.Begin);
                        memOutput.Read(bytes, 0, bytes.Length);

                        return bytes;
                    }
                }
            }
        }

        public static string DecompressJsonString(byte[] bytes)
        {
            if (bytes == null)
                return null;

            using (Stream memInput = new MemoryStream(bytes))
            using (GZipInputStream zipInput = new GZipInputStream(memInput))
            using (StreamReader reader = new StreamReader(zipInput))
            {
                string text = reader.ReadToEnd();

                return text;
            }
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);

        }
    }

}
