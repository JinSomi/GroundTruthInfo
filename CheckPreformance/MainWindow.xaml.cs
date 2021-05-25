using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using HalconDotNet;
using GLB;
using System.Drawing;
using System.Drawing.Imaging;

 using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using System.Windows.Media;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CheckPreformance
{
    public struct Defect_struct
    {

        public int cenx;
        public int ceny;
        public int width;
        public int height;
        public float angle;

    }

    public class ResultInfo
    {
        public FileInfo datName { get; set; }
        public string ImageName_bf { get; set; }
        public string ImageName_bf_defect { get; set; }
        public string ImageName_df_defect { get; set; }
        public string ImageName_co_defect { get; set; }
        public string ImageName_df { get; set; }
        public string ImageName_co { get; set; }
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

        public List<Defect_struct> Real_Defects { get; set; }

        public List<Defect_struct> BF_Defects { get; set; }
        public List<Defect_struct> DF_Defects { get; set; }
        public List<Defect_struct> CO_Defects { get; set; }


        public List<Defect_struct> Real_BF_Defects { get; set; }
        public List<Defect_struct> Real_DF_Defects { get; set; }
        public List<Defect_struct> Real_CO_Defects { get; set; }
    }

    
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        public string PathDF;
        public string PathBF;
        public string PathCO;

        public Bitmap p_BF;
        public Bitmap d_BF;
        public Bitmap p_DF;
        public Bitmap p_CO;
        public Bitmap d_DF;
        public Bitmap d_CO;


        public FileInfo[] datFiles;
        public List<ResultInfo> infos;
        public HObject DefectRegion;
        public HObject DefectRegion_BF;
        public HObject DefectRegion_DF;
        public HObject DefectRegion_CO;
        public HObject select_defectRgn;
        public int Current_i;
        public HObject BaseImage_BF;
        public HObject BaseImage_DF;
        public StreamWriter Writer;
        public string RootAdderss;
        public string SysRcpPath = null;
        public string ModelRcpPath = null;

        public MemMapName InspMode;

        public MainWindow()
        {
            InitializeComponent();
            Current_i = 0;
            HOperatorSet.GenEmptyObj(out select_defectRgn);
            HOperatorSet.GenEmptyObj(out DefectRegion);

            orginalWidth = this.Width;

            originalHeight = this.Height;

          
        }


        public bool SetFolder(string Address)
        {
            if (InspMode == MemMapName.TMIC)
            {
                if (Directory.Exists(Address + "\\MicroDefect"))
                {
                    datFiles = new DirectoryInfo(Address + "\\MicroDefect").GetFiles("*.dat");
                    infos = new List<ResultInfo>();
                    RootAdderss = Address;
                    foreach (FileInfo f in datFiles)
                    {
                        ResultInfo temp = new ResultInfo();
                        temp.datName = f;
                        string name_temp = f.Name.Split('.')[0];
                        string corner = name_temp.Substring(name_temp.Length - 1);
                        string name = name_temp.Substring(0, name_temp.Length - 1);
                        temp.CH0 = new FileInfo(string.Format("{0}\\RawMicro\\{1}Micro{2}Ch0.bmp", Address, name, corner));
                        temp.CH1 = new FileInfo(string.Format("{0}\\RawMicro\\{1}Micro{2}Ch1.bmp", Address, name, corner));
                        temp.CH2 = new FileInfo(string.Format("{0}\\RawMicro\\{1}Micro{2}Ch2.bmp", Address, name, corner));
                        temp.CH3 = new FileInfo(string.Format("{0}\\RawMicro\\{1}Micro{2}Ch3.bmp", Address, name, corner));

                        temp.ImageName_bf = string.Format("{0}BF{1}.bmp", name, corner);
                        temp.ImageName_df = string.Format("{0}DF{1}.bmp", name, corner);
                        temp.ImageName_co = string.Format("{0}CO{1}.bmp", name, corner);
                        temp.ImageName_bf_defect = string.Format("{0}BF{1}_Marking.bmp", name, corner);
                        temp.ImageName_df_defect = string.Format("{0}DF{1}_Marking.bmp", name, corner);
                        temp.ImageName_co_defect = string.Format("{0}CO{1}_Marking.bmp", name, corner);

                        GetDatResult_Mic(ref temp);
                        infos.Add(temp);
                    }

                    Writer = new StreamWriter(string.Format("{0}\\Perfomance.csv", Address), true, Encoding.Default);
                    Writer.WriteLine("이미지,Detect, UnderKill, OverKill, 먼지, 부착, 깨짐, 기타");
                    Current_i = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (Directory.Exists(Address + "\\MacroColor"))
                {
                    datFiles = new DirectoryInfo(Address + "\\MacroColor").GetFiles("*.dat");
                    infos = new List<ResultInfo>();
                    RootAdderss = Address;
                    foreach (FileInfo f in datFiles)
                    {
                        if (f.Name.Contains("Defect"))
                        {
                            ResultInfo temp = new ResultInfo();
                            temp.datName = f;
                            string name_temp = f.Name.Split('.')[0];
                            //string corner = name_temp.Substring(name_temp.Length - 1);
                            string name = name_temp.Replace("Defect","");
                            temp.CH0 = new FileInfo(string.Format("{0}\\RawMacro\\{1}Ch0.bmp", Address, name));
                            temp.CH1 = new FileInfo(string.Format("{0}\\RawMacro\\{1}Ch1.bmp", Address, name));
                            temp.CH2 = new FileInfo(string.Format("{0}\\RawMacro\\{1}Ch2.bmp", Address, name));
                            temp.CH3 = new FileInfo(string.Format("{0}\\RawMacro\\{1}Ch3.bmp", Address, name));

                            temp.ImageName_bf = string.Format("{0}COLOR.bmp", name);
                            temp.ImageName_df = string.Format("{0}DF.bmp", name);
                            temp.ImageName_co = string.Format("{0}AREA.bmp", name);
                            temp.ImageName_bf_defect = string.Format("{0}COLOR_Marking.bmp", name);
                            GetDatResult_Mac(ref temp);
                            infos.Add(temp);
                        }

                        //Writer = new StreamWriter(string.Format("{0}\\Perfomance.csv", Address), true, Encoding.Default);
                        //Writer.WriteLine("이미지,Detect, UnderKill, OverKill, 먼지, 부착, 깨짐, 기타");
                        Current_i = 0;
                    }
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        private void Load_Btn_Click(object sender, RoutedEventArgs e)
        {
            bool load_ok = SetFolder(txt_folderPath.Text);

            if (!load_ok)
            {
                MessageBox.Show("폴더다시 설정");
            }
            else
            {
                try
                {
                    ImageName_lbl.Content = infos[Current_i].datName.Name;
                    AddList(infos[Current_i]);
                    if (InspMode == MemMapName.TMIC)
                    {
                        Convert_Image_Mic(infos[Current_i].CH0, infos[Current_i].CH1, infos[Current_i].CH2, infos[Current_i].CH3);
                    }
                    else
                    {
                        Convert_Image_Mac(infos[Current_i].CH0, infos[Current_i].CH1, infos[Current_i].CH2, infos[Current_i].CH3);
                    }
                    DrawDefects(infos[Current_i]);
                   
                    UpdateWindow(0);
                }
                catch
                {
                    //Writer.Close();
                }
                finally
                {
                    
                }
            }

        }

        private void AddList(ResultInfo info)
        {
            Defects_lst.Items.Clear();

            if (info.Defects.Count == 0)
            {
                Defects_lst.Items.Add("결함 없음.");
            }
            else
            {
                for (int i = 0; i < info.Defects.Count ; i++)
                {
                    Defects_lst.Items.Add(i);
                }
            }
        }


        private void GetDatResult_Mic(ref ResultInfo info)
        {
            
            IniReader result = new IniReader(info.datName.FullName);

            int Defect_num = result.GetInteger("LAND_DEFECT", "DEFECT_NUM");

            info.Defects = new List<Defect_struct>();
            info.BF_Defects = new List<Defect_struct>();
            info.CO_Defects = new List<Defect_struct>();
            info.DF_Defects = new List<Defect_struct>();

           
            for (int i = 0; i < Defect_num; i++)
            {
                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("LAND_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));

                info.Defects.Add(temp);
             
            }

            Defect_num = result.GetInteger("EDGE_DEFECT", "DEFECT_NUM");
           
            for (int i = 0; i < Defect_num; i++)
            {

                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("EDGE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));

                info.Defects.Add(temp);
             
            }


            Defect_num = result.GetInteger("SADDLE_DEFECT", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("SADDLE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));

                info.Defects.Add(temp);

            }



            Defect_num = result.GetInteger("OUTSIDE_DEFECT", "DEFECT_NUM");
           
            for (int i = 0; i < Defect_num; i++)
            {
            
                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("OUTSIDE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));

                info.Defects.Add(temp);

            }

            Defect_num = result.GetInteger("DF", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("DF", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("DF", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("DF", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("DF", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("DF", string.Format("DEFECT{0}_ANGLE", i)));

                info.DF_Defects.Add(temp);

            }

            Defect_num = result.GetInteger("BF", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("BF", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("BF", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("BF", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("BF", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("BF", string.Format("DEFECT{0}_ANGLE", i)));

                info.BF_Defects.Add(temp);

            }


            Defect_num = result.GetInteger("CO", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Defect_struct temp = new Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("CO", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("CO", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("CO", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("CO", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("CO", string.Format("DEFECT{0}_ANGLE", i)));

                info.CO_Defects.Add(temp);

            }

        }
        private void GetDatResult_Mac(ref ResultInfo info)
        {

            IniReader reader = new IniReader(info.datName.FullName);
            info.Defects = new List<Defect_struct>();


            #region 매크로 결함정보
            string section = "Attach";
             int  num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Defect_struct item = new Defect_struct();
                
                int Left = reader.GetInteger(section, string.Format("Defect{0}X1", i));
                int  Top = reader.GetInteger(section, string.Format("Defect{0}Y1", i));
                int  Right = reader.GetInteger(section, string.Format("Defect{0}X2", i));
                int  Bottom = reader.GetInteger(section, string.Format("Defect{0}Y2", i));

                item.cenx = Left + (Right - Left) / 2;
                item.ceny = Top + (Bottom - Top) / 2;
                item.width = (Right - Left);
                item.height = Bottom - Top;
                item.angle = 0;

                info.Defects.Add(item);
            }

            section = "Caking";
            num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Defect_struct item = new Defect_struct();

                int Left = reader.GetInteger(section, string.Format("Defect{0}X1", i));
                int Top = reader.GetInteger(section, string.Format("Defect{0}Y1", i));
                int Right = reader.GetInteger(section, string.Format("Defect{0}X2", i));
                int Bottom = reader.GetInteger(section, string.Format("Defect{0}Y2", i));

                item.cenx = Left + (Right - Left) / 2;
                item.ceny = Top + (Bottom - Top) / 2;
                item.width = (Right - Left);
                item.height = Bottom - Top;
                item.angle = 0;

                info.Defects.Add(item);
            }

            section = "OutsideBump";
            num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Defect_struct item = new Defect_struct();

                int Left = reader.GetInteger(section, string.Format("Defect{0}X1", i));
                int Top = reader.GetInteger(section, string.Format("Defect{0}Y1", i));
                int Right = reader.GetInteger(section, string.Format("Defect{0}X2", i));
                int Bottom = reader.GetInteger(section, string.Format("Defect{0}Y2", i));

                item.cenx = Left + (Right - Left) / 2;
                item.ceny = Top + (Bottom - Top) / 2;
                item.width = (Right - Left);
                item.height = Bottom - Top;
                item.angle = 0;

                info.Defects.Add(item);
            }

            section = "EdgeBroken";
            num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Defect_struct item = new Defect_struct();

                int Left = reader.GetInteger(section, string.Format("Defect{0}X1", i));
                int Top = reader.GetInteger(section, string.Format("Defect{0}Y1", i));
                int Right = reader.GetInteger(section, string.Format("Defect{0}X2", i));
                int Bottom = reader.GetInteger(section, string.Format("Defect{0}Y2", i));

                item.cenx = Left + (Right - Left) / 2;
                item.ceny = Top + (Bottom - Top) / 2;
                item.width = (Right - Left);
                item.height = Bottom - Top;
                item.angle = 0;

                info.Defects.Add(item);
            }

            section = "HoleDefect";
            num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Defect_struct item = new Defect_struct();

                int Left = reader.GetInteger(section, string.Format("Defect{0}X1", i));
                int Top = reader.GetInteger(section, string.Format("Defect{0}Y1", i));
                int Right = reader.GetInteger(section, string.Format("Defect{0}X2", i));
                int Bottom = reader.GetInteger(section, string.Format("Defect{0}Y2", i));

                item.cenx = Left + (Right - Left) / 2;
                item.ceny = Top + (Bottom - Top) / 2;
                item.width = (Right - Left);
                item.height = Bottom - Top;
                item.angle = 0;

                info.Defects.Add(item);
            }

           
            #endregion 매크로 결함정보

            
        }
        private void DrawDefects(ResultInfo info)
        {
            HObject temp_rect;
            HOperatorSet.GenEmptyObj(out DefectRegion);
            HOperatorSet.GenEmptyObj(out DefectRegion_DF);
            HOperatorSet.GenEmptyObj(out DefectRegion_BF);
            HOperatorSet.GenEmptyObj(out DefectRegion_CO);

            foreach (Defect_struct d in info.Defects)
            {
                HTuple deg=new HTuple(d.angle);
               HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height/2, d.width/2);
                HOperatorSet.Union2(DefectRegion, temp_rect, out DefectRegion);
            }

            foreach (Defect_struct d in info.BF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_BF, temp_rect, out DefectRegion_BF);
            }
            foreach (Defect_struct d in info.DF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_DF, temp_rect, out DefectRegion_DF);
            }
            foreach (Defect_struct d in info.CO_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_CO, temp_rect, out DefectRegion_CO);
            }
        }

        private void intersection_Defect(ResultInfo info)
        {
            HObject temp_rect, intersection_rgn;
            HTuple blob_area;
            info.Real_BF_Defects = new List<Defect_struct>();
            info.Real_CO_Defects = new List<Defect_struct>();
            info.Real_DF_Defects = new List<Defect_struct>();

            foreach (Defect_struct d in info.BF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.GenEmptyObj(out intersection_rgn);
                HOperatorSet.Intersection(select_defectRgn, temp_rect, out intersection_rgn);
                HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                if(blob_area>1)
                {
                    info.Real_BF_Defects.Add(d);
                }
            }

            foreach (Defect_struct d in info.DF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.GenEmptyObj(out intersection_rgn);
                HOperatorSet.Intersection(select_defectRgn, temp_rect, out intersection_rgn);
                HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                if (blob_area > 1)
                {
                    info.Real_DF_Defects.Add(d);
                }
            }

            foreach (Defect_struct d in info.CO_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.GenEmptyObj(out intersection_rgn);
                HOperatorSet.Intersection(select_defectRgn, temp_rect, out intersection_rgn);
                HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                if (blob_area > 1)
                {
                    info.Real_CO_Defects.Add(d);
                }
            }
        }

        private HObject DrawDefects_select(Defect_struct defect)
        {
            HObject temp_rect;

            HOperatorSet.GenEmptyObj(out temp_rect);
            HTuple deg = new HTuple(defect.angle);
            HOperatorSet.GenRectangle2(out temp_rect, defect.ceny, defect.cenx, deg.TupleRad(), defect.height/2, defect.width/2);

            return temp_rect;
        }

        System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2.0f);
        private void DrawDefects_onRawImg(List<Defect_struct> defects)
        {
            d_BF = p_BF.Clone() as Bitmap;// new Bitmap(p_BF.Width, p_DF.Height, Graphics.FromImage(p_BF));
           foreach (Defect_struct defect in defects)
            {
                using (Graphics g = Graphics.FromImage(d_BF))
                {
                    System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                    m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                    g.Transform = m;
                    g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                    m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                    g.Transform = m;

                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defects"></param>
        /// <param name="Mode">0: BF, 1:DF, 2:CO</param>
        private void DrawDefects_onRawImg(List<Defect_struct> defects, int Mode)
        {

            if (Mode == 0)
            {
                d_BF = p_BF.Clone() as Bitmap;
                using (Graphics g = Graphics.FromImage(d_BF))
                {
                    foreach (Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;

                    }
                }
            }

            else if (Mode == 1)
            {
                d_DF = p_DF.Clone() as Bitmap;
                using (Graphics g = Graphics.FromImage(d_DF))
                {
                    foreach (Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                    }
                }
            }
            else if (Mode == 2)
            {
                d_CO = p_CO.Clone() as Bitmap;
                using (Graphics g = Graphics.FromImage(d_CO))
                {
                    foreach (Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                    }
                }
            }

        }

        private void Defects_lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            var select_ind = list.SelectedItems;
           
            HOperatorSet.GenEmptyObj(out select_defectRgn);
            foreach (var temp in select_ind)
            {
                int  t = Convert.ToInt32(temp.ToString());

               HObject select=  DrawDefects_select(infos[Current_i].Defects[t]);
                HOperatorSet.Union2(select_defectRgn, select, out select_defectRgn);
            }

            UpdateWindow(0);
          
        }

        private void Prev_Btn_Click(object sender, RoutedEventArgs e)
        {
            Button click_btn = sender as Button;
            int befor_i = Current_i;

            if (click_btn.Name == "Prev_Btn")
            {
                Current_i--;
                if (Current_i < 0)
                {
                    Current_i = 0;
                    MessageBox.Show("첫번째 이미지");
                }
            }
            else
            {
                Current_i++;
                if (Current_i > infos.Count - 1)
                {
                    Current_i = infos.Count - 1;
                    MessageBox.Show("마지막 이미지");
                }
            }
            try
            {
                var select_ind = Defects_lst.SelectedItems;
                if (select_ind.Count > 0)
                {
                    foreach (var temp in select_ind)
                    {
                        int t = Convert.ToInt32(temp.ToString());

                        if (infos[befor_i].Real_Defects == null) infos[befor_i].Real_Defects = new List<Defect_struct>();
                        infos[befor_i].Real_Defects.Add(infos[befor_i].Defects[t]);

                    }

                    intersection_Defect(infos[befor_i]);
                    DrawDefects_onRawImg(infos[befor_i].Real_BF_Defects,0);
                    DrawDefects_onRawImg(infos[befor_i].Real_DF_Defects, 1);
                    DrawDefects_onRawImg(infos[befor_i].Real_CO_Defects, 2);

                    if (!Directory.Exists(RootAdderss + "\\Defect_Image")) Directory.CreateDirectory(RootAdderss + "\\Defect_Image");

                    p_BF.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_bf, ImageFormat.Bmp);
                    p_DF.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_df, ImageFormat.Bmp);
                    p_CO.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_co, ImageFormat.Bmp);
                    if(d_BF!=null) d_BF.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_bf_defect, ImageFormat.Bmp);
                    if(d_DF!=null)d_DF.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_df_defect, ImageFormat.Bmp);
                    if(d_CO!=null)d_CO.Save(RootAdderss + "\\Defect_Image\\" + infos[befor_i].ImageName_co_defect, ImageFormat.Bmp);

                    saveGrt(infos[befor_i]);


                }
                else
                {
                    if (!Directory.Exists(RootAdderss + "\\OK_Image")) Directory.CreateDirectory(RootAdderss + "\\OK_Image");

                    p_BF.Save(RootAdderss + "\\OK_Image\\" + infos[befor_i].ImageName_bf, ImageFormat.Bmp);
                    p_DF.Save(RootAdderss + "\\OK_Image\\" + infos[befor_i].ImageName_df, ImageFormat.Bmp);
                    p_CO.Save(RootAdderss + "\\OK_Image\\" + infos[befor_i].ImageName_co, ImageFormat.Bmp);
                    //d_BF.Save(RootAdderss + "\\OK_Image\\" + infos[befor_i].ImageName_bf_defect, ImageFormat.Bmp);

                }
              //  SaveContents(infos[befor_i]);
                AddList(infos[Current_i]);
                ImageName_lbl.Content = infos[Current_i].datName.Name;
                Convert_Image_Mic(infos[Current_i].CH0, infos[Current_i].CH1, infos[Current_i].CH2, infos[Current_i].CH3);
                DrawDefects(infos[Current_i]);
                
                UpdateWindow(0);
            }
            catch
            {    Writer.Close();}
            finally
            {
                //p_BF.Dispose();
                //p_DF.Dispose();
                //p_CO.Dispose();
                if (d_BF != null) d_BF.Dispose();
                if (d_BF != null) d_CO.Dispose();
                if (d_BF != null) d_DF.Dispose();
            }
        }
        private void saveGrt(ResultInfo res)
        {
            if (File.Exists(RootAdderss + "\\Defect_Image\\" + res.datName.Name.Replace("dat", "txt"))) File.Delete(RootAdderss + "\\Defect_Image\\" + res.datName.Name.Replace("dat", "txt"));
            IniReader grt = new IniReader(RootAdderss + "\\Defect_Image\\"+res.datName.Name.Replace("dat", "txt"));

            for(int i=0;i<res.Real_BF_Defects.Count;i++)
            {
                string Defect_centerx = string.Format("DEFECT{0}_CENTER_X", i);
                string Defect_centery = string.Format("DEFECT{0}_CENTER_Y", i);
                string Defect_w= string.Format("DEFECT{0}_WIDTH", i);
                string Defect_h = string.Format("DEFECT{0}_HEIGHT", i);
                string Defect_a = string.Format("DEFECT{0}_ANGLE", i);

                grt.SetString("BF_DEFECT_INFO", Defect_centerx, res.Real_BF_Defects[i].cenx.ToString());
                grt.SetString("BF_DEFECT_INFO", Defect_centery, res.Real_BF_Defects[i].ceny.ToString());
                grt.SetString("BF_DEFECT_INFO", Defect_w, res.Real_BF_Defects[i].width.ToString());
                grt.SetString("BF_DEFECT_INFO", Defect_h, res.Real_BF_Defects[i].height.ToString());
                grt.SetString("BF_DEFECT_INFO", Defect_a, res.Real_BF_Defects[i].angle.ToString());
            }

            for (int i = 0; i < res.Real_DF_Defects.Count; i++)
            {
                string Defect_centerx = string.Format("DEFECT{0}_CENTER_X", i);
                string Defect_centery = string.Format("DEFECT{0}_CENTER_Y", i);
                string Defect_w = string.Format("DEFECT{0}_WIDTH", i);
                string Defect_h = string.Format("DEFECT{0}_HEIGHT", i);
                string Defect_a = string.Format("DEFECT{0}_ANGLE", i);

                grt.SetString("DF_DEFECT_INFO", Defect_centerx, res.Real_DF_Defects[i].cenx.ToString());
                grt.SetString("DF_DEFECT_INFO", Defect_centery, res.Real_DF_Defects[i].ceny.ToString());
                grt.SetString("DF_DEFECT_INFO", Defect_w, res.Real_DF_Defects[i].width.ToString());
                grt.SetString("DF_DEFECT_INFO", Defect_h, res.Real_DF_Defects[i].height.ToString());
                grt.SetString("DF_DEFECT_INFO", Defect_a, res.Real_DF_Defects[i].angle.ToString());
            }

            for (int i = 0; i < res.Real_CO_Defects.Count; i++)
            {
                string Defect_centerx = string.Format("DEFECT{0}_CENTER_X", i);
                string Defect_centery = string.Format("DEFECT{0}_CENTER_Y", i);
                string Defect_w = string.Format("DEFECT{0}_WIDTH", i);
                string Defect_h = string.Format("DEFECT{0}_HEIGHT", i);
                string Defect_a = string.Format("DEFECT{0}_ANGLE", i);

                grt.SetString("CO_DEFECT_INFO", Defect_centerx, res.Real_CO_Defects[i].cenx.ToString());
                grt.SetString("CO_DEFECT_INFO", Defect_centery, res.Real_CO_Defects[i].ceny.ToString());
                grt.SetString("CO_DEFECT_INFO", Defect_w, res.Real_CO_Defects[i].width.ToString());
                grt.SetString("CO_DEFECT_INFO", Defect_h, res.Real_CO_Defects[i].height.ToString());
                grt.SetString("CO_DEFECT_INFO", Defect_a, res.Real_CO_Defects[i].angle.ToString());
            }
        }

        private void UpdateWindow(int mode)
        {
            var halW = Halcon_Window.HalconWindow;
            HOperatorSet.SetDraw(halW, "margin");

            HOperatorSet.SetColor(halW, "spring green");

            switch (mode)
            {
                case 1:
                    break;

                default:
                    HOperatorSet.DispColor(BaseImage_BF, halW);
                    HOperatorSet.DispObj(DefectRegion, halW);

                    HOperatorSet.SetColor(halW, "medium slate blue");
                    HOperatorSet.DispObj(DefectRegion_BF, halW);
                    HOperatorSet.DispObj(DefectRegion_DF, halW);
                    HOperatorSet.DispObj(DefectRegion_CO, halW);
                    HOperatorSet.SetColor(halW, "red");
                    HOperatorSet.DispObj(select_defectRgn, halW);
                    break;
            }
           
        }

        public void Detect_btn_Click(object sender, RoutedEventArgs e)
        {
            Button click_btn = sender as Button;

            switch (click_btn.Name)
            {
                case "Detect_btn":
                    infos[Current_i].Detect_num = Defects_lst.SelectedItems.Count;
                    break;
                case "Under_btn":
                    UnderDetail.Visibility = Visibility.Visible;
                  
                    break;
                case "Over_btn":
                    OverDetail.Visibility = Visibility.Visible;
                    break;
                case "Dust_btn":
                    infos[Current_i].Over_Dust_num = Defects_lst.SelectedItems.Count;
                    OverDetail.Visibility = Visibility.Hidden;
                    break;
                case "Chip_btn":
                    infos[Current_i].Over_Defect_num = Defects_lst.SelectedItems.Count;
                    OverDetail.Visibility = Visibility.Hidden;
                    break;
                case "Bump_btn":
                    infos[Current_i].Over_Bump_num = Defects_lst.SelectedItems.Count;
                    OverDetail.Visibility = Visibility.Hidden;
                    break;
                case "Ect_btn":
                    infos[Current_i].Over_Ect_num = Defects_lst.SelectedItems.Count;
                    OverDetail.Visibility = Visibility.Hidden;
                    break;
                case "Done_btn":
                    infos[Current_i].Under_num = Convert.ToInt32(UnderNum_txt.Text);
                    UnderDetail.Visibility = Visibility.Hidden;

                    break;
            }
        }

        private void SaveContents(ResultInfo info)
        {
            int overnum = info.Over_Bump_num + info.Over_Defect_num + info.Over_Dust_num + info.Over_Ect_num;
            Writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", info.datName.Name, info.Detect_num, info.Under_num, overnum, info.Over_Dust_num, info.Over_Bump_num, info.Over_Defect_num, info.Over_Ect_num));
        }


        double orginalWidth, originalHeight;

        System.Windows.Media.ScaleTransform scale = new System.Windows.Media.ScaleTransform();

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeSize(this.ActualWidth, this.ActualHeight);
        }

        private void ChangeSize(double width, double height)
        {

            scale.ScaleX = width / orginalWidth;

            scale.ScaleY = height / originalHeight;



            FrameworkElement rootElement = this.Content as FrameworkElement;



            rootElement.LayoutTransform = scale;

        }

        private void save_grt(ResultInfo info)
        {
            if (!Directory.Exists(RootAdderss + "\\Grt"))
            {
                Directory.CreateDirectory(RootAdderss + "\\Grt");
            }

            string grt_path = string.Format("{0}\\Grt\\{1}", RootAdderss, info.datName.Replace("dat", "grt"));

            IniReader gr= new IniReader(grt_path);
            
            

        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label s = sender as Label;
            OpenFileDialog file = new OpenFileDialog();
            if (file.ShowDialog().Value ==true)
            {
                if (s.Content.ToString()== "Recipe")
                {
                    RecipeIOI.Read(file.FileName);
                    s.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 185, 185));
                    ModelRcpPath = file.FileName;
                    //RecipeIOI.Read(@"C:\Users\WTA\Desktop\grtTEST\CRecipe.rcp");
                }
                else
                {
                    SYS.Read(file.FileName, InspMode);
                    SysRcpPath = file.FileName;
                    s.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 185, 185));
                    //SYS.Read(@"C:\Users\WTA\Desktop\grtTEST\System.rcp", MemMapName.TMIC);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button s = sender as Button;
            switch (s.Name)
            {
                case "btn_Mac":
                    InspMode = MemMapName.TMAC;
                    if(SysRcpPath!=null) SYS.Read(SysRcpPath, InspMode);
                    s.Background= new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 255, 127));
                    btn_Mic.Background=  new SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 197, 197));
                    break;
                case "btn_Mic":
                    InspMode = MemMapName.TMIC;
                    if (SysRcpPath != null) SYS.Read(SysRcpPath, InspMode);
                    s.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 255, 127));
                    btn_Mac.Background= new SolidColorBrush(System.Windows.Media.Color.FromRgb(197, 197, 197));
                    break;
            }

        }

        private void Convert_Image_Mic(FileInfo ch0, FileInfo ch1, FileInfo ch2, FileInfo ch3)
        {
            string name = ch0.Name.Split('C')[0];
            string first = name.Substring(0, 9);
            string c_num = name.Substring(name.Length - 1);
            //PathDF = string.Format("{0}\\Result_dll\\{1}DF{2}.bmp", RootAdderss, first, c_num);
            //PathBF = string.Format("{0}\\Result_dll\\{1}BF{2}.bmp", RootAdderss, first, c_num);
            //PathCO= string.Format("{0}\\Result_dll\\{1}CO{2}.bmp", RootAdderss, first, c_num);


           
                Bitmap Ch0 = new Bitmap(ch0.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh0.bmp"));
                Bitmap Ch1 = new Bitmap(ch1.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh1.bmp"));
                Bitmap Ch2 = new Bitmap(ch2.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh2.bmp"));
                Bitmap Ch3 = new Bitmap(ch3.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh3.bmp"));


                BitmapData bufCh0 = Ch0.LockBits(new System.Drawing.Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                BitmapData bufCh1 = Ch1.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                BitmapData bufCh2 = Ch2.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                BitmapData bufCh3 = Ch3.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

                IntPtr PtrBufCh0 = bufCh0.Scan0;
                IntPtr PtrBufCh1 = bufCh1.Scan0;
                IntPtr PtrBufCh2 = bufCh2.Scan0;
                IntPtr PtrBufCh3 = bufCh3.Scan0;


                p_BF = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                p_DF = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                Bitmap Shilluette = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                p_CO = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                BitmapData BufBF = p_BF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                BitmapData BufDF = p_DF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                BitmapData BufShilluette = Shilluette.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                BitmapData BufCO = p_CO.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                IntPtr PtrBufDF = BufDF.Scan0;
                IntPtr PtrBufBF = BufBF.Scan0;
                IntPtr PtrBufShilluette = BufShilluette.Scan0;
                IntPtr PtrBufCO = BufCO.Scan0;

                Bitmap ImageBuf0 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf1 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf2 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf3 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);

                BitmapData DImageBuf0 = ImageBuf0.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf1 = ImageBuf1.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf2 = ImageBuf2.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf3 = ImageBuf3.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                IntPtr PtrImageBuf0 = DImageBuf0.Scan0;
                IntPtr PtrImageBuf1 = DImageBuf1.Scan0;
                IntPtr PtrImageBuf2 = DImageBuf2.Scan0;
                IntPtr PtrImageBuf3 = DImageBuf3.Scan0;

                Bitmap ImageBuf0_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf1_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf2_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
                Bitmap ImageBuf3_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);

                BitmapData DImageBuf0_ = ImageBuf0_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf1_ = ImageBuf1_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf2_ = ImageBuf2_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                BitmapData DImageBuf3_ = ImageBuf3_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);


                IntPtr PtrImageBuf0_ = DImageBuf0_.Scan0;
                IntPtr PtrImageBuf1_ = DImageBuf1_.Scan0;
                IntPtr PtrImageBuf2_ = DImageBuf2_.Scan0;
                IntPtr PtrImageBuf3_ = DImageBuf3_.Scan0;

            try
            {
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf0, PtrBufCh0, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf1, PtrBufCh1, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf2, PtrBufCh2, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf3, PtrBufCh3, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());

                //DarkField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmicDF[0], SYS.GainCh0[1] * RecipeIOI.IlTmicDF[0], SYS.GainCh0[2] * RecipeIOI.IlTmicDF[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmicDF[1], SYS.GainCh1[1] * RecipeIOI.IlTmicDF[1], SYS.GainCh1[2] * RecipeIOI.IlTmicDF[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmicDF[2], SYS.GainCh2[1] * RecipeIOI.IlTmicDF[2], SYS.GainCh2[2] * RecipeIOI.IlTmicDF[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmicDF[3], SYS.GainCh3[1] * RecipeIOI.IlTmicDF[3], SYS.GainCh3[2] * RecipeIOI.IlTmicDF[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufDF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //DarkField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmicBF[0], SYS.GainCh0[1] * RecipeIOI.IlTmicBF[0], SYS.GainCh0[2] * RecipeIOI.IlTmicBF[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmicBF[1], SYS.GainCh1[1] * RecipeIOI.IlTmicBF[1], SYS.GainCh1[2] * RecipeIOI.IlTmicBF[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmicBF[2], SYS.GainCh2[1] * RecipeIOI.IlTmicBF[2], SYS.GainCh2[2] * RecipeIOI.IlTmicBF[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmicBF[3], SYS.GainCh3[1] * RecipeIOI.IlTmicBF[3], SYS.GainCh3[2] * RecipeIOI.IlTmicBF[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufBF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //double[] IlTmicCO = { 0, 0, 1.0, 1.0 };


                //MiddleField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmicCO[0], SYS.GainCh0[1] * RecipeIOI.IlTmicCO[0], SYS.GainCh0[2] * RecipeIOI.IlTmicCO[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmicCO[1], SYS.GainCh1[1] * RecipeIOI.IlTmicCO[1], SYS.GainCh1[2] * RecipeIOI.IlTmicCO[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmicCO[2], SYS.GainCh2[1] * RecipeIOI.IlTmicCO[2], SYS.GainCh2[2] * RecipeIOI.IlTmicCO[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmicCO[3], SYS.GainCh3[1] * RecipeIOI.IlTmicCO[3], SYS.GainCh3[2] * RecipeIOI.IlTmicCO[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufCO, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //BackLight -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmicBL[0], SYS.GainCh0[1] * RecipeIOI.IlTmicBL[0], SYS.GainCh0[2] * RecipeIOI.IlTmicBL[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmicBL[1], SYS.GainCh1[1] * RecipeIOI.IlTmicBL[1], SYS.GainCh1[2] * RecipeIOI.IlTmicBL[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmicBL[2], SYS.GainCh2[1] * RecipeIOI.IlTmicBL[2], SYS.GainCh2[2] * RecipeIOI.IlTmicBL[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmicBL[3], SYS.GainCh3[1] * RecipeIOI.IlTmicBL[3], SYS.GainCh3[2] * RecipeIOI.IlTmicBL[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufShilluette, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                HOperatorSet.GenImageInterleaved(out BaseImage_BF, BufBF.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);
               
            }
            catch 
            { }
            finally
            {
                p_BF.UnlockBits(BufBF);
                p_DF.UnlockBits(BufDF);
                Shilluette.UnlockBits(BufShilluette);
                p_CO.UnlockBits(BufCO);
                Ch0.UnlockBits(bufCh0);
                Ch1.UnlockBits(bufCh1);
                Ch2.UnlockBits(bufCh2);
                Ch3.UnlockBits(bufCh3);
                ImageBuf0.UnlockBits(DImageBuf0);
                ImageBuf1.UnlockBits(DImageBuf1);
                ImageBuf2.UnlockBits(DImageBuf2);
                ImageBuf3.UnlockBits(DImageBuf3);
                ImageBuf0_.UnlockBits(DImageBuf0_);
                ImageBuf1_.UnlockBits(DImageBuf1_);
                ImageBuf2_.UnlockBits(DImageBuf2_);
                ImageBuf3_.UnlockBits(DImageBuf3_);


          
                Shilluette.Dispose();
                Ch0.Dispose();
                Ch1.Dispose();
                Ch2.Dispose();
                Ch3.Dispose();
                ImageBuf0.Dispose();
                ImageBuf1.Dispose();
                ImageBuf2.Dispose();
                ImageBuf3.Dispose();
                ImageBuf0_.Dispose();
                ImageBuf1_.Dispose();
                ImageBuf2_.Dispose();
                ImageBuf3_.Dispose();

            }
        }


        private void Convert_Image_Mac(FileInfo ch0, FileInfo ch1, FileInfo ch2, FileInfo ch3)
        {
            string name = ch0.Name.Split('C')[0];
            string first = name.Substring(0, 9);
            string c_num = name.Substring(name.Length - 1);
            //PathDF = string.Format("{0}\\Result_dll\\{1}DF{2}.bmp", RootAdderss, first, c_num);
            //PathBF = string.Format("{0}\\Result_dll\\{1}BF{2}.bmp", RootAdderss, first, c_num);
            //PathCO= string.Format("{0}\\Result_dll\\{1}CO{2}.bmp", RootAdderss, first, c_num);



            Bitmap Ch0 = new Bitmap(ch0.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh0.bmp"));
            Bitmap Ch1 = new Bitmap(ch1.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh1.bmp"));
            Bitmap Ch2 = new Bitmap(ch2.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh2.bmp"));
            Bitmap Ch3 = new Bitmap(ch3.FullName);// new Bitmap(string.Format("{0}\\{1}\\{2}", Folder, "RawMicro", "[0001]TopCh3.bmp"));


            BitmapData bufCh0 = Ch0.LockBits(new System.Drawing.Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            BitmapData bufCh1 = Ch1.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bufCh2 = Ch2.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bufCh3 = Ch3.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            IntPtr PtrBufCh0 = bufCh0.Scan0;
            IntPtr PtrBufCh1 = bufCh1.Scan0;
            IntPtr PtrBufCh2 = bufCh2.Scan0;
            IntPtr PtrBufCh3 = bufCh3.Scan0;


            p_BF = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            p_DF = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Bitmap Shilluette = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            p_CO = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


            BitmapData BufBF = p_BF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BitmapData BufDF = p_DF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BitmapData BufShilluette = Shilluette.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BitmapData BufCO = p_CO.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            IntPtr PtrBufDF = BufDF.Scan0;
            IntPtr PtrBufBF = BufBF.Scan0;
            IntPtr PtrBufShilluette = BufShilluette.Scan0;
            IntPtr PtrBufCO = BufCO.Scan0;

            Bitmap ImageBuf0 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf1 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf2 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf3 = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);

            BitmapData DImageBuf0 = ImageBuf0.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf1 = ImageBuf1.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf2 = ImageBuf2.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf3 = ImageBuf3.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            IntPtr PtrImageBuf0 = DImageBuf0.Scan0;
            IntPtr PtrImageBuf1 = DImageBuf1.Scan0;
            IntPtr PtrImageBuf2 = DImageBuf2.Scan0;
            IntPtr PtrImageBuf3 = DImageBuf3.Scan0;

            Bitmap ImageBuf0_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf1_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf2_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);
            Bitmap ImageBuf3_ = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, PixelFormat.Format24bppRgb);

            BitmapData DImageBuf0_ = ImageBuf0_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf1_ = ImageBuf1_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf2_ = ImageBuf2_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData DImageBuf3_ = ImageBuf3_.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);


            IntPtr PtrImageBuf0_ = DImageBuf0_.Scan0;
            IntPtr PtrImageBuf1_ = DImageBuf1_.Scan0;
            IntPtr PtrImageBuf2_ = DImageBuf2_.Scan0;
            IntPtr PtrImageBuf3_ = DImageBuf3_.Scan0;

            try
            {
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf0, PtrBufCh0, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf1, PtrBufCh1, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf2, PtrBufCh2, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());
                DllImport.CvtImageDLLImport.Demosaic(PtrImageBuf3, PtrBufCh3, SYS.ImageWidth, SYS.ImageHeight, SYS.BayerType.ToString());

                //DarkField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmacDark[0], SYS.GainCh0[1] * RecipeIOI.IlTmacDark[0], SYS.GainCh0[2] * RecipeIOI.IlTmacDark[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmacDark[1], SYS.GainCh1[1] * RecipeIOI.IlTmacDark[1], SYS.GainCh1[2] * RecipeIOI.IlTmacDark[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmacDark[2], SYS.GainCh2[1] * RecipeIOI.IlTmacDark[2], SYS.GainCh2[2] * RecipeIOI.IlTmacDark[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmacDark[3], SYS.GainCh3[1] * RecipeIOI.IlTmacDark[3], SYS.GainCh3[2] * RecipeIOI.IlTmacDark[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufDF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //DarkField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmacData[0], SYS.GainCh0[1] * RecipeIOI.IlTmacData[0], SYS.GainCh0[2] * RecipeIOI.IlTmacData[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmacData[1], SYS.GainCh1[1] * RecipeIOI.IlTmacData[1], SYS.GainCh1[2] * RecipeIOI.IlTmacData[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmacData[2], SYS.GainCh2[1] * RecipeIOI.IlTmacData[2], SYS.GainCh2[2] * RecipeIOI.IlTmacData[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmacData[3], SYS.GainCh3[1] * RecipeIOI.IlTmacData[3], SYS.GainCh3[2] * RecipeIOI.IlTmacData[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufBF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //double[] IlTmicCO = { 0, 0, 1.0, 1.0 };


                //MiddleField -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmacArea[0], SYS.GainCh0[1] * RecipeIOI.IlTmacArea[0], SYS.GainCh0[2] * RecipeIOI.IlTmacArea[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmacArea[1], SYS.GainCh1[1] * RecipeIOI.IlTmacArea[1], SYS.GainCh1[2] * RecipeIOI.IlTmacArea[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmacArea[2], SYS.GainCh2[1] * RecipeIOI.IlTmacArea[2], SYS.GainCh2[2] * RecipeIOI.IlTmacArea[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmacArea[3], SYS.GainCh3[1] * RecipeIOI.IlTmacArea[3], SYS.GainCh3[2] * RecipeIOI.IlTmacArea[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufCO, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                //BackLight -> WB 및 Intensity 적용
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, PtrImageBuf0_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh0[0] * RecipeIOI.IlTmacMeas[0], SYS.GainCh0[1] * RecipeIOI.IlTmacMeas[0], SYS.GainCh0[2] * RecipeIOI.IlTmacMeas[0]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, PtrImageBuf1_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh1[0] * RecipeIOI.IlTmacMeas[1], SYS.GainCh1[1] * RecipeIOI.IlTmacMeas[1], SYS.GainCh1[2] * RecipeIOI.IlTmacMeas[1]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, PtrImageBuf2_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh2[0] * RecipeIOI.IlTmacMeas[2], SYS.GainCh2[1] * RecipeIOI.IlTmacMeas[2], SYS.GainCh2[2] * RecipeIOI.IlTmacMeas[2]);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, PtrImageBuf3_, SYS.ImageWidth, SYS.ImageHeight, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.GainCh3[0] * RecipeIOI.IlTmacMeas[3], SYS.GainCh3[1] * RecipeIOI.IlTmacMeas[3], SYS.GainCh3[2] * RecipeIOI.IlTmacMeas[3]);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufShilluette, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.ImageSize);

                HOperatorSet.GenImageInterleaved(out BaseImage_BF, BufBF.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);

            }
            catch
            { }
            finally
            {
                p_BF.UnlockBits(BufBF);
                p_DF.UnlockBits(BufDF);
                Shilluette.UnlockBits(BufShilluette);
                p_CO.UnlockBits(BufCO);
                Ch0.UnlockBits(bufCh0);
                Ch1.UnlockBits(bufCh1);
                Ch2.UnlockBits(bufCh2);
                Ch3.UnlockBits(bufCh3);
                ImageBuf0.UnlockBits(DImageBuf0);
                ImageBuf1.UnlockBits(DImageBuf1);
                ImageBuf2.UnlockBits(DImageBuf2);
                ImageBuf3.UnlockBits(DImageBuf3);
                ImageBuf0_.UnlockBits(DImageBuf0_);
                ImageBuf1_.UnlockBits(DImageBuf1_);
                ImageBuf2_.UnlockBits(DImageBuf2_);
                ImageBuf3_.UnlockBits(DImageBuf3_);



                Shilluette.Dispose();
                Ch0.Dispose();
                Ch1.Dispose();
                Ch2.Dispose();
                Ch3.Dispose();
                ImageBuf0.Dispose();
                ImageBuf1.Dispose();
                ImageBuf2.Dispose();
                ImageBuf3.Dispose();
                ImageBuf0_.Dispose();
                ImageBuf1_.Dispose();
                ImageBuf2_.Dispose();
                ImageBuf3_.Dispose();

            }
        }
    }
}
