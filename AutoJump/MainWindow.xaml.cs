using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoJump
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        CmdHelper cmdHelper;
        AdbCmdGenerator adbGenerator;
        //string lastPngFile;
        string currentPngFile = "autojump.png";
        public MainWindow()
        {
            InitializeComponent();
            //cmdHelper = new CmdHelper();
            //adbGenerator = new AdbCmdGenerator();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int upX;
            ReadColor(out upX);
            Console.WriteLine(upX);
            //List<string> list = adbGenerator.SendDown(100, 100);
            //foreach (var item in list)
            //{
            //    cmdHelper.WriteCmd(item);
            //    Thread.Sleep(100);
            //}
            ////Thread.Sleep(400);
            //list = adbGenerator.SendUp();
            //foreach (var item in list)
            //{
            //    cmdHelper.WriteCmd(item);
            //    Thread.Sleep(100);
            //}
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cmdHelper.Close();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            ScreenCap();
            FileInfo imageFile = new FileInfo(Environment.CurrentDirectory + "/" + currentPngFile);
            imageBox.Source = new BitmapImage(new Uri(imageFile.FullName, UriKind.Absolute));

        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {

        }

        //截屏
        private void ScreenCap()
        {
            List<string> list = adbGenerator.ScreenCap(currentPngFile);
            foreach (var item in list)
            {
                cmdHelper.WriteCmd(item);
                Thread.Sleep(1000);
            }
        }


        //操作图像

        public void ReadColor(out int upX)
        {
            Bitmap b = new Bitmap(@"C:\Users\陈帅宇\Documents\Tencent Files\274322346\FileRecv\MobileFile\a.png");
            BitmapData bData = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            unsafe
            {

                for (int y = 320; y < 960; y++)
                {
                    //int r =
                    byte* color0 = (byte*)bData.Scan0 + y * bData.Stride;
                    int R0 = *(color0 + 2);
                    int G0 = *(color0 + 1);
                    int B0 = *color0;
                    bool hasFound = false;
                    int min = 0;
                    int max = 0;
                    for (int x = 0; x < b.Width; x++)
                    {
                        byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                        int R = *(color + 2);
                        int G = *(color + 1);
                        int B = *color;
                        if (R0 != R || G0 != G || B0 != B)
                        {
                            max = x;
                            if (hasFound == false)
                            {
                                min = x;
                                hasFound = true;
                            }
                        }
                        else if (hasFound)
                        {
                            upX = (min + max) / 2;
                            return;
                        }
                    }
                }
            }
            upX = 0;
            b.UnlockBits(bData);
        }
    }

    public struct Pixel
    {
        public byte B;         //
        public byte G;        //
        public byte R;        //
    }
}
