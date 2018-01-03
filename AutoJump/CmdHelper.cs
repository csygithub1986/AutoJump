namespace AutoJump
{
    public class CmdHelper
    {
        System.Diagnostics.Process process;

        public CmdHelper()
        {
            process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            process.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
            process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            process.StartInfo.CreateNoWindow = true;//不显示程序窗口
            process.Start();//启动程序
            process.StandardInput.AutoFlush = true;
        }

        public void WriteCmd(string cmd)
        {
            process.StandardInput.WriteLine(cmd);
        }

        public void Close()
        {
            process.Close();
        }
    }
}
