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
using System.Diagnostics;


namespace CheckPreformance
{

    
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {


        public Bitmap p_BF;
        public Bitmap d_BF;
        public Bitmap p_DF;
        public Bitmap p_CO;
        public Bitmap d_DF;
        public Bitmap d_CO;
        public Bitmap p_BL;
        public Bitmap d_BL;

        public FileInfo[] datFiles;
        public List<Structure.ResultInfo> infos;
        public HObject DefectRegion;
        public HObject DefectRegion_BF;
        public HObject DefectRegion_DF;
        public HObject DefectRegion_CO;
        public HObject DefectRegion_BL;

        public HObject select_defectRgn;
        public int Current_i;
        public HObject BaseImage_BF;
        public HObject BaseImage_DF;
        public HObject BaseImage_CO;

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

            int DC_num = System.Enum.GetValues(typeof(Structure.Defect_Classification)).Length;
            for (int i = 0; i < DC_num; i++)
            {
                cmb_DC.Items.Add((Structure.Defect_Classification)(i+1));
            }


           // Halcon_Window.MouseLeftButtonDown+= HWindowControl1_HMouseDown;
            Halcon_Window.HMouseDown += HWindowControl1_HMouseDown;
            Halcon_Window.HMouseMove += HWindowControl1_HMouseMove;
            //Halcon_Window.MouseWheel += HSmartWindowEdit_MouseWheel;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {


            if (e.Key == Key.Q)
            {

                UpdateWindow(1);
            }
            else if (e.Key == Key.W)
            {
                UpdateWindow(2);
            }
            else if (e.Key == Key.E)
            {
                UpdateWindow(3);
            }
            else if (e.Key == Key.Delete)
            {
                string select = Defects_lst.SelectedItem.ToString();

                string[] temp_st = select.Split('-');

                if (temp_st[temp_st.Length - 1] == "Under")
                {
                    Structure.Defect_struct temp_under = infos[Current_i].Defects[Convert.ToInt32(temp_st[0])];
                    infos[Current_i].Defects.Remove(temp_under);
                    infos[Current_i].Under_Defects.Remove(temp_under);
                }
                //Defects_lst.Items.Remove(Defects_lst.SelectedItem);
                //Defects_lst.Items.Add(string.Format("{0}-{1}-{2}", infos[Current_i].Defects.Count - 1, cmb_DC.SelectedItem.ToString(), "Under"));
                Defects_lst.Items.Clear();

                for (int i = 0; i < infos[Current_i].Defects.Count - 1; i++)
                {
                    Structure.Defect_struct temp = infos[Current_i].Defects[i];

                    if (!temp.UnderDefect)
                    {
                        Defects_lst.Items.Add(string.Format("{0}-{1}", i, temp.Name.ToString()));
                    }
                    else
                    {
                        Defects_lst.Items.Add(string.Format("{0}-{1}-{2}",i,temp.Name.ToString(), "Under"));

                    }
                }

            }

            // throw new NotImplementedException();
        }

        bool underDetect_mode = false;
        bool drawstart = false;
        double Start_pt_x = 0;
        double Start_pt_y = 0;
        double End_pt_x = 0;
        double End_pt_y = 0;

        private void Under_btn_Click(object sender, RoutedEventArgs e)
        {
            if (underDetect_mode)
            {
                underDetect_mode = false;
                Structure.Defect_struct under_temp = new Structure.Defect_struct();
                under_temp.angle = 90;
                under_temp.cenx = Convert.ToInt32(Start_pt_x + (End_pt_x - Start_pt_x) / 2);
                under_temp.ceny = Convert.ToInt32(Start_pt_y + (End_pt_y - Start_pt_y) / 2);
                under_temp.width = Convert.ToInt32((End_pt_x - Start_pt_x));
                under_temp.height = Convert.ToInt32((End_pt_y - Start_pt_y));
                under_temp.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), cmb_DC.SelectedItem.ToString());
                under_temp.UnderDefect = true;
                if (infos[Current_i].Under_Defects == null) infos[Current_i].Under_Defects = new List<Structure.Defect_struct>();
                infos[Current_i].Under_Defects.Add(under_temp);
                infos[Current_i].Defects.Add(under_temp);

                
                Defects_lst.Items.Add(string.Format("{0}-{1}-{2}", infos[Current_i].Defects.Count-1, cmb_DC.SelectedItem.ToString(),"Under"));

