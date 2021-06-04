using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public static class SizeUtil
    {
        public static string Format(double size, string sufix = null)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (size >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                size = size / 1024;
            }
            return String.Format("{0:0.0} {1}{2}", size, sizes[order], sufix);
        }
    }
}
