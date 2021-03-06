﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace BeetleX.FastHttpApi.StaticResurce
{
    class FileResource
    {
        public FileResource(string filename, string urlname)
        {
            FullName = filename;
            UrlName = urlname;
        }

        public string UrlName { get; set; }

        public string UrlMD5 { get; set; }

        public long CreateTime { get; set; }

        public string FileMD5 { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public int Length { get; set; }

        public byte[] Data { get; set; }

        public virtual bool GZIP { get { return true; } }

        public virtual bool Cached => false;

        public virtual ArraySegment<byte> GetBlodk(int offset, int size, out int newOffset)
        {
            int length = Data.Length - offset;
            if (length >= size)
                length = size;
            newOffset = offset + length;
            return new ArraySegment<byte>(Data, offset, length);
        }

        public virtual void Recovery(byte[] buffer)
        {

        }

        protected virtual void LoadFile()
        {
            int length;
            using (FileStream fstream = System.IO.File.OpenRead(FullName))
            {
                length = (int)fstream.Length;
                byte[] buffer = HttpParse.GetByteBuffer();
                using (System.IO.MemoryStream memory = new MemoryStream())
                {
                    using (GZipStream gstream = new GZipStream(memory, CompressionMode.Compress))
                    {
                        while (length > 0)
                        {
                            int len = fstream.Read(buffer, 0, buffer.Length);
                            length -= len;
                            gstream.Write(buffer, 0, len);
                            gstream.Flush();
                            if (length == 0)
                                Data = memory.ToArray();
                        }
                    }
                }
            }
            Length = Data.Length;
        }

        public void Load()
        {
            Name = System.IO.Path.GetFileName(FullName);
            UrlMD5 = MD5Encrypt(UrlName);
            FileMD5 = FMD5(FullName);
            FileInfo fi = new FileInfo(FullName);
            Length = (int)fi.Length;
            LoadFile();
        }

        public string MD5Encrypt(string filename)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(filename));

                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static string FMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }

    class ImageResource : FileResource
    {
        public ImageResource(string filename, string urlname) : base(filename, urlname) { }

        protected override void LoadFile()
        {

        }

        public override bool GZIP => false;

        public override bool Cached => true;

        private System.Collections.Concurrent.ConcurrentQueue<byte[]> mPool = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();

        public override ArraySegment<byte> GetBlodk(int offset, int size, out int newOffset)
        {
            int length = Length - offset;
            if (length >= size)
                length = size;
            newOffset = offset + length;
            byte[] buffer = null;
            if (!mPool.TryDequeue(out buffer))
            {
                buffer = new byte[size];
            }
            using (System.IO.FileStream fs = System.IO.File.OpenRead(FullName))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                int len = fs.Read(buffer, 0, buffer.Length);
                return new ArraySegment<byte>(buffer, 0, len);
            }
        }

        public override void Recovery(byte[] buffer)
        {
            mPool.Enqueue(buffer);

        }
    }
}
