using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AutoJump
{
    public partial class MainWindow : Window
    {
        CmdHelper cmdHelper;
        //string lastPngFile;
        string currentPngFile = "autojump.png";
        double jumpParam = 2.05;//弹跳系数
        //double jumpParamAdjust;//弹跳系数
        double widthHeightRate = 1.7218;//长宽比
        double standardWidth = 720;//基准宽度

        bool autoJump = false;//表示自动跳线程是否继续

        public MainWindow()
        {
            InitializeComponent();
            cmdHelper = new CmdHelper();
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
                    Dispatcher.Invoke(new Action(() =>
                    {
                        // 读取图片源文件到
                        BinaryReader binReader = new BinaryReader(File.Open(imageFile.FullName, FileMode.Open));
                        byte[] bytes = binReader.ReadBytes((int)imageFile.Length);
                        binReader.Close();
                        // 将图片字节赋值给BitmapImage 
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(bytes);
                        bitmap.EndInit();
                        imageBox.Source = bitmap;
                    }));


                    //解析图像
                    int centerX, centerY, pieceX, pieceY, imageWidth;
                    AnalyseImage(imageFile, out centerX, out centerY, out pieceX, out pieceY, out imageWidth);
                    if (pieceX == -1 || pieceY == -1)
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
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Canvas.SetLeft(gridFrom, pieceX * 0.5 * standardWidth / imageWidth);
                        Canvas.SetTop(gridFrom, pieceY * 0.5 * standardWidth / imageWidth);
                        Canvas.SetLeft(gridTo, centerX * 0.5 * standardWidth / imageWidth);
                        Canvas.SetTop(gridTo, centerY * 0.5 * standardWidth / imageWidth);
                    }));
                    //计算时间
                    int time = (int)(Math.Sqrt((centerX - pieceX) * (centerX - pieceX) + (centerY - pieceY) * (centerY - pieceY)) * jumpParam * standardWidth / imageWidth);
                    //跳
                    cmdHelper.WriteCmd("adb shell input swipe 500 500 500 700 " + time);

                    if (!autoJump)
                    {
                        break;
                    }
                    Thread.Sleep(4000);
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
            cmdHelper.WriteCmd("adb shell screencap -p /storage/sdcard0/autojump.png");//截图
            Thread.Sleep(3000);
            cmdHelper.WriteCmd("adb pull /storage/sdcard0/autojump.png " + currentPngFile);//复制
            Thread.Sleep(1500);
        }

        //分析图像
        public void AnalyseImage(FileInfo imageFile, out int centerX, out int centerY, out int pieceX, out int pieceY, out int imageWidth)
        {
            centerX = -1;
            centerY = -1;
            pieceX = -1;
            pieceY = -1;
            Bitmap bitmap = new Bitmap(imageFile.FullName);
            BitmapData bData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            imageWidth = bitmap.Width;
            unsafe
            {
                #region 找“小人”底座中心
                {
                    System.Windows.Media.Color colorLeft = new System.Windows.Media.Color() { R = 43, G = 43, B = 73 };
                    System.Windows.Media.Color colorRight = new System.Windows.Media.Color() { R = 58, G = 54, B = 81 };
                    int leftX = 0, rightX = 0, leftY = -1, rightY = -1;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = bitmap.Height * 3 / 4 - 1; y >= bitmap.Height / 2; y--)
                        {
                            byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                            byte r = *(color + 2);
                            byte g = *(color + 1);
                            byte b = *color;
                            if (colorLeft.R == r && colorLeft.G == g && colorLeft.B == b)
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
                        for (int y = bitmap.Height * 3 / 4 - 1; y >= bitmap.Height / 2; y--)
                        {
                            byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                            byte r = *(color + 2);
                            byte g = *(color + 1);
                            byte b = *color;
                            if (colorRight.R == r && colorRight.G == g && colorRight.B == b)
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
                    if (leftY != -1 && rightY != -1 && Math.Abs(leftY - rightY) < 10)
                    {
                        pieceX = (leftX + rightX) / 2;
                        pieceY = Math.Min(leftY, rightY);
                    }
                }

                #endregion

                #region 找“盒子”X中心
                {
                    int upY = 0;
                    int leftX = 0;
                    int rightX = 0;
                    System.Windows.Media.Color colorMain = new System.Windows.Media.Color();
                    for (int y = bitmap.Height / 4; y < bitmap.Height * 3 / 4; y++)
                    {
                        byte* color0 = (byte*)bData.Scan0 + y * bData.Stride;
                        byte r0 = *(color0 + 2);
                        byte g0 = *(color0 + 1);
                        byte b0 = *color0;
                        bool hasFound = false;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                            byte r = *(color + 2);
                            byte g = *(color + 1);
                            byte b = *color;
                            if (Math.Abs(r0 - r) > 2 || Math.Abs(g0 - g) > 2 || Math.Abs(b0 - b) > 2)
                            {
                                if (Math.Abs(x - pieceX) < (int)(25 * bitmap.Width / standardWidth))
                                {
                                    //当小人的头比新“盒子”高时，把“小人”的头避开
                                    continue;
                                }
                                rightX = x;
                                if (hasFound == false)
                                {
                                    leftX = x;
                                    upY = y;
                                    hasFound = true;
                                    //保险起见，颜色选取正中心下面一个像素的颜色
                                    color = (byte*)bData.Scan0 + x * 3 + (y + 1) * bData.Stride;
                                    r = *(color + 2);
                                    g = *(color + 1);
                                    b = *color;
                                    colorMain.R = r;
                                    colorMain.G = g;
                                    colorMain.B = b;
                                }
                            }
                            else if (hasFound)
                            {
                                centerX = (leftX + rightX) / 2;
                                break;
                            }
                        }
                        if (hasFound)
                        {
                            break;
                        }
                    }

                    //找相同颜色的连续点（不同于一般的算法，这里只在乎边缘的连续，只往下面和两边搜索，所以会快一点）
                    int downY = upY;
                    List<System.Drawing.Point> lastColorList = new List<System.Drawing.Point>();
                    for (int x = leftX; x <= rightX; x++)
                    {
                        lastColorList.Add(new System.Drawing.Point(x, upY));
                    }

                    for (int y = upY + 1; y < bitmap.Height * 3 / 4; y++)
                    {
                        List<System.Drawing.Point> currentColorList = new List<System.Drawing.Point>();
                        //搜索左边
                        for (int x = lastColorList[0].X; x >= 0; x--)
                        {
                            byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                            if (*(color + 2) == colorMain.R && *(color + 1) == colorMain.G && *color == colorMain.B)
                            {
                                currentColorList.Insert(0, new System.Drawing.Point(x, y));
                            }
                            else
                            {
                                break;
                            }
                        }
                        //搜索中间
                        for (int i = 1; i < lastColorList.Count - 1; i++)
                        {
                            if (currentColorList.Count > 0 && lastColorList[i].X <= currentColorList.Last().X)
                            {
                                continue;
                            }
                            byte* color = (byte*)bData.Scan0 + lastColorList[i].X * 3 + y * bData.Stride;
                            if (*(color + 2) == colorMain.R && *(color + 1) == colorMain.G && *color == colorMain.B)
                            {
                                currentColorList.Add(new System.Drawing.Point(lastColorList[i].X, y));
                                for (int j = lastColorList[i].X + 1; j < lastColorList.Last().X; j++)
                                {
                                    byte* color2 = (byte*)bData.Scan0 + j * 3 + y * bData.Stride;
                                    if (*(color2 + 2) == colorMain.R && *(color2 + 1) == colorMain.G && *color2 == colorMain.B)
                                    {
                                        currentColorList.Add(new System.Drawing.Point(j, y));
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        //搜索右边
                        for (int x = lastColorList[lastColorList.Count - 1].X; x < bitmap.Width; x++)
                        {
                            byte* color = (byte*)bData.Scan0 + x * 3 + y * bData.Stride;
                            if (*(color + 2) == colorMain.R && *(color + 1) == colorMain.G && *color == colorMain.B)
                            {
                                currentColorList.Add(new System.Drawing.Point(x, y));
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (currentColorList.Count == 0)
                        {
                            break;
                        }
                        else
                        {
                            lastColorList = currentColorList;
                            downY = y;
                            if (leftX > currentColorList[0].X)
                            {
                                leftX = currentColorList[0].X;
                            }
                            if (rightX < currentColorList[currentColorList.Count - 1].X)
                            {
                                rightX = currentColorList[currentColorList.Count - 1].X;
                            }
                        }
                    }

                    //限制条件
                    double width = rightX - leftX;
                    double height = downY - upY;
                    if (width > 360 * bitmap.Width / standardWidth || width < 45 * bitmap.Width / standardWidth || width / height > widthHeightRate * 1.25 || width / height < widthHeightRate / 1.25)
                    {
                        //判断出错，保险起见，只能以顶点为参考点，减去保守的固定值
                        centerY = upY + (int)(70 * bitmap.Width / standardWidth);
                        Console.WriteLine("Y坐标没有判断成功" + DateTime.Now.ToLongTimeString());
                    }
                    else
                    {
                        centerY = (upY + downY) / 2 - (int)((downY - upY) * 0.01);
                    }
                }

                #endregion

            }
            bitmap.UnlockBits(bData);
            bitmap.Dispose();
        }
    }
}
