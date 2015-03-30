using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;
using System.Diagnostics;

namespace 树莓派扫描器
{
    public partial class Form1 : Form
    {
        List<Thread> xiancheng = new List<Thread>();
        String text = "";   
        //编辑框1的内容，为什么弄这个，因为如果是直接textbox.appendtext,在多线程同时添加文本的时候会出现问题
        //但是直接给text添加，再直接给编辑框赋值，这样的方式即使中途多线程同时给编辑框赋值text的时候出现问题
        //最后一次赋值也可以是正确的，因为text被赋值所造成的延迟比操作窗口要低得多
        String text2 = "";//编辑框2的内容
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            startScan();
        }
        public void startScan()
        {
            button1.Enabled = false;
            button1.Text = "正在循环扫描当中";
            int i;
            for (i = 1; i < 255; i++)
            {
                Thread th = new Thread(scan);
                //创建一个线程
                th.IsBackground = true;
                //设置它为后台线程，不然程序关闭之后不会被结束
                th.Start((object)i);
                //传递参数
                xiancheng.Add(th);
                //将创建的线程加入到list中
                progressBar1.Value = i;
                //进度条更新
            }
        }
        public void scan(object ip)
        {
            int f = 1;
            //如果扫到树莓派，或者扫到的IP没有开放22端口，则f=0
            //如果IP是ping不通，则继续扫描
            while (f!=0)
            {
                try
                {
                    String IP = "192.168.191." + ip.ToString();
                    Ping p = new Ping();
                    //首先测试ping，能ping通再尝试TCP连接22端口，因为tcp超时很长
                    IPAddress myIP = IPAddress.Parse(IP.Split(':')[0]);
                    //建立一个IPArrdess类
                    PingReply reply = p.Send(myIP, 200);
                    //连接IP，超时200ms
                    if (reply.Status == IPStatus.Success)
                    {
                        f = 0;
                        //连接成功之后
                        text = text + "扫描到一个IP:" + IP + "\r\n";
                        textBox1.Text = text;
                        TcpClient client = new TcpClient();
                        client.Connect(IP, 22);
                        //尝试TCP连接22端口（ssh端口）
                        client.Close();
                        //如果没有抛出异常则会执行以下语句
                        text = text + "抓到一只树莓派:" + IP + "\r\n";
                        textBox1.Text = text;
                        getMac(IP);
                        Process.Start("putty.exe", "-ssh -l root -pw lxm -P 22 " + IP);
                        //运行当前目录下的putty，并且传递root,shang,ip等参数

                    }
                }
                catch (SocketException)
                {
                    f = 0;
                    //如果是树莓派则会抛出这个错误，服务器积极拒绝
                    //如果不是，就不会有这个错误
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int i;
            for (i = 0; i < xiancheng.Count; i++)
            {
                xiancheng[i].Abort();
                //停止所有线程
            }
            xiancheng.Clear();
            //清除list
            button1.Enabled = true;
            button1.Text = "开始循环扫描";
            progressBar1.Value = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Dispose(true);
            this.Close();
            Application.ExitThread();
            Application.Exit();
            //退出所有后台线程，不然进程不会自动结束
            //System.Environment.Exit(0); 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            //不加这句话的话，线程里无法操作编辑框添加文本
            //startScan();
            //程序开始运行以后自动开始扫描
        }

        private void button3_Click(object sender, EventArgs e)
        {
            getMac("");
        }
        public void getMac(String IP)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序

            try
            {
                //向cmd窗口发送输入信息
                p.StandardInput.WriteLine("arp -a&exit");
                p.StandardInput.AutoFlush = true;
                //获取cmd窗口的输出信息
                string output = p.StandardOutput.ReadToEnd();

                Regex regex; MatchCollection matches;
                regex = new Regex(@"\w*.\w*.\w*.\w* *\w*-\w*-\w*-\w*-\w*-\w*");
                matches = regex.Matches(output);
                //正则表达式匹配，\w*.\w*.\w*.\w* *\w*-\w*-\w*-\w*-\w*-\w*表示的意思是IP+任意多个空格+mac地址
                int i;
                if (IP == null||IP=="")
                {
                    //IP如果为空就输出所有mac地址
                    for (i = 0; i < matches.Count; i++) text2 += matches[i].Value.ToString() + "\r\n";
                    textBox2.Text = text2;
                }
                else
                {
                    for (i = 0; i < matches.Count; i++)
                    {
                        String ipmac=matches[i].Value.ToString();
                        regex = new Regex(@"\w*.\w*.\w*.\w*");
                        //继续匹配IP
                        MatchCollection matches2 = regex.Matches(ipmac);
                        if (matches2[0].Value == IP) text2 += ipmac + "\r\n";
                        //如果是IP就输出
                    }
                    textBox2.Text = text2;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public String zhongjian(String text, String textl, String textr, int start)
        {
            int left = text.IndexOf(textl, start);
            int right = text.IndexOf(textr, left);
            return text.Substring(left + textl.Length, right);
        }
    }
}
