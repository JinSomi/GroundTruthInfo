using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CheckPreformance
{
    public class DllImport
    {
        public static class CvtImageDLLImport
        {
            [DllImport("CvtImage.dll")]
            public static extern int Demosaic(IntPtr dst, IntPtr src, int width, int height, string BayerPatternType);
            [DllImport("CvtImage.dll")]
            public static extern int Intensity(IntPtr ptrDst, IntPtr ptrSrc, int srcSize, double illumin, int numCore = 0);
            [DllImport("CvtImage.dll")]
            public static extern int SetWB(IntPtr p, int srcWidth, int srcHeight, string type, double gainB, double gainG, double gainR, int numCore = 0);
            [DllImport("CvtImage.dll", EntryPoint = "SetWB_")]
            public static extern int SetWB(IntPtr ptrSrc, IntPtr ptrDst, int srcWidth, int srcHeight, string type, double gainB, double gainG, double gainR, int numCore = 0);
            [DllImport("CvtImage.dll")]
            public static extern int Summation(IntPtr ptrDst, IntPtr ptrSrc1, IntPtr ptrSrc2, int srcSize, int numCore = 0);
            [DllImport("CvtImage.dll", EntryPoint = "Summation_")]
            public static extern int Summation(IntPtr ptrDst, IntPtr ptrSrc1, IntPtr ptrSrc2, IntPtr ptrSrc3, IntPtr ptrSrc4, int srcSize, int numCore = 0);
           
        }
    }
}
