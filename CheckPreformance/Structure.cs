using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CheckPreformance
{
    public class Structure
    {
        public struct Defect_struct
        {

            public int cenx;
            public int ceny;
            public int width;
            public int height;
            public float angle;


            /// GroundTruth Feature
            public int blob_ind { get; set; }
            /// <summary>
            /// 1= true , -1 =fasle, 2 = under
            /// </summary>
            public int GroundTruth { get; set; }
            public int Blobs_num;
            public int In;
            public int Out;
            public int Defect_Location;
            public int Pixel_num;
            public double Average_Distance;
            public double Centroid_Distance;
            public double Long_Axis_len;
            public double Short_Axis_len;
            public double Long_Axis_angle;
            public double Short_Axis_angle;
            public double Avg_R;
            public double Avg_G;
            public double Avg_B;
            public double Avg_I;
            public Defect_Classification Name;
            public bool UnderDefect;

        }

        public enum Defect_Classification
        {
            Bump = 1,
            Chipping,
            CoatingOff,
            Stain,
            Color,
            Crack,
            Scratch,
            Dust,
            Texture,
            Ect
        }

        public class ResultInfo
        {
            public FileInfo datName { get; set; }
            public string ImageName_bf { get; set; }
            public string ImageName_bf_defect { get; set; }
            public string ImageName_df_defect { get; set; }
            public string ImageName_co_defect { get; set; }
            public string ImageName_bl_defect { get; set; }
            public string ImageName_df { get; set; }
            public string ImageName_co { get; set; }
            public string ImageName_bl { get; set; }
            public int Detect_num { get; set; }
            public int Under_num { get; set; }
            public int Over_Dust_num { get; set; }
            public int Over_Defect_num { get; set; }
            public int Over_Bump_num { get; set; }
            public int Over_Ect_num { get; set; }
            public List<Defect_struct> Defects { get; set; }

            public FileInfo CH0 { get; set; }
            public FileInfo CH1 { get; set; }
            public FileInfo CH2 { get; set; }
            public FileInfo CH3 { get; set; }

            public List<Defect_struct> True_Defects { get; set; }
            public List<Defect_struct> False_Defects { get; set; }
            public List<Defect_struct> Under_Defects { get; set; }

            public List<Defect_struct> BF_Defects { get; set; }
            public List<Defect_struct> DF_Defects { get; set; }
            public List<Defect_struct> CO_Defects { get; set; }
            public List<Defect_struct> BL_Defects { get; set; }

            public List<Defect_struct> True_BF_Defects { get; set; }
            public List<Defect_struct> True_DF_Defects { get; set; }
            public List<Defect_struct> True_CO_Defects { get; set; }
            public List<Defect_struct> True_BL_Defects { get; set; }

            public List<Defect_struct> False_BF_Defects { get; set; }
            public List<Defect_struct> False_DF_Defects { get; set; }
            public List<Defect_struct> False_CO_Defects { get; set; }
            public List<Defect_struct> False_BL_Defects { get; set; }




            public List<Defect_struct> Real_BF_Defects { get; set; }
            public List<Defect_struct> Real_DF_Defects { get; set; }
            public List<Defect_struct> Real_CO_Defects { get; set; }
            public List<Defect_struct> Real_BL_Defects { get; set; }

            public Defect_Classification Name;

        }

    }
}
