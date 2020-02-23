using System;
using System.IO;

namespace AigisCutter.Model
{
    public class TimeStamp
    {
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }

        public static TimeStamp GetTimeStamp(string filePath)
        {
            var inst = new TimeStamp();

            inst.CreationTime = File.GetCreationTime(filePath);
            inst.LastWriteTime = File.GetLastWriteTime(filePath);
            inst.LastAccessTime = File.GetLastAccessTime(filePath);

            return inst;
        }
    }
}