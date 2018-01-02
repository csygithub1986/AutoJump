using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoJump
{
    public class AdbCmdGenerator
    {
        string device = "event0";


        //生成Down
        public List<string> SendDown(int x, int y)
        {
            List<string> list = new List<string>();
            list.Add("adb shell sendevent /dev/input/" + device + " 3 53 " + x);
            list.Add("adb shell sendevent /dev/input/" + device + " 3 54 " + y);
            list.Add("adb shell sendevent /dev/input/" + device + " 3 57 1");
            list.Add("adb shell sendevent /dev/input/" + device + " 1 330 1");
            list.Add("adb shell sendevent /dev/input/" + device + " 0 0 0");
            return list;
        }

        //生成Up
        public List<string> SendUp()
        {
            List<string> list = new List<string>();
            list.Add("adb shell sendevent /dev/input/" + device + " 3 57 0");
            list.Add("adb shell sendevent /dev/input/" + device + " 1 330 0");
            list.Add("adb shell sendevent /dev/input/" + device + " 0 0 0");
            return list;
        }

        //截图
        public List<string> ScreenCap(string fileName)
        {
            List<string> list = new List<string>();
            list.Add("adb shell screencap -p /storage/sdcard0/autojump.png");
            list.Add("adb pull /storage/sdcard0/autojump.png " + fileName);
            return list;
        }
    }
}
