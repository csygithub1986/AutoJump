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
        double jumpParam = 2.0;//弹跳系数

        bool autoJump = false;//表示自动跳线程是否继续

        public MainWindow()
        {
            InitializeComponent();
            cmdHelper = new CmdHelper();
            adbGenerator = new AdbCmdGenerator();
        }

        private void Jump(int time)
        {
            List<string> list = adbGenerator.SendDown(500, 500);
            foreach (var item in list)
            {
                cmdHelper.WriteCmd(item);
                Thread.Sleep(100);
            }
            Thread.Sleep(time > 200 ? time - 200 : time);
            list = adbGenerator.SendUp();
            foreach (var item in list)
            {
                cmdHelper.WriteCmd(item);
                Thread.Sleep(100);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cmdHelper.Close();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            autoJump = true;
            Task.Factory.StartNew(() =>
            {
                while (autoJump)
                {
                    //截图
                    ScreenCap();
                    FileInfo imageFile = new FileInfo(Environment.CurrentDirectory + "/" + currentPngFile);
                    //调试用
                    //Dispatcher.BeginInvoke(new Action(() =>
                    //{
                    //    imageBox.Source = new BitmapImage(new Uri(imageFile.FullName, UriKind.Absolute));
                    //}));
                    //解析图像
                    int upX, downX;
                    AnalyseImage(imageFile, out upX, out downX);
                    if (upX == -1 || downX == -1)
                    {
                        autoJump = false;
                        MessageBox.Show("图像解析错误");
                        Dispatcher.Invoke(() =>
                        {
                            btnStart.IsEnabled = true;
                            btnStop.IsEnabled = false;
                        });
                        return;
                    }
                    //计算时间
                    int time = (int)(Math.Abs(upX - downX) * jumpParam);
                    //跳
                    Jump(time);
                    if (!autoJump)
                    {
                        break;
                    }
                    Thread.Sleep(2000);
                }
                Dispatcher.Invoke(() =>
                {
                    btnStart.IsEnabled = true;
                    btnStop.IsEnabled = false;
                });
            });

        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            autoJump = false;
        }

        //截屏
        private void ScreenCap()
        {
            //currentPngFile = DateTime.Now.Ticks.ToString() + ".png";
            List<string> list = adbGenerator.ScreenCap(currentPngFile);
            foreach (var item in list)
            {
                cmdHelper.WriteCmd(item);
                Thread.Sleep(1000);
            }
        }


        //操作图像
        public void AnalyseImage(FileInfo imageFile, out int upX, out int downX)
        {
            upX = -1;
            downX = -1;
            Bitmap bitmap = new Bitmap(imageFile.FullName);
            BitmapData bData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            unsafe
            {
                //找“棋盘”中心
                for (int y = 320; y < 960; y++)
                {
                    byte* color0 = (byte*)bData.Scan0 + y * bData.Stride;
                    int r0 = *(color0 + 2);
                    int g0 = *(color0 + 1);
                    int b0 = *color0;
                    bool hasFound = false;
                    int min = 0;
                    int max = 0;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                        int r = *(color + 2);
                        int g = *(color + 1);
                        int b = *color;
                        if (r0 != r || g0 != g || b0 != b)
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
                            break;
                        }
                    }
                    if (hasFound)
                    {
                        break;
                    }
                }
                //找“棋子”底座中心
                int rLeft = 43, gLeft = 43, bLeft = 73;
                int rRight = 58, gRight = 54, bRight = 81;
                int leftX = 0, rightX = 0, leftY = -1, rightY = -1;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 960 - 1; y >= 640; y--)
                    {
                        byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                        int r = *(color + 2);
                        int g = *(color + 1);
                        int b = *color;
                        if (rLeft == r && gLeft == g && bLeft == b)
                        {
                            leftX = x;
                            leftY = y;
                            break;
                        }
                    }
                    if (leftY != -1)
                    {
                        break;
                    }
                }
                for (int x = bitmap.Width - 1; x >= 0; x--)
                {
                    for (int y = 960 - 1; y >= 640; y--)
                    {
                        byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                        int r = *(color + 2);
                        int g = *(color + 1);
                        int b = *color;
                        if (rRight == r && gRight == g && bRight == b)
                        {
                            rightX = x;
                            rightY = y;
                            break;
                        }
                    }
                    if (rightY != -1)
                    {
                        break;
                    }
                }
                if (leftY != -1 && rightY != -1 && Math.Abs(leftY - rightY) < 5)
                {
                    downX = (leftX + rightX) / 2;
                }
            }
            bitmap.UnlockBits(bData);
            bitmap.Dispose();
        }
    }

    public struct Pixel
    {
        public byte B;         //
        public byte G;        //
        public byte R;        //
    }
}