                Under_btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221));
            }
            else
            {
                underDetect_mode = true;
                Under_btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(164, 255, 137));
            }
        }
        public void HWindowControl1_HMouseDown(object sender, EventArgs e)
        {
            if (underDetect_mode)
            {
                HSmartWindowControlWPF.HMouseEventArgsWPF t = e as HSmartWindowControlWPF.HMouseEventArgsWPF;
                HMouseEventHandlerWPF sss = sender as HMouseEventHandlerWPF;

                System.Drawing.Point temp = new System.Drawing.Point((int)t.Column, (int)t.Row);
                Debug.WriteLine(temp);
                if (drawstart == true)
                {
                    drawstart = false;
                    HObject temp_missing_rgn;
                    HOperatorSet.GenEmptyObj(out temp_missing_rgn);
                    HOperatorSet.GenRectangle1(out temp_missing_rgn, (int)Start_pt_y, (int)Start_pt_x, (int)t.Row, (int)t.Column);

                    var halW = Halcon_Window.HalconWindow;
                    HOperatorSet.SetDraw(halW, "margin");

                    HOperatorSet.SetColor(halW, "yellow");
                    HOperatorSet.DispObj(BaseImage_BF, halW);
                    HOperatorSet.DispObj(temp_missing_rgn, halW);

                    End_pt_x = t.Column;
                    End_pt_y = t.Row;

                }
                else
                {
                    Start_pt_x = t.Column;
                    Start_pt_y = t.Row;
                    drawstart = true;
                }
            }
        }
        public void HWindowControl1_HMouseMove(object sender, EventArgs e)
        {

            if (underDetect_mode)
            {
                if (drawstart)
                {
                    HSmartWindowControlWPF.HMouseEventArgsWPF t = e as HSmartWindowControlWPF.HMouseEventArgsWPF;
                    HObject temp_missing_rgn;
                    HOperatorSet.GenEmptyObj(out temp_missing_rgn);
                    if (Start_pt_x >= t.Column || Start_pt_y >= t.Row)
                    {
                       // HOperatorSet.DispObj(temp_missing_rgn, halW);
                    }
                    else
                    {
                        HOperatorSet.GenRectangle1(out temp_missing_rgn, (int)Start_pt_y, (int)Start_pt_x, (int)t.Row,(int)t.Column );

                        var halW = Halcon_Window.HalconWindow;
                        HOperatorSet.SetDraw(halW, "margin");

                        HOperatorSet.SetColor(halW, "yellow");
                        HOperatorSet.DispObj(BaseImage_BF, halW);
                        HOperatorSet.DispObj(temp_missing_rgn, halW);
                    }
                }
            }

        }




        public bool SetFolder(string Address)
        {
            if (InspMode == MemMapName.TMIC)
            {
                if (Directory.Exists(Address + "\\MicroDefect"))
                {
                    datFiles = new DirectoryInfo(Address + "\\MicroDefect").GetFiles("*.dat").OrderBy(x=>x.Name).ToArray();
                    infos = new List<Structure.ResultInfo>();
                    RootAdderss = Address;
                    foreach (FileInfo f in datFiles)
                    {
                        Structure.ResultInfo temp = new Structure.ResultInfo();
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
                        temp.ImageName_bl = string.Format("{0}BL{1}.bmp", name, corner);

                        temp.ImageName_bf_defect = string.Format("{0}BF{1}_Marking.bmp", name, corner);
                        temp.ImageName_df_defect = string.Format("{0}DF{1}_Marking.bmp", name, corner);
                        temp.ImageName_co_defect = string.Format("{0}CO{1}_Marking.bmp", name, corner);
                        temp.ImageName_bl_defect = string.Format("{0}BL{1}_Marking.bmp", name, corner);

                        GetDatResult_Mic(ref temp);

                        if (File.Exists(temp.datName.FullName.Replace("dat", "txt")))
                        {
                            Structure.ResultInfo gt_temp = new Structure.ResultInfo();
                            gt_temp.datName = f;
                            GetGTResult_Mic(ref temp);

                            //temp.Defects = gt_temp.Defects;
                            //temp.Under_Defects = gt_temp.Under_Defects;
                            //temp.Under_num = gt_temp.Under_Defects.Count;

                        }

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
                    infos = new List<Structure.ResultInfo>();
                    RootAdderss = Address;
                    foreach (FileInfo f in datFiles)
                    {
                        if (f.Name.Contains("Defect"))
                        {
                            Structure.ResultInfo temp = new Structure.ResultInfo();
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

                    foreach (Structure.ResultInfo image_ in infos)
                    {
                        cmb_ImgList.Items.Add(image_.datName.Name);
                    }

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

        private void AddList(Structure.ResultInfo info)
        {
            Defects_lst.Items.Clear();

            if (info.Defects.Count == 0)
            {
                Defects_lst.Items.Add("결함 없음.");
            }

            else //if (info.True_Defects.Count + info.False_Defects.Count + info.Under_Defects.Count > 0)
            {
                List<int> select_i = new List<int>();
                for (int i = 0; i < info.Defects.Count; i++)
                {
                    int name = (int)info.Defects[i].Name;

                    string list_format = string.Format("{0}-{1}", i, name==0?"":info.Defects[i].Name.ToString());
                    if (info.Defects[i].GroundTruth == 2) list_format += "-Under";
                    else
                    {
                        if (info.Defects[i].GroundTruth == 1) list_format += "-True";
                        else list_format += "-False";
                    }

                    Defects_lst.Items.Add(list_format);
                    if (info.Defects[i].GroundTruth == 1)
                    {
                        Defects_lst.SelectedItems.Add(i);
                        select_i.Add(i);
                    }
                }

                
            }
            //else
            //{
            //    for (int i = 0; i < info.Defects.Count; i++)
            //    {
            //        Defects_lst.Items.Add(i);
            //    }
            //}
        }


        private void GetDatResult_Mic(ref Structure.ResultInfo info)
        {
            
            IniReader result = new IniReader(info.datName.FullName);

            int Defect_num = result.GetInteger("LAND_DEFECT", "DEFECT_NUM");

            info.Defects = new List<Structure.Defect_struct>();
            info.BF_Defects = new List<Structure.Defect_struct>();
            info.CO_Defects = new List<Structure.Defect_struct>();
            info.DF_Defects = new List<Structure.Defect_struct>();
            info.BL_Defects = new List<Structure.Defect_struct>();

            for (int i = 0; i < Defect_num; i++)
            {
                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("LAND_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("LAND_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out = 1;
                temp.blob_ind = info.Defects.Count;
                info.Defects.Add(temp);
             
            }

            Defect_num = result.GetInteger("EDGE_DEFECT", "DEFECT_NUM");
           
            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("EDGE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("EDGE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out = 1;
                temp.blob_ind = info.Defects.Count;
                info.Defects.Add(temp);
             
            }


            Defect_num = result.GetInteger("SADDLE_DEFECT", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("SADDLE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("SADDLE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out = 1;
                temp.blob_ind = info.Defects.Count;
                info.Defects.Add(temp);

            }



            Defect_num = result.GetInteger("OUTSIDE_DEFECT", "DEFECT_NUM");
           
            for (int i = 0; i < Defect_num; i++)
            {
            
                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("OUTSIDE_DEFECT", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("OUTSIDE_DEFECT", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out = 0;
                temp.blob_ind = info.Defects.Count;
                info.Defects.Add(temp);

            }

            Defect_num = result.GetInteger("DF", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("DF", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("DF", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("DF", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("DF", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("DF", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out= result.GetInteger("DF", string.Format("DEFECT{0}_INOUT", i));
                temp.Defect_Location=result.GetInteger("DF", string.Format("DEFECT{0}_Defect_Location", i));
                temp.Pixel_num = result.GetInteger("DF", string.Format("DEFECT{0}_Pixel_num", i));
                temp.Average_Distance = result.GetDouble("DF", string.Format("DEFECT{0}_Average_Distance", i));
                temp.Centroid_Distance = result.GetDouble("DF", string.Format("DEFECT{0}_Centroid_Distance", i));
                temp.Long_Axis_len = result.GetDouble("DF", string.Format("DEFECT{0}_Long_Axis_len", i));
                temp.Short_Axis_len = result.GetDouble("DF", string.Format("DEFECT{0}_Short_Axis_len", i));
                temp.Long_Axis_angle = result.GetDouble("DF", string.Format("DEFECT{0}_Long_Axis_angle", i));
                temp.Short_Axis_angle = result.GetDouble("DF", string.Format("DEFECT{0}_Short_Axis_angle", i));
                temp.Avg_R = result.GetDouble("DF", string.Format("DEFECT{0}_Avg_R", i));
                temp.Avg_G = result.GetDouble("DF", string.Format("DEFECT{0}_Avg_G", i));
                temp.Avg_B = result.GetDouble("DF", string.Format("DEFECT{0}_Avg_B", i));
                temp.Avg_I = result.GetDouble("DF", string.Format("DEFECT{0}_Avg_I", i));

                info.DF_Defects.Add(temp);

            }

            Defect_num = result.GetInteger("BF", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("BF", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("BF", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("BF", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("BF", string.Format("DEFECT{0}_HEIGHT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("BF", string.Format("DEFECT{0}_ANGLE", i)));
                temp.In_Out = result.GetInteger("BF", string.Format("DEFECT{0}_INOUT", i));
                temp.Defect_Location = result.GetInteger("BF", string.Format("DEFECT{0}_Defect_Location", i));
                temp.Pixel_num = result.GetInteger("BF", string.Format("DEFECT{0}_Pixel_num", i));
                temp.Average_Distance = result.GetDouble("BF", string.Format("DEFECT{0}_Average_Distance", i));
                temp.Centroid_Distance = result.GetDouble("BF", string.Format("DEFECT{0}_Centroid_Distance", i));
                temp.Long_Axis_len = result.GetDouble("BF", string.Format("DEFECT{0}_Long_Axis_len", i));
                temp.Short_Axis_len = result.GetDouble("BF", string.Format("DEFECT{0}_Short_Axis_len", i));
                temp.Long_Axis_angle = result.GetDouble("BF", string.Format("DEFECT{0}_Long_Axis_angle", i));
                temp.Short_Axis_angle = result.GetDouble("BF", string.Format("DEFECT{0}_Short_Axis_angle", i));
                temp.Avg_R = result.GetDouble("BF", string.Format("DEFECT{0}_Avg_R", i));
                temp.Avg_G = result.GetDouble("BF", string.Format("DEFECT{0}_Avg_G", i));
                temp.Avg_B = result.GetDouble("BF", string.Format("DEFECT{0}_Avg_B", i));
                temp.Avg_I = result.GetDouble("BF", string.Format("DEFECT{0}_Avg_I", i));

                info.BF_Defects.Add(temp);

            }


            Defect_num = result.GetInteger("CO", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("CO", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("CO", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("CO", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("CO", string.Format("DEFECT{0}_HEIGHT", i));
                temp.In_Out = result.GetInteger("CO", string.Format("DEFECT{0}_INOUT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("CO", string.Format("DEFECT{0}_ANGLE", i)));
                temp.Defect_Location = result.GetInteger("CO", string.Format("DEFECT{0}_Defect_Location", i));
                temp.Pixel_num = result.GetInteger("CO", string.Format("DEFECT{0}_Pixel_num", i));
                temp.Average_Distance = result.GetDouble("CO", string.Format("DEFECT{0}S_Average_Distance", i));
                temp.Centroid_Distance = result.GetDouble("CO", string.Format("DEFECT{0}_Centroid_Distance", i));
                temp.Long_Axis_len = result.GetDouble("CO", string.Format("DEFECT{0}_Long_Axis_len", i));
                temp.Short_Axis_len = result.GetDouble("CO", string.Format("DEFECT{0}_Short_Axis_len", i));
                temp.Long_Axis_angle = result.GetDouble("CO", string.Format("DEFECT{0}_Long_Axis_angle", i));
                temp.Short_Axis_angle = result.GetDouble("CO", string.Format("DEFECT{0}_Short_Axis_angle", i));
                temp.Avg_R = result.GetDouble("CO", string.Format("DEFECT{0}_Avg_R", i));
                temp.Avg_G = result.GetDouble("CO", string.Format("DEFECT{0}_Avg_G", i));
                temp.Avg_B = result.GetDouble("CO", string.Format("DEFECT{0}_Avg_B", i));
                temp.Avg_I = result.GetDouble("CO", string.Format("DEFECT{0}_Avg_I", i));

                info.CO_Defects.Add(temp);

            }


            Defect_num = result.GetInteger("BL", "DEFECT_NUM");

            for (int i = 0; i < Defect_num; i++)
            {

                Structure.Defect_struct temp = new Structure.Defect_struct();// = new HTuple();

                temp.cenx = result.GetInteger("BL", string.Format("DEFECT{0}_CENTER_X", i));
                temp.ceny = result.GetInteger("BL", string.Format("DEFECT{0}_CENTER_Y", i));
                temp.width = result.GetInteger("BL", string.Format("DEFECT{0}_WIDTH", i));
                temp.height = result.GetInteger("BL", string.Format("DEFECT{0}_HEIGHT", i));
                temp.In_Out = result.GetInteger("BL", string.Format("DEFECT{0}_INOUT", i));
                temp.angle = Convert.ToSingle(result.GetDouble("BL", string.Format("DEFECT{0}_ANGLE", i)));
                temp.Defect_Location = result.GetInteger("BL", string.Format("DEFECT{0}_Defect_Location", i));
                temp.Pixel_num = result.GetInteger("BL", string.Format("DEFECT{0}_Pixel_num", i));
                temp.Average_Distance = result.GetDouble("BL", string.Format("DEFECT{0}S_Average_Distance", i));
                temp.Centroid_Distance = result.GetDouble("BL", string.Format("DEFECT{0}_Centroid_Distance", i));
                temp.Long_Axis_len = result.GetDouble("BL", string.Format("DEFECT{0}_Long_Axis_len", i));
                temp.Short_Axis_len = result.GetDouble("BL", string.Format("DEFECT{0}_Short_Axis_len", i));
                temp.Long_Axis_angle = result.GetDouble("BL", string.Format("DEFECT{0}_Long_Axis_angle", i));
                temp.Short_Axis_angle = result.GetDouble("BL", string.Format("DEFECT{0}_Short_Axis_angle", i));
                temp.Avg_R = result.GetDouble("BL", string.Format("DEFECT{0}_Avg_R", i));
                temp.Avg_G = result.GetDouble("BL", string.Format("DEFECT{0}_Avg_G", i));
                temp.Avg_B = result.GetDouble("BL", string.Format("DEFECT{0}_Avg_B", i));
                temp.Avg_I = result.GetDouble("BL", string.Format("DEFECT{0}_Avg_I", i));

                info.BL_Defects.Add(temp);

            }

        }
        private void GetDatResult_Mac(ref Structure.ResultInfo info)
        {

            IniReader reader = new IniReader(info.datName.FullName);
            info.Defects = new List<Structure.Defect_struct>();


            #region 매크로 결함정보
            string section = "Attach";
             int  num = reader.GetInteger(section, "DefectCount");
            for (int i = 0; i < num; i++)
            {
                Structure.Defect_struct item = new Structure.Defect_struct();
                
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
                Structure.Defect_struct item = new Structure.Defect_struct();

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
                Structure.Defect_struct item = new Structure.Defect_struct();

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
                Structure.Defect_struct item = new Structure.Defect_struct();

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
                Structure.Defect_struct item = new Structure.Defect_struct();

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
        private void DrawDefects(Structure.ResultInfo info)
        {
            HObject temp_rect;
            HOperatorSet.GenEmptyObj(out DefectRegion);
            HOperatorSet.GenEmptyObj(out DefectRegion_DF);
            HOperatorSet.GenEmptyObj(out DefectRegion_BF);
            HOperatorSet.GenEmptyObj(out DefectRegion_CO);
            HOperatorSet.GenEmptyObj(out DefectRegion_BL);

            foreach (Structure.Defect_struct d in info.Defects)
            {
                HTuple deg=new HTuple(d.angle);
               HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height/2, d.width/2);
                HOperatorSet.Union2(DefectRegion, temp_rect, out DefectRegion);
            }

            foreach (Structure.Defect_struct d in info.BF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_BF, temp_rect, out DefectRegion_BF);
            }
            foreach (Structure.Defect_struct d in info.DF_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_DF, temp_rect, out DefectRegion_DF);
            }
            foreach (Structure.Defect_struct d in info.CO_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_CO, temp_rect, out DefectRegion_CO);
            }

            foreach (Structure.Defect_struct d in info.BL_Defects) 
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.Union2(DefectRegion_BL, temp_rect, out DefectRegion_BL);
            }
        }

        private void intersection_Defect(Structure.ResultInfo info)
        {
            HObject temp_rect, intersection_rgn;
            HTuple blob_area;
            info.Real_BF_Defects = new List<Structure.Defect_struct>();
            info.Real_CO_Defects = new List<Structure.Defect_struct>();
            info.Real_DF_Defects = new List<Structure.Defect_struct>();
            info.Real_BL_Defects = new List<Structure.Defect_struct>();


            foreach (Structure.Defect_struct d in info.BF_Defects)
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

            foreach (Structure.Defect_struct d in info.DF_Defects)
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

            foreach (Structure.Defect_struct d in info.CO_Defects)
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

            foreach (Structure.Defect_struct d in info.BL_Defects)
            {
                HTuple deg = new HTuple(d.angle);
                HOperatorSet.GenEmptyObj(out temp_rect);
                HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                HOperatorSet.GenEmptyObj(out intersection_rgn);
                HOperatorSet.Intersection(select_defectRgn, temp_rect, out intersection_rgn);
                HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                if (blob_area > 1)
                {
                    info.Real_BL_Defects.Add(d);
                }
            }
        }

        private void intersection_Defect_(Structure.ResultInfo info)
        {
            HObject temp_rect, intersection_rgn;
            HTuple blob_area;
            HObject RealDefect_rgn, FasleDefct_rgn;

           // foreach (Structure.ResultInfo info in Defects_info)
            {
                info.True_BF_Defects = new List<Structure.Defect_struct>();
                info.True_CO_Defects = new List<Structure.Defect_struct>();
                info.True_DF_Defects = new List<Structure.Defect_struct>();
                info.True_BL_Defects = new List<Structure.Defect_struct>();
                info.False_BF_Defects = new List<Structure.Defect_struct>();
                info.False_CO_Defects = new List<Structure.Defect_struct>();
                info.False_DF_Defects = new List<Structure.Defect_struct>();
                info.False_BL_Defects = new List<Structure.Defect_struct>();


                foreach (Structure.Defect_struct real_d in info.True_Defects)
                {
                    HOperatorSet.GenEmptyObj(out RealDefect_rgn);
                    HOperatorSet.GenRectangle2(out RealDefect_rgn, real_d.ceny, real_d.cenx, new HTuple(real_d.angle).TupleRad(), real_d.height / 2, real_d.width / 2);

                    foreach (Structure.Defect_struct d in info.BF_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(RealDefect_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.True_BF_Defects.Add(tempstruct);
                           
                        }
                    }

                    foreach (Structure.Defect_struct d in info.DF_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(RealDefect_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.True_DF_Defects.Add(tempstruct);
                       
                        }
                    }

                    foreach (Structure.Defect_struct d in info.CO_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(RealDefect_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.True_CO_Defects.Add(tempstruct);
                        }
                    }

                    foreach (Structure.Defect_struct d in info.BL_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(RealDefect_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.True_BL_Defects.Add(tempstruct);
                        }
                    }
                }

                foreach (Structure.Defect_struct real_d in info.False_Defects)
                {
                    HOperatorSet.GenEmptyObj(out FasleDefct_rgn);
                    HOperatorSet.GenRectangle2(out FasleDefct_rgn, real_d.ceny, real_d.cenx, new HTuple(real_d.angle).TupleRad(), real_d.height / 2, real_d.width / 2);

                    foreach (Structure.Defect_struct d in info.BF_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(FasleDefct_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.False_BF_Defects.Add(tempstruct);

                        }
                    }

                    foreach (Structure.Defect_struct d in info.DF_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(FasleDefct_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.False_DF_Defects.Add(tempstruct);

                        }
                    }

                    foreach (Structure.Defect_struct d in info.CO_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(FasleDefct_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.False_CO_Defects.Add(tempstruct);
                        }
                    }
                    foreach (Structure.Defect_struct d in info.BL_Defects)
                    {
                        HTuple deg = new HTuple(d.angle);
                        HOperatorSet.GenEmptyObj(out temp_rect);
                        HOperatorSet.GenRectangle2(out temp_rect, d.ceny, d.cenx, deg.TupleRad(), d.height / 2, d.width / 2);
                        HOperatorSet.GenEmptyObj(out intersection_rgn);
                        HOperatorSet.Intersection(FasleDefct_rgn, temp_rect, out intersection_rgn);
                        HOperatorSet.RegionFeatures(intersection_rgn, "area", out blob_area);
                        if (blob_area >= 1)
                        {
                            Structure.Defect_struct tempstruct = d;
                            tempstruct.blob_ind = real_d.blob_ind;
                            tempstruct.Name = real_d.Name;
                            info.False_BL_Defects.Add(tempstruct);
                        }
                    }
                }
            }


        }



        private HObject DrawDefects_select(Structure.Defect_struct defect)
        {
            HObject temp_rect;

            HOperatorSet.GenEmptyObj(out temp_rect);
            HTuple deg = new HTuple(defect.angle);
            HOperatorSet.GenRectangle2(out temp_rect, defect.ceny, defect.cenx, deg.TupleRad(), defect.height/2, defect.width/2);

            return temp_rect;
        }

        System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2.0f);
        System.Drawing.Pen pen_false = new System.Drawing.Pen(System.Drawing.Color.Blue, 2.0f);
        private void DrawDefects_onRawImg(List<Structure.Defect_struct> defects)
        {
            d_BF = p_BF.Clone() as Bitmap;// new Bitmap(p_BF.Width, p_DF.Height, Graphics.FromImage(p_BF));
           foreach (Structure.Defect_struct defect in defects)
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
        /// <param name="Mode">0: BF, 1:DF, 2:CO, 3:BL</param>
        private void DrawDefects_onRawImg(List<Structure.Defect_struct> defects, int Mode, bool Truedetect=true)
        {

            if (Mode == 0)
            {
                d_BF = p_BF.Clone() as Bitmap;
                using (Graphics g = Graphics.FromImage(d_BF))
                {
                    foreach (Structure.Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        if(Truedetect)g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        else g.DrawRectangle(pen_false, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
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
                    foreach (Structure.Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        if (Truedetect) g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        else g.DrawRectangle(pen_false, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
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
                    foreach (Structure.Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        if (Truedetect) g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        else g.DrawRectangle(pen_false, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                    }
                }
            }
            else if (Mode == 3)
            {
                d_BL = p_BL.Clone() as Bitmap;
                using (Graphics g = Graphics.FromImage(d_BL))
                {
                    foreach (Structure.Defect_struct defect in defects)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                        if (Truedetect) g.DrawRectangle(pen, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        else g.DrawRectangle(pen_false, (int)(defect.cenx - (defect.width / 2)), (int)(defect.ceny - (defect.height / 2)), (int)defect.width, (int)defect.height);
                        m.RotateAt((float)-defect.angle, new PointF((float)defect.cenx, (float)defect.ceny));
                        g.Transform = m;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defects"></param>
        /// <param name="Mode">0: BF, 1:DF, 2:CO</param>
        private void DrawDefects_onRawImg(Bitmap DstImg, List<Structure.Defect_struct> defect, int Mode, bool Truedetect = true)
        {
            List<Structure.Defect_struct> temp_struct;
            using (Graphics g = Graphics.FromImage(DstImg))
            {
               // temp_struct = new List<Structure.Defect_struct>();

               //// foreach (Structure.ResultInfo defect in defects)
               // {
               //     temp_struct.Clear();

               //     if (Mode == 0)
               //     {
               //         temp_struct = defect.Real_BF_Defects;
               //     }
               //     else if (Mode == 1)
               //     {
               //         temp_struct = defect.Real_DF_Defects;
               //     }
               //     else
               //     {
               //         temp_struct = defect.Real_CO_Defects;
               //     }

                    foreach (Structure.Defect_struct mode_defect in defect)
                    {
                        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
                        m.RotateAt((float)mode_defect.angle, new PointF((float)mode_defect.cenx, (float)mode_defect.ceny));
                        g.Transform = m;
                        if (Truedetect) g.DrawRectangle(pen, (int)(mode_defect.cenx - (mode_defect.width / 2)), (int)(mode_defect.ceny - (mode_defect.height / 2)), (int)mode_defect.width, (int)mode_defect.height);
                        else g.DrawRectangle(pen_false, (int)(mode_defect.cenx - (mode_defect.width / 2)), (int)(mode_defect.ceny - (mode_defect.height / 2)), (int)mode_defect.width, (int)mode_defect.height);
                        m.RotateAt((float)-mode_defect.angle, new PointF((float)mode_defect.cenx, (float)mode_defect.ceny));
                        g.Transform = m;
                    }
                }
            //}
        }

        private void Defects_lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            var select_ind = list.SelectedItems;
           
            HOperatorSet.GenEmptyObj(out select_defectRgn);
            foreach (var temp in select_ind)
            {
                int  t = Convert.ToInt32(temp.ToString().Split('-')[0]);

               HObject select=  DrawDefects_select(infos[Current_i].Defects[t]);
                HOperatorSet.Union2(select_defectRgn, select, out select_defectRgn);
            }

            UpdateWindow(0);
          
        }

        private void Prev_Btn_Click(object sender, RoutedEventArgs e)
        {
            string[] tempName = RootAdderss.Split('\\');
            string ModelName = tempName[tempName.Length - 1];
            Button click_btn = sender as Button;
            int befor_i = Current_i;
            int noDefect = 0;
            if (click_btn != null)
            {
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
            }
            else
            {
                Current_i = cmb_ImgList.SelectedIndex;
            }
            try
            {
                var select_ind = Defects_lst.SelectedItems;

                /*if (infos[befor_i].True_Defects == null)*/
                infos[befor_i].True_Defects = new List<Structure.Defect_struct>();
                /*if (infos[befor_i].False_Defects == null) */
                infos[befor_i].False_Defects = new List<Structure.Defect_struct>();
                /* if (infos[befor_i].Under_Defects == null) */
                infos[befor_i].Under_Defects = new List<Structure.Defect_struct>();



                if (select_ind.Count > 0)
                {
                    if (Defects_lst.Items.Contains("결함 없음."))
                    {
                        noDefect = 1;
                    }

                    for (int list_i = 0; list_i < Defects_lst.Items.Count; list_i++)
                    {
                        int newind = list_i - noDefect;
                        bool selected = false;//= select_ind.Contains(list_i.ToString());
                        for (int ch = 0; ch < select_ind.Count; ch++)
                        {
                            selected = select_ind[ch].ToString().Contains(newind.ToString());
                            if (selected) break;
                        }
                        if (selected)
                        {
                            Structure.Defect_struct temp_struct = infos[befor_i].Defects[newind];
                            temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[newind].ToString().Split('-')[1]);
                            temp_struct.GroundTruth = 1;
                            infos[befor_i].True_Defects.Add(temp_struct);
                            infos[befor_i].Defects[list_i] = temp_struct;
                            //infos[befor_i].True_Defects.Add(infos[befor_i].Defects[list_i]);
                        }
                        else
                        {
                            Structure.Defect_struct temp_struct = infos[befor_i].Defects[newind];
                            if (temp_struct.UnderDefect == false)
                            {
                                temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[newind].ToString().Split('-')[1]);
                                temp_struct.GroundTruth = -1;
                                infos[befor_i].False_Defects.Add(temp_struct);
                                infos[befor_i].Defects[list_i] = temp_struct;
                            }
                            else
                            {
                                temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[newind].ToString().Split('-')[1]);
                                temp_struct.GroundTruth = 2;
                                infos[befor_i].Under_Defects.Add(temp_struct);
                                infos[befor_i].Defects[list_i] = temp_struct;
                            }
                            //infos[befor_i].False_Defects.Add(infos[befor_i].Defects[list_i]);
                        }
                    }

                    intersection_Defect_(infos[befor_i]);

                    if (!Directory.Exists(RootAdderss + "\\" + ModelName)) Directory.CreateDirectory(RootAdderss + "\\" + ModelName);

                    saveGrt_withFeature_(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].datName.Name.Replace("dat", "txt"), infos[befor_i]);

                    //intersection_Defect(infos[befor_i]);
                    d_BF = p_BF.Clone() as Bitmap;
                    d_DF = p_DF.Clone() as Bitmap;
                    d_CO = p_CO.Clone() as Bitmap;
                    d_BL = p_BL.Clone() as Bitmap;

                    DrawDefects_onRawImg(d_BF, infos[befor_i].True_BF_Defects, 0);
                    DrawDefects_onRawImg(d_DF, infos[befor_i].True_DF_Defects, 1);
                    DrawDefects_onRawImg(d_CO, infos[befor_i].True_CO_Defects, 2);
                    DrawDefects_onRawImg(d_BL, infos[befor_i].True_BL_Defects, 3);

                    DrawDefects_onRawImg(d_BF, infos[befor_i].False_BF_Defects, 0, false);
                    DrawDefects_onRawImg(d_DF, infos[befor_i].False_DF_Defects, 1, false);
                    DrawDefects_onRawImg(d_CO, infos[befor_i].False_CO_Defects, 2, false);
                    DrawDefects_onRawImg(d_BL, infos[befor_i].False_BL_Defects, 3, false);

                    p_BF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bf, ImageFormat.Bmp);
                    p_DF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_df, ImageFormat.Bmp);
                    p_CO.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_co, ImageFormat.Bmp);
                    p_BL.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bl, ImageFormat.Bmp);

                    if (infos[befor_i].BF_Defects.Count != 0) d_BF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bf_defect, ImageFormat.Bmp);
                    if (infos[befor_i].DF_Defects.Count != 0) d_DF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_df_defect, ImageFormat.Bmp);
                    if (infos[befor_i].CO_Defects.Count != 0) d_CO.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_co_defect, ImageFormat.Bmp);
                    if (infos[befor_i].BL_Defects.Count != 0) d_BL.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bl_defect, ImageFormat.Bmp);

                    //saveGrt(infos[befor_i]);


                }
                else
                {
                    if (Defects_lst.Items.Contains("결함 없음."))
                    {
                        if (Defects_lst.Items.Count == 1)
                        {
                            if (!Directory.Exists(RootAdderss + "\\" + ModelName)) Directory.CreateDirectory(RootAdderss + "\\" + ModelName);

                            p_BF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bf, ImageFormat.Bmp);
                            p_DF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_df, ImageFormat.Bmp);
                            p_CO.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_co, ImageFormat.Bmp);
                            p_BL.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bl, ImageFormat.Bmp);
                            //d_BF.Save(RootAdderss + "\\OK_Image\\" + infos[befor_i].ImageName_bf_defect, ImageFormat.Bmp);
                            saveGrt_withFeature_(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].datName.Name.Replace("dat", "txt"), infos[befor_i]);
                        }
                        else
                        {
                            for (int list_i = 1; list_i < Defects_lst.Items.Count; list_i++)
                            {
                                int newind = list_i - 1;
                                
                               // for (int ch = 0; ch < select_ind.Count; ch++)
                                {
                                    Structure.Defect_struct temp_struct = infos[befor_i].Defects[newind];
                                    temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[list_i].ToString().Split('-')[1]);
                                    temp_struct.GroundTruth = 2;
                                    infos[befor_i].Under_Defects.Add(temp_struct);
                                    infos[befor_i].Defects[newind] = temp_struct;
                                }
                            }
                            saveGrt_withFeature_(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].datName.Name.Replace("dat", "txt"), infos[befor_i]);
                        }
                    }
                    else
                    {
                        if (infos[befor_i].Under_Defects == null)
                        {
                            for (int list_i = 0; list_i < Defects_lst.Items.Count; list_i++)
                            {
                                {
                                    Structure.Defect_struct temp_struct = infos[befor_i].Defects[list_i];
                                    if (temp_struct.UnderDefect == false)
                                    {
                                        temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[list_i].ToString().Split('-')[1]);
                                        temp_struct.GroundTruth = -1;
                                        infos[befor_i].False_Defects.Add(temp_struct);
                                        infos[befor_i].Defects[list_i] = temp_struct;
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int list_i = 0; list_i < Defects_lst.Items.Count; list_i++)
                            {
                                string item_ = Defects_lst.Items[list_i].ToString();
                                if (!item_.Contains("Under"))
                                {
                                    Structure.Defect_struct temp_struct = infos[befor_i].Defects[list_i];
                                    if (temp_struct.UnderDefect == false)
                                    {
                                        temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[list_i].ToString().Split('-')[1]);
                                        temp_struct.GroundTruth = -1;
                                        infos[befor_i].False_Defects.Add(temp_struct);
                                        infos[befor_i].Defects[list_i] = temp_struct;
                                    }
                                }
                                else
                                {
                                    Structure.Defect_struct temp_struct = infos[befor_i].Defects[list_i];
                                    temp_struct.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), Defects_lst.Items[list_i].ToString().Split('-')[1]);
                                    temp_struct.GroundTruth = 2;
                                    infos[befor_i].Under_Defects.Add(temp_struct);
                                    infos[befor_i].Defects[list_i] = temp_struct;
                                }
                            }

                        }

                        intersection_Defect_(infos[befor_i]);


                        saveGrt_withFeature_(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].datName.Name.Replace("dat", "txt"), infos[befor_i]);

                        //intersection_Defect(infos[befor_i]);
                        d_BF = p_BF.Clone() as Bitmap;
                        d_DF = p_DF.Clone() as Bitmap;
                        d_CO = p_CO.Clone() as Bitmap;
                        d_BL = p_CO.Clone() as Bitmap;

                        DrawDefects_onRawImg(d_BF, infos[befor_i].True_BF_Defects, 0);
                        DrawDefects_onRawImg(d_DF, infos[befor_i].True_DF_Defects, 1);
                        DrawDefects_onRawImg(d_CO, infos[befor_i].True_CO_Defects, 2);
                        DrawDefects_onRawImg(d_BL, infos[befor_i].True_BL_Defects, 2);

                        DrawDefects_onRawImg(d_BF, infos[befor_i].False_BF_Defects, 0, false);
                        DrawDefects_onRawImg(d_DF, infos[befor_i].False_DF_Defects, 1, false);
                        DrawDefects_onRawImg(d_CO, infos[befor_i].False_CO_Defects, 2, false);
                        DrawDefects_onRawImg(d_BL, infos[befor_i].False_BL_Defects, 2, false);

                        if (!Directory.Exists(RootAdderss + "\\" + ModelName + "\\")) Directory.CreateDirectory(RootAdderss + "\\" + ModelName);

                        p_BF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bf, ImageFormat.Bmp);
                        p_DF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_df, ImageFormat.Bmp);
                        p_CO.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_co, ImageFormat.Bmp);
                        p_BL.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bl, ImageFormat.Bmp);
                        if (infos[befor_i].BF_Defects.Count != 0) d_BF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bf_defect, ImageFormat.Bmp);
                        if (infos[befor_i].DF_Defects.Count != 0) d_DF.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_df_defect, ImageFormat.Bmp);
                        if (infos[befor_i].CO_Defects.Count != 0) d_CO.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_co_defect, ImageFormat.Bmp);
                        if (infos[befor_i].BL_Defects.Count != 0) d_BL.Save(RootAdderss + "\\" + ModelName + "\\" + infos[befor_i].ImageName_bl_defect, ImageFormat.Bmp);
                    }
                }
                //  SaveContents(infos[befor_i]);
                AddList(infos[Current_i]);
                ImageName_lbl.Content = infos[Current_i].datName.Name;
                Convert_Image_Mic(infos[Current_i].CH0, infos[Current_i].CH1, infos[Current_i].CH2, infos[Current_i].CH3);
                DrawDefects(infos[Current_i]);

                UpdateWindow(0);
            }
            catch
            {
                Writer.Close();
                Current_i = befor_i;
            }
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
        private void saveGrt(Structure.ResultInfo res)
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

            for (int i = 0; i < res.Real_BL_Defects.Count; i++)
            {
                string Defect_centerx = string.Format("DEFECT{0}_CENTER_X", i);
                string Defect_centery = string.Format("DEFECT{0}_CENTER_Y", i);
                string Defect_w = string.Format("DEFECT{0}_WIDTH", i);
                string Defect_h = string.Format("DEFECT{0}_HEIGHT", i);
                string Defect_a = string.Format("DEFECT{0}_ANGLE", i);

                grt.SetString("BL_DEFECT_INFO", Defect_centerx, res.Real_BL_Defects[i].cenx.ToString());
                grt.SetString("BL_DEFECT_INFO", Defect_centery, res.Real_BL_Defects[i].ceny.ToString());
                grt.SetString("BL_DEFECT_INFO", Defect_w, res.Real_BL_Defects[i].width.ToString());
                grt.SetString("BL_DEFECT_INFO", Defect_h, res.Real_BL_Defects[i].height.ToString());
                grt.SetString("BL_DEFECT_INFO", Defect_a, res.Real_BL_Defects[i].angle.ToString());
            }
        }

        private void saveGrt_withFeature(string SaveAddress, List<Structure.ResultInfo> TrueDefect, List<Structure.ResultInfo> FalseDefect)
        {
            if (File.Exists(SaveAddress)) File.Delete(SaveAddress);
            IniReader grt = new IniReader(SaveAddress);

            int bf_total_num=0, df_total_num = 0, cx_total_num = 0, bl_total_num=0;

            foreach (Structure.ResultInfo defect in TrueDefect)
            {
                bf_total_num += defect.True_BF_Defects.Count;
                df_total_num += defect.True_DF_Defects.Count;
                cx_total_num += defect.True_CO_Defects.Count;
                bl_total_num += defect.True_BL_Defects.Count;
            }
            foreach (Structure.ResultInfo defect in FalseDefect)
            {
                bf_total_num += defect.False_BF_Defects.Count;
                df_total_num += defect.False_DF_Defects.Count;
                cx_total_num += defect.False_CO_Defects.Count;
                bl_total_num += defect.False_BL_Defects.Count;

            }
            grt.SetString("ImageResult", "GroundTruth", TrueDefect.Count != 0 ? "1" : "0");
            grt.SetString("ImageResult", "Blob_num_at_BF", TrueDefect[0].BF_Defects.Count().ToString());
            grt.SetString("ImageResult", "Blob_num_at_DF", TrueDefect[0].DF_Defects.Count().ToString());
            grt.SetString("ImageResult", "Blob_num_at_CX", TrueDefect[0].CO_Defects.Count().ToString());
            grt.SetString("ImageResult", "Blob_num_at_BL", TrueDefect[0].BL_Defects.Count().ToString());

            for (int i = 0; i < TrueDefect.Count; ++i)
            {
                string Defect_centerx = string.Format("Blob{0}_CENTER_X", i);
                string Defect_centery = string.Format("Blob{0}_CENTER_Y", i);
                string Defect_w = string.Format("Blob{0}_WIDTH", i);
                string Defect_h = string.Format("Blob{0}_HEIGHT", i);
                string Defect_a = string.Format("Blob{0}_ANGLE", i);

                string True_Defect=string.Format("Blob{0}_TrueDefect", i);
                string BF = string.Format("Blob{0}_BF", i);
                string DF = string.Format("Blob{0}_DF", i);
                string CX = string.Format("Blob{0}_CX", i);
                string BL = string.Format("Blob{0}_BL", i);

                string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);
                string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i);

                string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);
                string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i);

                string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);
                string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i);

                string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);
                string _Average_Distance_BL= string.Format("Blob{0}_Average_Distance_BL", i);

                string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);
                string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i);

                string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);
                string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i);

                string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                string _Short_Axis_CX= string.Format("Blob{0}_Short_Axis_CX", i);
                string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i);

                string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);
                string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i);
            
                string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);
                string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i);

                string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);
                string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i);

                string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);
                string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i);

                string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);
                string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i);

                string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);
                string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i);


                grt.SetString("Blob_Info", Defect_centerx, TrueDefect[i].Defects[0].cenx.ToString("F3"));
                grt.SetString("Blob_Info", Defect_centery, TrueDefect[i].Defects[0].ceny.ToString("F3"));
                grt.SetString("Blob_Info", Defect_w,TrueDefect[i].Defects[0].width.ToString("F3"));
                grt.SetString("Blob_Info", Defect_h, TrueDefect[i].Defects[0].height.ToString("F3"));
                grt.SetString("Blob_Info", Defect_a, TrueDefect[i].Defects[0].angle.ToString("F3"));


                grt.SetString("Blob_Info", True_Defect, "1.000");
                grt.SetString("Blob_info", BF, TrueDefect[i].Real_BF_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", DF, TrueDefect[i].Real_DF_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", CX, TrueDefect[i].Real_CO_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", BL, TrueDefect[i].Real_BL_Defects.Count.ToString("F3"));


                grt.SetString("Blob_info", _INOUT_at_BF, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_DF, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_CX, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_BL, TrueDefect[i].Defects[0].In_Out.ToString("F3"));


                grt.SetString("Blob_info", _Defect_Location_BF, TrueDefect[i].Real_BF_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_DF, TrueDefect[i].Real_DF_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_CX, TrueDefect[i].Real_CO_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_BL, TrueDefect[i].Real_BL_Defects[0].Defect_Location.ToString("F3"));

                grt.SetString("Blob_info", _Pixel_num_BF, TrueDefect[i].Real_BF_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_DF, TrueDefect[i].Real_DF_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_CX, TrueDefect[i].Real_CO_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_BL, TrueDefect[i].Real_BL_Defects[0].Pixel_num.ToString("F3"));


                grt.SetString("Blob_info", _Average_Distance_BF, TrueDefect[i].Real_BF_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_DF, TrueDefect[i].Real_DF_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_CX, TrueDefect[i].Real_CO_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_BL, TrueDefect[i].Real_BL_Defects[0].Average_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Centroid_Distance_BF, TrueDefect[i].Real_BF_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_DF, TrueDefect[i].Real_DF_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_CX, TrueDefect[i].Real_CO_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_BL, TrueDefect[i].Real_BL_Defects[0].Centroid_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Long_Axis_BF, TrueDefect[i].Real_BF_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_DF, TrueDefect[i].Real_DF_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_CX, TrueDefect[i].Real_CO_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_BL, TrueDefect[i].Real_BL_Defects[0].Long_Axis_len.ToString("F3"));

                grt.SetString("Blob_info", _Short_Axis_BF, TrueDefect[i].Real_BF_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_DF, TrueDefect[i].Real_DF_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_CX, TrueDefect[i].Real_CO_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_BL, TrueDefect[i].Real_BL_Defects[0].Short_Axis_len.ToString("F3"));

                grt.SetString("Blob_info", _Average_R_BF, TrueDefect[i].Real_BF_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_DF, TrueDefect[i].Real_DF_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_CX, TrueDefect[i].Real_CO_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_BL, TrueDefect[i].Real_BL_Defects[0].Avg_R.ToString("F3"));

                grt.SetString("Blob_info", _Average_G_BF, TrueDefect[i].Real_BF_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_DF, TrueDefect[i].Real_DF_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_CX, TrueDefect[i].Real_CO_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_BL, TrueDefect[i].Real_BL_Defects[0].Avg_G.ToString("F3"));

                grt.SetString("Blob_info", _Average_B_BF, TrueDefect[i].Real_BF_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_DF, TrueDefect[i].Real_DF_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_CX, TrueDefect[i].Real_CO_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_BL, TrueDefect[i].Real_BL_Defects[0].Avg_B.ToString("F3"));


                grt.SetString("Blob_info", _Average_I_BF, TrueDefect[i].Real_BF_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_DF, TrueDefect[i].Real_DF_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_CX, TrueDefect[i].Real_CO_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_BL, TrueDefect[i].Real_BL_Defects[0].Avg_I.ToString("F3"));
            }


            for (int i = 0; i < FalseDefect.Count; ++i)
            {
                string Defect_centerx = string.Format("Blob{0}_CENTER_X", i);
                string Defect_centery = string.Format("Blob{0}_CENTER_Y", i);
                string Defect_w = string.Format("Blob{0}_WIDTH", i);
                string Defect_h = string.Format("Blob{0}_HEIGHT", i);
                string Defect_a = string.Format("Blob{0}_ANGLE", i);

                string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                string BF = string.Format("Blob{0}_BF", i);
                string DF = string.Format("Blob{0}_DF", i);
                string CX = string.Format("Blob{0}_CX", i);
                string BL = string.Format("Blob{0}_BL", i);

                string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);
                string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i);

                string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);
                string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i);

                string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);
                string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i);

                string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);
                string _Average_Distance_BL = string.Format("Blob{0}_Average_Distance_BL", i);

                string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);
                string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i);

                string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);
                string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i);

                string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);
                string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i);

                string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);
                string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i);

                string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);
                string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i);

                string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);
                string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i);

                string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);
                string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i);

                string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);
                string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i);

                string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);
                string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i);





                grt.SetString("Blob_Info", Defect_centerx, TrueDefect[i].Defects[0].cenx.ToString("F3"));
                grt.SetString("Blob_Info", Defect_centery, TrueDefect[i].Defects[0].ceny.ToString("F3"));
                grt.SetString("Blob_Info", Defect_w, TrueDefect[i].Defects[0].width.ToString("F3"));
                grt.SetString("Blob_Info", Defect_h, TrueDefect[i].Defects[0].height.ToString("F3"));
                grt.SetString("Blob_Info", Defect_a, TrueDefect[i].Defects[0].angle.ToString("F3"));


                grt.SetString("Blob_Info", True_Defect, "0");
                grt.SetString("Blob_info", BF, TrueDefect[i].Real_BF_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", DF, TrueDefect[i].Real_DF_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", CX, TrueDefect[i].Real_CO_Defects.Count.ToString("F3"));
                grt.SetString("Blob_info", BL, TrueDefect[i].Real_BL_Defects.Count.ToString("F3"));

                grt.SetString("Blob_info", _INOUT_at_BF, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_DF, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_CX, TrueDefect[i].Defects[0].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_BL, TrueDefect[i].Defects[0].In_Out.ToString("F3"));

                grt.SetString("Blob_info", _Defect_Location_BF, TrueDefect[i].Real_BF_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_DF, TrueDefect[i].Real_DF_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_CX, TrueDefect[i].Real_CO_Defects[0].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_BL, TrueDefect[i].Real_BL_Defects[0].Defect_Location.ToString("F3"));


                grt.SetString("Blob_info", _Pixel_num_BF, TrueDefect[i].Real_BF_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_DF, TrueDefect[i].Real_DF_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_CX, TrueDefect[i].Real_CO_Defects[0].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_BL, TrueDefect[i].Real_BL_Defects[0].Pixel_num.ToString("F3"));

                grt.SetString("Blob_info", _Average_Distance_BF, TrueDefect[i].Real_BF_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_DF, TrueDefect[i].Real_DF_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_CX, TrueDefect[i].Real_CO_Defects[0].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_BL, TrueDefect[i].Real_BL_Defects[0].Average_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Centroid_Distance_BF, TrueDefect[i].Real_BF_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_DF, TrueDefect[i].Real_DF_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_CX, TrueDefect[i].Real_CO_Defects[0].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_BL, TrueDefect[i].Real_BL_Defects[0].Centroid_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Long_Axis_BF, TrueDefect[i].Real_BF_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_DF, TrueDefect[i].Real_DF_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_CX, TrueDefect[i].Real_CO_Defects[0].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_BL, TrueDefect[i].Real_BL_Defects[0].Long_Axis_len.ToString("F3"));

                grt.SetString("Blob_info", _Short_Axis_BF, TrueDefect[i].Real_BF_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_DF, TrueDefect[i].Real_DF_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_CX, TrueDefect[i].Real_CO_Defects[0].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_BL, TrueDefect[i].Real_BL_Defects[0].Short_Axis_len.ToString("F3"));

                grt.SetString("Blob_info", _Average_R_BF, TrueDefect[i].Real_BF_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_DF, TrueDefect[i].Real_DF_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_CX, TrueDefect[i].Real_CO_Defects[0].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_BL, TrueDefect[i].Real_BL_Defects[0].Avg_R.ToString("F3"));

                grt.SetString("Blob_info", _Average_G_BF, TrueDefect[i].Real_BF_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_DF, TrueDefect[i].Real_DF_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_CX, TrueDefect[i].Real_CO_Defects[0].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_BL, TrueDefect[i].Real_BL_Defects[0].Avg_G.ToString("F3"));

                grt.SetString("Blob_info", _Average_B_BF, TrueDefect[i].Real_BF_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_DF, TrueDefect[i].Real_DF_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_CX, TrueDefect[i].Real_CO_Defects[0].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_BL, TrueDefect[i].Real_BL_Defects[0].Avg_B.ToString("F3"));

                grt.SetString("Blob_info", _Average_I_BF, TrueDefect[i].Real_BF_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_DF, TrueDefect[i].Real_DF_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_CX, TrueDefect[i].Real_CO_Defects[0].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_BL, TrueDefect[i].Real_BL_Defects[0].Avg_I.ToString("F3"));
            }
        }


        private void saveGrt_withFeature(string SaveAddress, Structure.ResultInfo Defects)
        {
            if (File.Exists(SaveAddress)) File.Delete(SaveAddress);
            IniReader grt = new IniReader(SaveAddress);

            

           // foreach (Structure.ResultInfo defect in Defects)
            {

                grt.SetString("ImageResult", "GroundTruth", Defects.True_Defects.Count != 0 ? "1.000" : "0.000");
                grt.SetString("ImageResult", "Blob_num_at_BF", Defects.BF_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_DF", Defects.DF_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_CX", Defects.CO_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_BL", Defects.BL_Defects.Count().ToString("F3"));
            }


            if (Defects.Defects.Count> 0)
            {

                for (int i = 0; i < Defects.True_Defects.Count; i++)
                {
                    string Defect_centerx = string.Format("Blob{0}_CENTER_X", i);
                    string Defect_centery = string.Format("Blob{0}_CENTER_Y", i);
                    string Defect_w = string.Format("Blob{0}_WIDTH", i);
                    string Defect_h = string.Format("Blob{0}_HEIGHT", i);
                    string Defect_a = string.Format("Blob{0}_ANGLE", i);

                    string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                    string BF = string.Format("Blob{0}_BF", i);
                    string DF = string.Format("Blob{0}_DF", i);
                    string CX = string.Format("Blob{0}_CX", i);
                    string BL = string.Format("Blob{0}_BL", i);

                    string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                    string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                    string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);
                    string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i);

                    string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                    string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                    string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);
                    string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i);

                    string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                    string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                    string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);
                    string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i);

                    string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                    string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                    string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);
                    string _Average_Distance_BL = string.Format("Blob{0}_Average_Distance_BL", i);

                    string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                    string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                    string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);
                    string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i);

                    string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                    string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                    string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);
                    string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i);

                    string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                    string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                    string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);
                    string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i);

                    string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                    string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                    string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);
                    string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i);

                    string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                    string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                    string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);
                    string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i);

                    string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                    string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                    string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);
                    string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i);

                    string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                    string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                    string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);
                    string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i);

                    string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                    string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                    string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);
                    string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i);

                    string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                    string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                    string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);
                    string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i);


                    grt.SetString("Blob_Info", Defect_centerx, Defects.True_Defects[i].cenx.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_centery, Defects.True_Defects[i].ceny.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_w, Defects.True_Defects[i].width.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_h, Defects.True_Defects[i].height.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_a, Defects.True_Defects[i].angle.ToString("F3"));


                    grt.SetString("Blob_Info", True_Defect, "1.000");


                    Structure.Defect_struct temp_Bf_struct = Defects.True_BF_Defects.Find(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    Structure.Defect_struct temp_Df_struct = Defects.True_DF_Defects.Find(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    Structure.Defect_struct temp_Co_struct = Defects.True_CO_Defects.Find(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    Structure.Defect_struct temp_Bl_struct = Defects.True_BL_Defects.Find(x => x.blob_ind == Defects.True_Defects[i].blob_ind);

                    grt.SetString("Blob_info", BF, Defects.True_BF_Defects.Count(x => x.blob_ind == Defects.True_Defects[i].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", DF, Defects.True_DF_Defects.Count(x => x.blob_ind == Defects.True_Defects[i].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", CX, Defects.True_CO_Defects.Count(x => x.blob_ind == Defects.True_Defects[i].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", BL, Defects.True_BL_Defects.Count(x => x.blob_ind == Defects.True_Defects[i].blob_ind) != 0 ? "1.000" : "-1.000");


                    grt.SetString("Blob_info", _INOUT_at_BF, temp_Bf_struct.In_Out.ToString("F3"));
                    grt.SetString("Blob_info", _INOUT_at_DF, temp_Df_struct.In_Out.ToString("F3"));
                    grt.SetString("Blob_info", _INOUT_at_CX, temp_Co_struct.In_Out.ToString("F3"));

                    grt.SetString("Blob_info", _Defect_Location_BF, temp_Bf_struct.Defect_Location.ToString("F3"));
                    grt.SetString("Blob_info", _Defect_Location_DF, temp_Df_struct.Defect_Location.ToString("F3"));
                    grt.SetString("Blob_info", _Defect_Location_CX, temp_Co_struct.Defect_Location.ToString("F3"));


                    grt.SetString("Blob_info", _Pixel_num_BF, temp_Bf_struct.Pixel_num.ToString("F3"));
                    grt.SetString("Blob_info", _Pixel_num_DF, temp_Df_struct.Pixel_num.ToString("F3"));
                    grt.SetString("Blob_info", _Pixel_num_CX, temp_Co_struct.Pixel_num.ToString("F3"));

                    grt.SetString("Blob_info", _Average_Distance_BF, temp_Bf_struct.Average_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Average_Distance_DF, temp_Df_struct.Average_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Average_Distance_CX, temp_Co_struct.Average_Distance.ToString("F3"));


                    grt.SetString("Blob_info", _Centroid_Distance_BF, temp_Bf_struct.Centroid_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Centroid_Distance_DF, temp_Df_struct.Centroid_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Centroid_Distance_CX, temp_Co_struct.Centroid_Distance.ToString("F3"));

                    grt.SetString("Blob_info", _Long_Axis_BF, temp_Bf_struct.Long_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Long_Axis_DF, temp_Df_struct.Long_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Long_Axis_CX, temp_Co_struct.Long_Axis_len.ToString("F3"));

                    grt.SetString("Blob_info", _Short_Axis_BF, temp_Bf_struct.Short_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Short_Axis_DF, temp_Df_struct.Short_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Short_Axis_CX, temp_Co_struct.Short_Axis_len.ToString("F3"));


                    grt.SetString("Blob_info", _Average_R_BF, temp_Bf_struct.Avg_R.ToString("F3"));
                    grt.SetString("Blob_info", _Average_R_DF, temp_Df_struct.Avg_R.ToString("F3"));
                    grt.SetString("Blob_info", _Average_R_CX, temp_Co_struct.Avg_R.ToString("F3"));

                    grt.SetString("Blob_info", _Average_G_BF, temp_Bf_struct.Avg_G.ToString("F3"));
                    grt.SetString("Blob_info", _Average_G_DF, temp_Df_struct.Avg_G.ToString("F3"));
                    grt.SetString("Blob_info", _Average_G_CX, temp_Co_struct.Avg_G.ToString("F3"));

                    grt.SetString("Blob_info", _Average_B_BF, temp_Bf_struct.Avg_B.ToString("F3"));
                    grt.SetString("Blob_info", _Average_B_DF, temp_Df_struct.Avg_B.ToString("F3"));
                    grt.SetString("Blob_info", _Average_B_CX, temp_Co_struct.Avg_B.ToString("F3"));

                    grt.SetString("Blob_info", _Average_I_BF, temp_Bf_struct.Avg_I.ToString("F3"));
                    grt.SetString("Blob_info", _Average_I_DF, temp_Df_struct.Avg_I.ToString("F3"));
                    grt.SetString("Blob_info", _Average_I_CX, temp_Co_struct.Avg_I.ToString("F3"));
                }


                for (int j = 0; j < Defects.False_Defects.Count; j++)
                {
                    int i = Defects.True_Defects.Count + j;

                    string Defect_centerx = string.Format("Blob{0}_CENTER_X", i);
                    string Defect_centery = string.Format("Blob{0}_CENTER_Y", i);
                    string Defect_w = string.Format("Blob{0}_WIDTH", i);
                    string Defect_h = string.Format("Blob{0}_HEIGHT", i);
                    string Defect_a = string.Format("Blob{0}_ANGLE", i);

                    string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                    string BF = string.Format("Blob{0}_BF", i);
                    string DF = string.Format("Blob{0}_DF", i);
                    string CX = string.Format("Blob{0}_CX", i);
                    string BL = string.Format("Blob{0}_BL", i);

                    string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                    string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                    string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);
                    string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i);

                    string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                    string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                    string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);
                    string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i);

                    string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                    string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                    string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);
                    string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i);

                    string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                    string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                    string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);
                    string _Average_Distance_BL = string.Format("Blob{0}_Average_Distance_BL", i);

                    string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                    string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                    string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);
                    string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i);

                    string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                    string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                    string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);
                    string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i);

                    string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                    string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                    string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);
                    string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i);

                    string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                    string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                    string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);
                    string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i);

                    string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                    string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                    string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);
                    string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i);

                    string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                    string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                    string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);
                    string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i);

                    string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                    string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                    string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);
                    string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i);

                    string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                    string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                    string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);
                    string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i);

                    string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                    string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                    string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);
                    string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i);


                    grt.SetString("Blob_Info", Defect_centerx, Defects.False_Defects[j].cenx.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_centery, Defects.False_Defects[j].ceny.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_w, Defects.False_Defects[j].width.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_h, Defects.False_Defects[j].height.ToString("F3"));
                    grt.SetString("Blob_Info", Defect_a, Defects.False_Defects[j].angle.ToString("F3"));


                    Structure.Defect_struct temp_Bf_struct = Defects.False_BF_Defects.Find(x => x.blob_ind == Defects.False_Defects[j].blob_ind);
                    Structure.Defect_struct temp_Df_struct = Defects.False_DF_Defects.Find(x => x.blob_ind == Defects.False_Defects[j].blob_ind);
                    Structure.Defect_struct temp_Co_struct = Defects.False_CO_Defects.Find(x => x.blob_ind == Defects.False_Defects[j].blob_ind);
                    Structure.Defect_struct temp_Bl_struct = Defects.False_BL_Defects.Find(x => x.blob_ind == Defects.False_Defects[j].blob_ind);

                    grt.SetString("Blob_Info", True_Defect, "-1.000");
                    grt.SetString("Blob_info", BF, Defects.False_BF_Defects.Count(x => x.blob_ind == Defects.False_Defects[j].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", DF, Defects.False_DF_Defects.Count(x => x.blob_ind == Defects.False_Defects[j].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", CX, Defects.False_CO_Defects.Count(x => x.blob_ind == Defects.False_Defects[j].blob_ind) != 0 ? "1.000" : "-1.000");
                    grt.SetString("Blob_info", BL, Defects.False_BL_Defects.Count(x => x.blob_ind == Defects.False_Defects[j].blob_ind) != 0 ? "1.000" : "-1.000");

                    grt.SetString("Blob_info", _INOUT_at_BF, temp_Bf_struct.In_Out.ToString("F3"));
                    grt.SetString("Blob_info", _INOUT_at_DF, temp_Df_struct.In_Out.ToString("F3"));
                    grt.SetString("Blob_info", _INOUT_at_CX, temp_Co_struct.In_Out.ToString("F3"));

                    grt.SetString("Blob_info", _Defect_Location_BF, temp_Bf_struct.Defect_Location.ToString("F3"));
                    grt.SetString("Blob_info", _Defect_Location_DF, temp_Df_struct.Defect_Location.ToString("F3"));
                    grt.SetString("Blob_info", _Defect_Location_CX, temp_Co_struct.Defect_Location.ToString("F3"));


                    grt.SetString("Blob_info", _Pixel_num_BF, temp_Bf_struct.Pixel_num.ToString("F3"));
                    grt.SetString("Blob_info", _Pixel_num_DF, temp_Df_struct.Pixel_num.ToString("F3"));
                    grt.SetString("Blob_info", _Pixel_num_CX, temp_Co_struct.Pixel_num.ToString("F3"));

                    grt.SetString("Blob_info", _Average_Distance_BF, temp_Bf_struct.Average_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Average_Distance_DF, temp_Df_struct.Average_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Average_Distance_CX, temp_Co_struct.Average_Distance.ToString("F3"));


                    grt.SetString("Blob_info", _Centroid_Distance_BF, temp_Bf_struct.Centroid_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Centroid_Distance_DF, temp_Df_struct.Centroid_Distance.ToString("F3"));
                    grt.SetString("Blob_info", _Centroid_Distance_CX, temp_Co_struct.Centroid_Distance.ToString("F3"));

                    grt.SetString("Blob_info", _Long_Axis_BF, temp_Bf_struct.Long_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Long_Axis_DF, temp_Df_struct.Long_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Long_Axis_CX, temp_Co_struct.Long_Axis_len.ToString("F3"));

                    grt.SetString("Blob_info", _Short_Axis_BF, temp_Bf_struct.Short_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Short_Axis_DF, temp_Df_struct.Short_Axis_len.ToString("F3"));
                    grt.SetString("Blob_info", _Short_Axis_CX, temp_Co_struct.Short_Axis_len.ToString("F3"));


                    grt.SetString("Blob_info", _Average_R_BF, temp_Bf_struct.Avg_R.ToString("F3"));
                    grt.SetString("Blob_info", _Average_R_DF, temp_Df_struct.Avg_R.ToString("F3"));
                    grt.SetString("Blob_info", _Average_R_CX, temp_Co_struct.Avg_R.ToString("F3"));

                    grt.SetString("Blob_info", _Average_G_BF, temp_Bf_struct.Avg_G.ToString("F3"));
                    grt.SetString("Blob_info", _Average_G_DF, temp_Df_struct.Avg_G.ToString("F3"));
                    grt.SetString("Blob_info", _Average_G_CX, temp_Co_struct.Avg_G.ToString("F3"));

                    grt.SetString("Blob_info", _Average_B_BF, temp_Bf_struct.Avg_B.ToString("F3"));
                    grt.SetString("Blob_info", _Average_B_DF, temp_Df_struct.Avg_B.ToString("F3"));
                    grt.SetString("Blob_info", _Average_B_CX, temp_Co_struct.Avg_B.ToString("F3"));

                    grt.SetString("Blob_info", _Average_I_BF, temp_Bf_struct.Avg_I.ToString("F3"));
                    grt.SetString("Blob_info", _Average_I_DF, temp_Df_struct.Avg_I.ToString("F3"));
                    grt.SetString("Blob_info", _Average_I_CX, temp_Co_struct.Avg_I.ToString("F3"));
                }
            }
            else
            {
                int i = 0;
                string Defect_centerx = string.Format("Blob{0}_CENTER_X", i);
                string Defect_centery = string.Format("Blob{0}_CENTER_Y", i);
                string Defect_w = string.Format("Blob{0}_WIDTH", i);
                string Defect_h = string.Format("Blob{0}_HEIGHT", i);
                string Defect_a = string.Format("Blob{0}_ANGLE", i);

                string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                string BF = string.Format("Blob{0}_BF", i);
                string DF = string.Format("Blob{0}_DF", i);
                string CX = string.Format("Blob{0}_CX", i);

                string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);

                string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);

                string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);

                string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);

                string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);

                string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);

                string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);

                string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);

                string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);

                string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);

                string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);

                string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);

                string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);


                grt.SetString("Blob_Info", Defect_centerx, "0.000");
                grt.SetString("Blob_Info", Defect_centery, "0.000");
                grt.SetString("Blob_Info", Defect_w, "0.000");
                grt.SetString("Blob_Info", Defect_h, "0.000");
                grt.SetString("Blob_Info", Defect_a, "0.000");


                grt.SetString("Blob_Info", True_Defect, "0.000");


               
                grt.SetString("Blob_info", BF, "0.000");
                grt.SetString("Blob_info", DF, "0.000");
                grt.SetString("Blob_info", CX, "0.000"); 

                grt.SetString("Blob_info", _INOUT_at_BF,"0.000");
                grt.SetString("Blob_info", _INOUT_at_DF,"0.000");
                grt.SetString("Blob_info", _INOUT_at_CX, "0.000");

                grt.SetString("Blob_info", _Defect_Location_BF, "0.000");
                grt.SetString("Blob_info", _Defect_Location_DF, "0.000");
                grt.SetString("Blob_info", _Defect_Location_CX, "0.000");


                grt.SetString("Blob_info", _Pixel_num_BF, "0.000");
                grt.SetString("Blob_info", _Pixel_num_DF, "0.000");
                grt.SetString("Blob_info", _Pixel_num_CX, "0.000");

                grt.SetString("Blob_info", _Average_Distance_BF, "0.000");
                grt.SetString("Blob_info", _Average_Distance_DF, "0.000");
                grt.SetString("Blob_info", _Average_Distance_CX, "0.000");


                grt.SetString("Blob_info", _Centroid_Distance_BF,"0.000");
                grt.SetString("Blob_info", _Centroid_Distance_DF,"0.000");
                grt.SetString("Blob_info", _Centroid_Distance_CX, "0.000");

                grt.SetString("Blob_info", _Long_Axis_BF,"0.000");
                grt.SetString("Blob_info", _Long_Axis_DF,"0.000");
                grt.SetString("Blob_info", _Long_Axis_CX, "0.000");

                grt.SetString("Blob_info", _Short_Axis_BF, "0.000");
                grt.SetString("Blob_info", _Short_Axis_DF, "0.000");
                grt.SetString("Blob_info", _Short_Axis_CX, "0.000");


                grt.SetString("Blob_info", _Average_R_BF, "0.000");
                grt.SetString("Blob_info", _Average_R_DF, "0.000");
                grt.SetString("Blob_info", _Average_R_CX, "0.000");

                grt.SetString("Blob_info", _Average_G_BF, "0.000");
                grt.SetString("Blob_info", _Average_G_DF, "0.000");
                grt.SetString("Blob_info", _Average_G_CX, "0.000");

                grt.SetString("Blob_info", _Average_B_BF, "0.000");
                grt.SetString("Blob_info", _Average_B_DF, "0.000");
                grt.SetString("Blob_info", _Average_B_CX, "0.000");

                grt.SetString("Blob_info", _Average_I_BF, "0.000");
                grt.SetString("Blob_info", _Average_I_DF, "0.000");
                grt.SetString("Blob_info", _Average_I_CX, "0.000");
            }

        }

        private void saveGrt_withFeature_(string SaveAddress, Structure.ResultInfo Defects)
        {
            if (File.Exists(SaveAddress)) File.Delete(SaveAddress);
            IniReader grt = new IniReader(SaveAddress);



            // foreach (Structure.ResultInfo defect in Defects)
            {
                double ground_tr = 0.000;
                if (Defects.True_Defects.Count > 0) ground_tr = 1.000;
                else
                {
                    if (Defects.Under_Defects.Count >0 ) ground_tr = 1.000;
                }
                grt.SetString("ImageResult", "GroundTruth", ground_tr.ToString("F3"));// Defects.True_Defects.Count != 0 ? "1.000" : "0.000");
                grt.SetString("ImageResult", "Blob_num_at_BF", Defects.BF_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_DF", Defects.DF_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_CX", Defects.CO_Defects.Count().ToString("F3"));
                grt.SetString("ImageResult", "Blob_num_at_BL", Defects.BL_Defects.Count().ToString("F3"));
            }


            if (Defects.BF_Defects.Count+Defects.DF_Defects.Count+Defects.CO_Defects.Count +Defects.BL_Defects.Count> 0)
            {

                for (int i = 0; i < Defects.True_Defects.Count; i++)
                {
                    string Defect_centerx = string.Format("Defect{0}_CENTER_X", i);
                    string Defect_centery = string.Format("Defect{0}_CENTER_Y", i);
                    string Defect_w = string.Format("Defect{0}_WIDTH", i);
                    string Defect_h = string.Format("Defect{0}_HEIGHT", i);
                    string Defect_a = string.Format("Defect{0}_ANGLE", i);

                    string True_Defect = string.Format("Defect{0}_TrueDefect", i);

                    string Classification=string.Format("Defect{0}_Classification", i);


                    grt.SetString("ImageResult", Defect_centerx, Defects.True_Defects[i].cenx.ToString("F3"));
                    grt.SetString("ImageResult", Defect_centery, Defects.True_Defects[i].ceny.ToString("F3"));
                    grt.SetString("ImageResult", Defect_w, Defects.True_Defects[i].width.ToString("F3"));
                    grt.SetString("ImageResult", Defect_h, Defects.True_Defects[i].height.ToString("F3"));
                    grt.SetString("ImageResult", Defect_a, Defects.True_Defects[i].angle.ToString("F3"));
                    grt.SetString("ImageResult", True_Defect, "1.000");
                    grt.SetString("ImageResult", Classification, ((int)Defects.True_Defects[i].Name).ToString("F3"));

                }
             
                if (Defects.False_Defects.Count>0)
                {

                    for (int i = 0; i < Defects.False_Defects.Count; i++)
                    {
                        int j = Defects.True_Defects.Count + i;
                        string Defect_centerx = string.Format("Defect{0}_CENTER_X", j);
                        string Defect_centery = string.Format("Defect{0}_CENTER_Y", j);
                        string Defect_w = string.Format("Defect{0}_WIDTH", j);
                        string Defect_h = string.Format("Defect{0}_HEIGHT", j);
                        string Defect_a = string.Format("Defect{0}_ANGLE", j);
                        string Classification = string.Format("Defect{0}_Classification", j);
                        string True_Defect = string.Format("Defect{0}_TrueDefect", j);


                        grt.SetString("ImageResult", Defect_centerx, Defects.False_Defects[i].cenx.ToString("F3"));
                        grt.SetString("ImageResult", Defect_centery, Defects.False_Defects[i].ceny.ToString("F3"));
                        grt.SetString("ImageResult", Defect_w, Defects.False_Defects[i].width.ToString("F3"));
                        grt.SetString("ImageResult", Defect_h, Defects.False_Defects[i].height.ToString("F3"));
                        grt.SetString("ImageResult", Defect_a, Defects.False_Defects[i].angle.ToString("F3"));
                        grt.SetString("ImageResult", True_Defect, "-1.000");
                        grt.SetString("ImageResult", Classification, ((int)Defects.False_Defects[i].Name).ToString("F3"));

                    }
                }

                //write_func(grt, 0, Defects, 0, 1);
                //write_func(grt, Defects.True_BF_Defects.Count, Defects, 1, 1);
                //write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count, Defects, 2, 1);
                //write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count, Defects, 0, 0);
                //write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.False_BF_Defects.Count, Defects, 1, 0);
                //write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.False_BF_Defects.Count + Defects.False_DF_Defects.Count, Defects, 2, 0);
                write_func(grt, 0, Defects, 0, 1);
                write_func(grt, Defects.True_BF_Defects.Count, Defects, 1, 1);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count, Defects, 2, 1);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count, Defects, 3, 1);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.True_BL_Defects.Count, Defects, 0, 0);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.True_BL_Defects.Count + Defects.False_BF_Defects.Count, Defects, 1, 0);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.True_BL_Defects.Count + Defects.False_BF_Defects.Count + Defects.False_DF_Defects.Count, Defects, 2, 0);
                write_func(grt, Defects.True_BF_Defects.Count + Defects.True_DF_Defects.Count + Defects.True_CO_Defects.Count + Defects.True_BL_Defects.Count + Defects.False_BF_Defects.Count + Defects.False_DF_Defects.Count+Defects.False_CO_Defects.Count, Defects,3, 0);

            }
            else
            {
                int i = 0;
                string Defect_centerx = string.Format("Defect{0}_CENTER_X", i);
                string Defect_centery = string.Format("Defect{0}_CENTER_Y", i);
                string Defect_w = string.Format("Defect{0}_WIDTH", i);
                string Defect_h = string.Format("Defect{0}_HEIGHT", i);
                string Defect_a = string.Format("Defect{0}_ANGLE", i);
                string Classification = string.Format("Defect{0}_Classification", i);
                string True_Defect_=string.Format("Defect{0}_TrueDefect", i);
            
                string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                string BF = string.Format("Blob{0}_BF", i);
                string DF = string.Format("Blob{0}_DF", i);
                string CX = string.Format("Blob{0}_CX", i);
                string BL = string.Format("Blob{0}_BL", i);


                string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);
                string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i);

                string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);
                string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i);

                string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);
                string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i);

                string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);
                string _Average_Distance_BL = string.Format("Blob{0}_Average_Distance_BL", i);

                string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);
                string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i);

                string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);
                string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i);

                string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);
                string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i);

                string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);
                string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i);

                string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);
                string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i);

                string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);
                string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i);


                string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);
                string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i);

                string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);
                string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i);

                string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);
                string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i);



                if (Defects.Under_Defects == null) Defects.Under_Defects = new List<Structure.Defect_struct>();

                if (Defects.Under_Defects.Count == 0)
                {
                    grt.SetString("Blob_Info", True_Defect, "0.000");
                    grt.SetString("ImageResult", Defect_centerx, "0.000");
                    grt.SetString("ImageResult", Defect_centery, "0.000");
                    grt.SetString("ImageResult", Defect_w, "0.000");
                    grt.SetString("ImageResult", Defect_h, "0.000");
                    grt.SetString("ImageResult", Defect_a, "0.000");
                    grt.SetString("ImageResult", True_Defect_, "0.000");
                    grt.SetString("ImageResult", Classification, "0.000");

                }
                else
                {
                  
                    for (int a = 0; i < Defects.Under_Defects.Count; i++)
                    {

                        int b = a + Defects.True_Defects.Count + Defects.False_Defects.Count;
                         Defect_centerx = string.Format("Defect{0}_CENTER_X",b);
                         Defect_centery = string.Format("Defect{0}_CENTER_Y", b);
                         Defect_w = string.Format("Defect{0}_WIDTH", b);
                         Defect_h = string.Format("Defect{0}_HEIGHT", b);
                         Defect_a = string.Format("Defect{0}_ANGLE", b);
                         Classification = string.Format("Defect{0}_Classification", b);
                         True_Defect_ = string.Format("Defect{0}_TrueDefect", b);

                        grt.SetString("ImageResult", Defect_centerx, Defects.Under_Defects[a].cenx.ToString("F3"));
                        grt.SetString("ImageResult", Defect_centery, Defects.Under_Defects[a].ceny.ToString("F3"));
                        grt.SetString("ImageResult", Defect_w, Defects.Under_Defects[a].width.ToString("F3"));
                        grt.SetString("ImageResult", Defect_h, Defects.Under_Defects[a].height.ToString("F3"));
                        grt.SetString("ImageResult", Defect_a, Defects.Under_Defects[a].angle.ToString("F3"));
                        grt.SetString("ImageResult", True_Defect_, "2.000");
                        grt.SetString("ImageResult", Classification, ((int)Defects.Under_Defects[a].Name).ToString("F3"));

                    }
                }

                grt.SetString("Blob_info", True_Defect, "0.000");
                grt.SetString("Blob_info", BF, "0.000");
                grt.SetString("Blob_info", DF, "0.000");
                grt.SetString("Blob_info", CX, "0.000");
                grt.SetString("Blob_info", BL, "0.000");

                Classification = string.Format("Blob{0}_Classification", i );
                grt.SetString("Blob_info", Classification, "0.000");
                grt.SetString("Blob_info", _INOUT_at_BF, "0.000");
                grt.SetString("Blob_info", _INOUT_at_DF, "0.000");
                grt.SetString("Blob_info", _INOUT_at_CX, "0.000");
                grt.SetString("Blob_info", _INOUT_at_BL, "0.000");

                grt.SetString("Blob_info", _Defect_Location_BF, "0.000");
                grt.SetString("Blob_info", _Defect_Location_DF, "0.000");
                grt.SetString("Blob_info", _Defect_Location_CX, "0.000");
                grt.SetString("Blob_info", _Defect_Location_BL, "0.000");


                grt.SetString("Blob_info", _Pixel_num_BF, "0.000");
                grt.SetString("Blob_info", _Pixel_num_DF, "0.000");
                grt.SetString("Blob_info", _Pixel_num_CX, "0.000");
                grt.SetString("Blob_info", _Pixel_num_BL, "0.000");

                grt.SetString("Blob_info", _Average_Distance_BF, "0.000");
                grt.SetString("Blob_info", _Average_Distance_DF, "0.000");
                grt.SetString("Blob_info", _Average_Distance_CX, "0.000");
                grt.SetString("Blob_info", _Average_Distance_BL, "0.000");

                grt.SetString("Blob_info", _Centroid_Distance_BF, "0.000");
                grt.SetString("Blob_info", _Centroid_Distance_DF, "0.000");
                grt.SetString("Blob_info", _Centroid_Distance_CX, "0.000");
                grt.SetString("Blob_info", _Centroid_Distance_BL, "0.000");

                grt.SetString("Blob_info", _Long_Axis_BF, "0.000");
                grt.SetString("Blob_info", _Long_Axis_DF, "0.000");
                grt.SetString("Blob_info", _Long_Axis_CX, "0.000");
                grt.SetString("Blob_info", _Long_Axis_BL, "0.000");

                grt.SetString("Blob_info", _Short_Axis_BF, "0.000");
                grt.SetString("Blob_info", _Short_Axis_DF, "0.000");
                grt.SetString("Blob_info", _Short_Axis_CX, "0.000");
                grt.SetString("Blob_info", _Short_Axis_BL, "0.000");

                grt.SetString("Blob_info", _Average_R_BF, "0.000");
                grt.SetString("Blob_info", _Average_R_DF, "0.000");
                grt.SetString("Blob_info", _Average_R_CX, "0.000");
                grt.SetString("Blob_info", _Average_R_BL, "0.000");

                grt.SetString("Blob_info", _Average_G_BF, "0.000");
                grt.SetString("Blob_info", _Average_G_DF, "0.000");
                grt.SetString("Blob_info", _Average_G_CX, "0.000");
                grt.SetString("Blob_info", _Average_G_BL, "0.000");

                grt.SetString("Blob_info", _Average_B_BF, "0.000");
                grt.SetString("Blob_info", _Average_B_DF, "0.000");
                grt.SetString("Blob_info", _Average_B_CX, "0.000");
                grt.SetString("Blob_info", _Average_B_BL, "0.000");

                grt.SetString("Blob_info", _Average_I_BF, "0.000");
                grt.SetString("Blob_info", _Average_I_DF, "0.000");
                grt.SetString("Blob_info", _Average_I_CX, "0.000");
                grt.SetString("Blob_info", _Average_I_BL, "0.000");
            }

            if (Defects.Under_Defects!=null)
            {
                if (Defects.Under_Defects.Count > 0)
                {
                    for (int i = 0; i < Defects.Under_Defects.Count; i++)
                    {
                        int b = Defects.False_Defects.Count+Defects.True_Defects.Count + i;
                        string Defect_centerx = string.Format("Defect{0}_CENTER_X", b);
                        string Defect_centery = string.Format("Defect{0}_CENTER_Y", b);
                        string Defect_w = string.Format("Defect{0}_WIDTH", b);
                        string Defect_h = string.Format("Defect{0}_HEIGHT", b);
                        string Defect_a = string.Format("Defect{0}_ANGLE", b);
                        string Classification = string.Format("Defect{0}_Classification", b);
                        string True_Defect_ = string.Format("Defect{0}_TrueDefect", b);
      
                        grt.SetString("ImageResult", Defect_centerx, Defects.Under_Defects[i].cenx.ToString("F3"));
                        grt.SetString("ImageResult", Defect_centery, Defects.Under_Defects[i].ceny.ToString("F3"));
                        grt.SetString("ImageResult", Defect_w, Defects.Under_Defects[i].width.ToString("F3"));
                        grt.SetString("ImageResult", Defect_h, Defects.Under_Defects[i].height.ToString("F3"));
                        grt.SetString("ImageResult", Defect_a, Defects.Under_Defects[i].angle.ToString("F3"));
                        grt.SetString("ImageResult", True_Defect_, "2.000");
                        grt.SetString("ImageResult", Classification, ((int)Defects.Under_Defects[i].Name).ToString("F3"));

                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grt">ini 파일</param>
        /// <param name="i">최종 결함 인덱스 </param>
        /// <param name="Defects"> 최종 결함</param>
        /// <param name="defect_s"></param>
        /// <param name="defect_mode">wirte할 이미지 모드 결함 0:BF, 1:DF, 2:CO</param>
        ///<param name="TrueFalse">해당결함이 True인지 false 인지</param>
        private void write_func(IniReader grt, int i, Structure.ResultInfo Defects, int defect_mode, int TrueFalse)
        {

            List<Structure.Defect_struct> temp_Bf_struct = null;
            List<Structure.Defect_struct> temp_Df_struct = null;
            List<Structure.Defect_struct> temp_Co_struct = null;
            List<Structure.Defect_struct> temp_BL_struct = null;
            int defect_num = 0;

         
            switch (defect_mode)
            {
                case 0:
                    if (TrueFalse == 1) temp_Bf_struct = Defects.True_BF_Defects;
                    else temp_Bf_struct = Defects.False_BF_Defects;
                    //temp_Bf_struct = Defects.BF_Defects;// Defects.True_BF_Defects.FindAll(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    temp_Df_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Bf_struct.Count]);
                    temp_Co_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Bf_struct.Count]);
                    temp_BL_struct=new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Bf_struct.Count]);
                    defect_num = temp_Bf_struct.Count;
                    break;
                case 1:
                    if (TrueFalse == 1) temp_Df_struct = Defects.True_DF_Defects;
                    else temp_Df_struct = Defects.False_DF_Defects;
                    //temp_Df_struct = Defects.True_DF_Defects.FindAll(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    temp_Bf_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Df_struct.Count]);
                    temp_Co_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Df_struct.Count]);
                    temp_BL_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Df_struct.Count]);
                    defect_num = temp_Df_struct.Count;
                    break;
                case 2:
                    if (TrueFalse == 1) temp_Co_struct = Defects.True_CO_Defects;
                    else temp_Co_struct = Defects.False_CO_Defects;
                   // temp_Co_struct = Defects.True_CO_Defects.FindAll(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    temp_Bf_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Co_struct.Count]);
                    temp_Df_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Co_struct.Count]);
                    temp_BL_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_Co_struct.Count]);
                    defect_num = temp_Co_struct.Count;
                    break;
                case 3:
                    if (TrueFalse == 1) temp_BL_struct = Defects.True_BL_Defects;
                    else temp_BL_struct = Defects.False_BL_Defects;
                    // temp_Co_struct = Defects.True_CO_Defects.FindAll(x => x.blob_ind == Defects.True_Defects[i].blob_ind);
                    temp_Bf_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_BL_struct.Count]);
                    temp_Df_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_BL_struct.Count]);
                    temp_Co_struct = new List<Structure.Defect_struct>(new Structure.Defect_struct[temp_BL_struct.Count]);
                    defect_num = temp_BL_struct.Count;
                    break;
            }




            for (int j = 0; j < defect_num; j++)
            {
                #region format
                string Defect_centerx = string.Format("Blob{0}_CENTER_X", i+j);
                string Defect_centery = string.Format("Blob{0}_CENTER_Y", i + j);
                string Defect_w = string.Format("Blob{0}_WIDTH", i + j);
                string Defect_h = string.Format("Blob{0}_HEIGHT", i + j);
                string Defect_a = string.Format("Blob{0}_ANGLE", i + j);

                string True_Defect = string.Format("Blob{0}_TrueDefect", i + j);
                string BF = string.Format("Blob{0}_BF",  i+j);
                string DF = string.Format("Blob{0}_DF",  i+j);
                string CX = string.Format("Blob{0}_CX", i + j);
                string BL = string.Format("Blob{0}_BL", i + j);
                string Classification = string.Format("Blob{0}_Classification", i + j);

                string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i + j);
                string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF",  i+j);
                string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i + j);
                string _INOUT_at_BL = string.Format("Blob{0}_INOUT_at_BL", i + j);

                string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i + j);
                string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i + j);
                string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i + j);
                string _Defect_Location_BL = string.Format("Blob{0}_Defect_Location_BL", i + j);

                string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF",  i+j);
                string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF",  i+j);
                string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX",  i+j);
                string _Pixel_num_BL = string.Format("Blob{0}_Pixel_num_BL", i + j);

                string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF",  i+j);
                string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF",  i+j);
                string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX",  i+j);
                string _Average_Distance_BL = string.Format("Blob{0}_Average_Distance_BL", i + j);

                string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF",  i+j);
                string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF",  i+j);
                string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX",  i+j);
                string _Centroid_Distance_BL = string.Format("Blob{0}_Centroid_Distance_BL", i + j);

                string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF",  i+j);
                string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF",  i+j);
                string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX",  i+j);
                string _Long_Axis_BL = string.Format("Blob{0}_Long_Axis_BL", i + j);

                string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF",  i+j);
                string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF",  i+j);
                string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX",  i+j);
                string _Short_Axis_BL = string.Format("Blob{0}_Short_Axis_BL", i + j);

                string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF",  i+j);
                string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF",  i+j);
                string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX",  i+j);
                string _Long_Axis_angle_BL = string.Format("Blob{0}_Long_Axis_angle_BL", i + j);

                string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF",  i+j);
                string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF",  i+j);
                string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX",  i+j);
                string _Short_Axis_angle_BL = string.Format("Blob{0}_Short_Axis_angle_BL", i + j);

                string _Average_R_BF = string.Format("Blob{0}_Average_R_BF",  i+j);
                string _Average_R_DF = string.Format("Blob{0}_Average_R_DF",  i+j);
                string _Average_R_CX = string.Format("Blob{0}_Average_R_CX",  i+j);
                string _Average_R_BL = string.Format("Blob{0}_Average_R_BL", i + j);

                string _Average_G_BF = string.Format("Blob{0}_Average_G_BF",  i+j);
                string _Average_G_DF = string.Format("Blob{0}_Average_G_DF",  i+j);
                string _Average_G_CX = string.Format("Blob{0}_Average_G_CX",  i+j);
                string _Average_G_BL = string.Format("Blob{0}_Average_G_BL", i + j);

                string _Average_B_BF = string.Format("Blob{0}_Average_B_BF",  i+j);
                string _Average_B_DF = string.Format("Blob{0}_Average_B_DF",  i+j);
                string _Average_B_CX = string.Format("Blob{0}_Average_B_CX",  i+j);
                string _Average_B_BL = string.Format("Blob{0}_Average_B_BL", i + j);

                string _Average_I_BF = string.Format("Blob{0}_Average_I_BF",  i+j);
                string _Average_I_DF = string.Format("Blob{0}_Average_I_DF",  i+j);
                string _Average_I_CX = string.Format("Blob{0}_Average_I_CX",  i+j);
                string _Average_I_BL = string.Format("Blob{0}_Average_I_BL", i + j);
                #endregion

                grt.SetString("Blob_Info", True_Defect, TrueFalse == 1?"1.000":"-1.000");

                grt.SetString("Blob_info", BF, defect_mode == 0 ? "1.000" : "-1.000");
                grt.SetString("Blob_info", DF, defect_mode == 1 ? "1.000" : "-1.000");
                grt.SetString("Blob_info", CX, defect_mode == 2 ? "1.000" : "-1.000");
                grt.SetString("Blob_info", BL, defect_mode == 3 ? "1.000" : "-1.000");

                if (defect_mode == 0) grt.SetString("Blob_info", Classification, ((int)temp_Bf_struct[j].Name).ToString("F3"));
                else if (defect_mode == 1) grt.SetString("Blob_info", Classification, ((int)temp_Df_struct[j].Name).ToString("F3"));
                else if (defect_mode == 2) grt.SetString("Blob_info", Classification, ((int)temp_Co_struct[j].Name).ToString("F3"));
                else grt.SetString("Blob_info", Classification, ((int)temp_BL_struct[j].Name).ToString("F3"));

                grt.SetString("Blob_info", _INOUT_at_BF, temp_Bf_struct[j].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_DF, temp_Df_struct[j].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_CX, temp_Co_struct[j].In_Out.ToString("F3"));
                grt.SetString("Blob_info", _INOUT_at_BL, temp_BL_struct[j].In_Out.ToString("F3"));

                grt.SetString("Blob_info", _Defect_Location_BF, temp_Bf_struct[j].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_DF, temp_Df_struct[j].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_CX, temp_Co_struct[j].Defect_Location.ToString("F3"));
                grt.SetString("Blob_info", _Defect_Location_BL, temp_BL_struct[j].Defect_Location.ToString("F3"));

                grt.SetString("Blob_info", _Pixel_num_BF, temp_Bf_struct[j].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_DF, temp_Df_struct[j].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_CX, temp_Co_struct[j].Pixel_num.ToString("F3"));
                grt.SetString("Blob_info", _Pixel_num_BL, temp_BL_struct[j].Pixel_num.ToString("F3"));

                grt.SetString("Blob_info", _Average_Distance_BF, temp_Bf_struct[j].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_DF, temp_Df_struct[j].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_CX, temp_Co_struct[j].Average_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Average_Distance_BL, temp_BL_struct[j].Average_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Centroid_Distance_BF, temp_Bf_struct[j].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_DF, temp_Df_struct[j].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_CX, temp_Co_struct[j].Centroid_Distance.ToString("F3"));
                grt.SetString("Blob_info", _Centroid_Distance_BL, temp_BL_struct[j].Centroid_Distance.ToString("F3"));

                grt.SetString("Blob_info", _Long_Axis_BF, temp_Bf_struct[j].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_DF, temp_Df_struct[j].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_CX, temp_Co_struct[j].Long_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Long_Axis_BL, temp_BL_struct[j].Long_Axis_len.ToString("F3"));

                grt.SetString("Blob_info", _Short_Axis_BF, temp_Bf_struct[j].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_DF, temp_Df_struct[j].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_CX, temp_Co_struct[j].Short_Axis_len.ToString("F3"));
                grt.SetString("Blob_info", _Short_Axis_BL, temp_BL_struct[j].Short_Axis_len.ToString("F3"));


                grt.SetString("Blob_info", _Average_R_BF, temp_Bf_struct[j].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_DF, temp_Df_struct[j].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_CX, temp_Co_struct[j].Avg_R.ToString("F3"));
                grt.SetString("Blob_info", _Average_R_BL, temp_BL_struct[j].Avg_R.ToString("F3"));


                grt.SetString("Blob_info", _Average_G_BF, temp_Bf_struct[j].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_DF, temp_Df_struct[j].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_CX, temp_Co_struct[j].Avg_G.ToString("F3"));
                grt.SetString("Blob_info", _Average_G_BL, temp_BL_struct[j].Avg_G.ToString("F3"));

                grt.SetString("Blob_info", _Average_B_BF, temp_Bf_struct[j].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_DF, temp_Df_struct[j].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_CX, temp_Co_struct[j].Avg_B.ToString("F3"));
                grt.SetString("Blob_info", _Average_B_BL, temp_BL_struct[j].Avg_B.ToString("F3"));

                grt.SetString("Blob_info", _Average_I_BF, temp_Bf_struct[j].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_DF, temp_Df_struct[j].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_CX, temp_Co_struct[j].Avg_I.ToString("F3"));
                grt.SetString("Blob_info", _Average_I_BL, temp_BL_struct[j].Avg_I.ToString("F3"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">1=bf, 2=df, 3= co, 0 = default</param>
        private void UpdateWindow(int mode)
        {
            var halW = Halcon_Window.HalconWindow;
            HOperatorSet.SetDraw(halW, "margin");

            HOperatorSet.SetColor(halW, "spring green");

            switch (mode)
            {
                case 1:
                    HOperatorSet.DispColor(BaseImage_BF, halW);
                    break;
                case 2:
                    HOperatorSet.DispColor(BaseImage_DF, halW);
                    break;
                case 3:
                    HOperatorSet.DispColor(BaseImage_CO, halW);
                    break;
                default:
                    HOperatorSet.DispColor(BaseImage_BF, halW);
                  
                    break;
            }

            HOperatorSet.DispObj(DefectRegion, halW);
            HOperatorSet.SetColor(halW, "medium slate blue");
            HOperatorSet.DispObj(DefectRegion_BF, halW);
            HOperatorSet.DispObj(DefectRegion_DF, halW);
            HOperatorSet.DispObj(DefectRegion_CO, halW);
            HOperatorSet.DispObj(DefectRegion_BL, halW);
            HOperatorSet.SetColor(halW, "red");
            HOperatorSet.DispObj(select_defectRgn, halW);
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

        private void SaveContents(Structure.ResultInfo info)
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

        private void save_grt(Structure.ResultInfo info)
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
                p_BL = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                p_CO = new Bitmap(SYS.ImageWidth, SYS.ImageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                BitmapData BufBF = p_BF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                BitmapData BufDF = p_DF.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                BitmapData BufShilluette = p_BL.LockBits(new Rectangle(0, 0, SYS.ImageWidth, SYS.ImageHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
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


                //WB 적용 
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf0, SYS.MICRO_SIZE_WIDTH, SYS.MICRO_SIZE_HEIGHT, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.MicGainCh0[0], SYS.MicGainCh0[1], SYS.MicGainCh0[2], 8);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf1, SYS.MICRO_SIZE_WIDTH, SYS.MICRO_SIZE_HEIGHT, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.MicGainCh1[0], SYS.MicGainCh1[1], SYS.MicGainCh1[2], 8);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf2, SYS.MICRO_SIZE_WIDTH, SYS.MICRO_SIZE_HEIGHT, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.MicGainCh2[0], SYS.MicGainCh2[1], SYS.MicGainCh2[2], 8);
                DllImport.CvtImageDLLImport.SetWB(PtrImageBuf3, SYS.MICRO_SIZE_WIDTH, SYS.MICRO_SIZE_HEIGHT, ImageAnalysis.ImagePattern.BGR8bpp.ToString(), SYS.MicGainCh3[0], SYS.MicGainCh3[1], SYS.MicGainCh3[2], 8);


                //DarkField -> Intensity 적용
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf0_, PtrImageBuf0, SYS.MICRO_SIZE, RecipeIOI.IlTmicDF[0], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf1_, PtrImageBuf1, SYS.MICRO_SIZE, RecipeIOI.IlTmicDF[1], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf2_, PtrImageBuf2, SYS.MICRO_SIZE, RecipeIOI.IlTmicDF[2], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf3_, PtrImageBuf3, SYS.MICRO_SIZE, RecipeIOI.IlTmicDF[3], 8);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufDF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.MICRO_SIZE, 8);

                //BrightField -> Intensity 적용
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf0_, PtrImageBuf0, SYS.MICRO_SIZE, RecipeIOI.IlTmicBF[0], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf1_, PtrImageBuf1, SYS.MICRO_SIZE, RecipeIOI.IlTmicBF[1], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf2_, PtrImageBuf2, SYS.MICRO_SIZE, RecipeIOI.IlTmicBF[2], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf3_, PtrImageBuf3, SYS.MICRO_SIZE, RecipeIOI.IlTmicBF[3], 8);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufBF, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.MICRO_SIZE, 8);

                //Coaxial -> Intensity 적용
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf0_, PtrImageBuf0, SYS.MICRO_SIZE, RecipeIOI.IlTmicCO[0], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf1_, PtrImageBuf1, SYS.MICRO_SIZE, RecipeIOI.IlTmicCO[1], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf2_, PtrImageBuf2, SYS.MICRO_SIZE, RecipeIOI.IlTmicCO[2], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf3_, PtrImageBuf3, SYS.MICRO_SIZE, RecipeIOI.IlTmicCO[3], 8);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufCO, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.MICRO_SIZE, 8);

                //BackLight -> Intensity 적용
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf0_, PtrImageBuf0, SYS.MICRO_SIZE, RecipeIOI.IlTmicBL[0], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf1_, PtrImageBuf1, SYS.MICRO_SIZE, RecipeIOI.IlTmicBL[1], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf2_, PtrImageBuf2, SYS.MICRO_SIZE, RecipeIOI.IlTmicBL[2], 8);
                DllImport.CvtImageDLLImport.Intensity(PtrImageBuf3_, PtrImageBuf3, SYS.MICRO_SIZE, RecipeIOI.IlTmicBL[3], 8);
                //병합
                DllImport.CvtImageDLLImport.Summation(PtrBufShilluette, PtrImageBuf0_, PtrImageBuf1_, PtrImageBuf2_, PtrImageBuf3_, SYS.MICRO_SIZE, 8);


                HOperatorSet.GenImageInterleaved(out BaseImage_BF, BufBF.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);
                HOperatorSet.GenImageInterleaved(out BaseImage_DF, BufDF.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);
                HOperatorSet.GenImageInterleaved(out BaseImage_CO, BufCO.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);
            }
            catch 
            { }
            finally
            {
                p_BF.UnlockBits(BufBF);
                p_DF.UnlockBits(BufDF);
                p_BL.UnlockBits(BufShilluette);
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

        private void Btn_DC_Click(object sender, RoutedEventArgs e)
        {
            var select_ind = Defects_lst.SelectedItems;

            if (select_ind.Count > 0)
            {
                for (int list_i = 0; list_i < Defects_lst.Items.Count; list_i++)
                {
                    bool selected=false;// = select_ind.e.Contains(list_i);
                    for (int i = 0; i < select_ind.Count; i++)
                    {
                        string temp_St = select_ind[i].ToString();
                        if (temp_St.Contains(list_i.ToString()))
                        {
                            selected = true;
                            break;
                        }
                    }

                    if (selected)
                    {
                        if (Defects_lst.Items[list_i].ToString().Contains("Under")) Defects_lst.Items[list_i] = string.Format("{0}-{1}-{2}", list_i, cmb_DC.SelectedItem.ToString(), "Under");
                        else
                        {
                            Defects_lst.Items[list_i] = string.Format("{0}-{1}", list_i, cmb_DC.SelectedItem.ToString());
                            Structure.Defect_struct temp_strucuture = infos[Current_i].Defects[list_i];
                            temp_strucuture.Name = (Structure.Defect_Classification)Enum.Parse(typeof(Structure.Defect_Classification), cmb_DC.SelectedItem.ToString());
                            infos[Current_i].Defects[list_i] = temp_strucuture;
                        }
                    }
                }

            }


        }

        private void Cmb_ImgList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckPerform_btn_Click(object sender, RoutedEventArgs e)
        {

            RootAdderss = @"C:\Users\WTA\Desktop\grtTEST\grtTEST";
            CompareWithGT();

        }
        private void CompareWithGT()
        {
            FileInfo[] grtFiles = new DirectoryInfo(RootAdderss).GetFiles("*.txt");

            List<Structure.ResultInfo> DatResults = new List<Structure.ResultInfo>();
            List<Structure.ResultInfo> GtResults = new List<Structure.ResultInfo>();


            foreach (FileInfo grt in grtFiles)
            {
                string datName = grt.FullName.Replace(".txt", ".dat");
                IniReader grt_ini = new IniReader(grt.FullName);
                IniReader dat_ini = new IniReader(datName);

                Structure.ResultInfo temp_dat = new Structure.ResultInfo();
                temp_dat.datName = new FileInfo(grt.FullName.Replace(".txt", ".dat"));
                GetDatResult_Mic(ref temp_dat);
                DatResults.Add(temp_dat);
                Structure.ResultInfo temp_GT = new Structure.ResultInfo();
                temp_GT.datName= grt;
                GetGTResult_Mic(ref temp_GT);


               
            }
        }

        private void CheckCompareResult(Structure.ResultInfo gt, Structure.ResultInfo dat)
        {

        }

        public void GetGTResult_Mic(ref Structure.ResultInfo resultinfo)
        {
            IniDataReader test = new IniDataReader(resultinfo.datName.FullName.Replace("dat","txt"));
            string[] testttt = test.GetAllKeys("ImageResult");
            string testt = testttt[testttt.Length - 1].Split('_')[0];
            int Defect_num = Convert.ToInt32(testt.Substring(testt.Length -1, 1));
            resultinfo.True_Defects = new List<Structure.Defect_struct>();
            resultinfo.False_Defects = new List<Structure.Defect_struct>();
            resultinfo.Under_Defects = new List<Structure.Defect_struct>();
            resultinfo.Defects = new List<Structure.Defect_struct>();

            for (int i = 0; i < Defect_num+1; i++)
            {

                string section = "ImageResult";
                string Defect_centerx = string.Format("Defect{0}_CENTER_X", i);
                string Defect_centery = string.Format("Defect{0}_CENTER_Y", i);
                string Defect_w = string.Format("Defect{0}_WIDTH", i);
                string Defect_h = string.Format("Defect{0}_HEIGHT", i);
                string Defect_a = string.Format("Defect{0}_ANGLE", i);
                string Classification = string.Format("Defect{0}_Classification", i);
                string True_Defect_ = string.Format("Defect{0}_TrueDefect", i);

                Structure.Defect_struct defec_ = new Structure.Defect_struct();
                defec_.cenx = Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_CENTER_X", i)));
                defec_.ceny= Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_CENTER_Y", i)));
                defec_.width= Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_WIDTH", i)));
                defec_.height = Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_HEIGHT", i)));
                defec_.angle = Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_ANGLE", i)));
                defec_.Name = (Structure.Defect_Classification)Enum.ToObject(typeof(Structure.Defect_Classification), Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_Classification", i))));
                defec_.GroundTruth = Convert.ToInt32(test.GetDouble(section, string.Format("Defect{0}_TrueDefect", i)));

              
                if (defec_.GroundTruth == 1) resultinfo.True_Defects.Add(defec_);
                else if (defec_.GroundTruth == -1) resultinfo.False_Defects.Add(defec_);
                else
                {
                    defec_.UnderDefect = true;
                    resultinfo.Under_Defects.Add(defec_);
                }
                 resultinfo.Defects.Add(defec_);
                #region Blob 정보 
                //string True_Defect = string.Format("Blob{0}_TrueDefect", i);
                //string BF = string.Format("Blob{0}_BF", i);
                //string DF = string.Format("Blob{0}_DF", i);
                //string CX = string.Format("Blob{0}_CX", i);

                //string _INOUT_at_BF = string.Format("Blob{0}_INOUT_at_BF", i);
                //string _INOUT_at_DF = string.Format("Blob{0}_INOUT_at_DF", i);
                //string _INOUT_at_CX = string.Format("Blob{0}_INOUT_at_CX", i);

                //string _Defect_Location_BF = string.Format("Blob{0}_Defect_Location_BF", i);
                //string _Defect_Location_DF = string.Format("Blob{0}_Defect_Location_DF", i);
                //string _Defect_Location_CX = string.Format("Blob{0}_Defect_Location_CX", i);

                //string _Pixel_num_BF = string.Format("Blob{0}_Pixel_num_BF", i);
                //string _Pixel_num_DF = string.Format("Blob{0}_Pixel_num_DF", i);
                //string _Pixel_num_CX = string.Format("Blob{0}_Pixel_num_CX", i);

                //string _Average_Distance_BF = string.Format("Blob{0}_Average_Distance_BF", i);
                //string _Average_Distance_DF = string.Format("Blob{0}_Average_Distance_DF", i);
                //string _Average_Distance_CX = string.Format("Blob{0}_Average_Distance_CX", i);

                //string _Centroid_Distance_BF = string.Format("Blob{0}_Centroid_Distance_BF", i);
                //string _Centroid_Distance_DF = string.Format("Blob{0}_Centroid_Distance_DF", i);
                //string _Centroid_Distance_CX = string.Format("Blob{0}_Centroid_Distance_CX", i);

                //string _Long_Axis_BF = string.Format("Blob{0}_Long_Axis_BF", i);
                //string _Long_Axis_DF = string.Format("Blob{0}_Long_Axis_DF", i);
                //string _Long_Axis_CX = string.Format("Blob{0}_Long_Axis_CX", i);

                //string _Short_Axis_BF = string.Format("Blob{0}_Short_Axis_BF", i);
                //string _Short_Axis_DF = string.Format("Blob{0}_Short_Axis_DF", i);
                //string _Short_Axis_CX = string.Format("Blob{0}_Short_Axis_CX", i);

                //string _Long_Axis_angle_BF = string.Format("Blob{0}_Long_Axis_angle_BF", i);
                //string _Long_Axis_angle_DF = string.Format("Blob{0}_Long_Axis_angle_DF", i);
                //string _Long_Axis_angle_CX = string.Format("Blob{0}_Long_Axis_angle_CX", i);

                //string _Short_Axis_angle_BF = string.Format("Blob{0}_Short_Axis_angle_BF", i);
                //string _Short_Axis_angle_DF = string.Format("Blob{0}_Short_Axis_angle_DF", i);
                //string _Short_Axis_angle_CX = string.Format("Blob{0}_Short_Axis_angle_CX", i);

                //string _Average_R_BF = string.Format("Blob{0}_Average_R_BF", i);
                //string _Average_R_DF = string.Format("Blob{0}_Average_R_DF", i);
                //string _Average_R_CX = string.Format("Blob{0}_Average_R_CX", i);

                //string _Average_G_BF = string.Format("Blob{0}_Average_G_BF", i);
                //string _Average_G_DF = string.Format("Blob{0}_Average_G_DF", i);
                //string _Average_G_CX = string.Format("Blob{0}_Average_G_CX", i);

                //string _Average_B_BF = string.Format("Blob{0}_Average_B_BF", i);
                //string _Average_B_DF = string.Format("Blob{0}_Average_B_DF", i);
                //string _Average_B_CX = string.Format("Blob{0}_Average_B_CX", i);

                //string _Average_I_BF = string.Format("Blob{0}_Average_I_BF", i);
                //string _Average_I_DF = string.Format("Blob{0}_Average_I_DF", i);
                //string _Average_I_CX = string.Format("Blob{0}_Average_I_CX", i);

                #endregion

            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
           

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
                HOperatorSet.GenImageInterleaved(out BaseImage_DF, BufDF.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);
                 HOperatorSet.GenImageInterleaved(out BaseImage_CO, BufCO.Scan0, "bgr", SYS.ImageWidth, SYS.ImageHeight, -1, "byte", SYS.ImageWidth, SYS.ImageHeight, 0, 0, -1, 0);

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
