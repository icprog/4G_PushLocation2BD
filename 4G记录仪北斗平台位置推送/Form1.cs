﻿using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace _4G记录仪北斗平台位置推送
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static string FileName = string.Empty;

        public static string FileNamePath = string.Empty;

        public p4g.LoginInfo logininfo = new p4g.LoginInfo();

        public static bool IsSend = false;//判断推送标志

        private void btn_logon_Click(object sender, EventArgs e)
        {
            if (PingDevice(tb_ServerIP.Text))
            {
                string command = "http://" + tb_ServerIP.Text + "/StandardApiAction_login.action?account=" + tb_UseName.Text + "&password=" + tb_Password.Text;
                string text = HttpGet(command);

               // p4g.LoginInfo logininfo = new p4g.LoginInfo();
                logininfo = JsonConvert.DeserializeObject<p4g.LoginInfo>(text);
                if (logininfo != null)
                {
                    if (logininfo.result == 0)
                    {
                        SetListBox(listBox1, "登录4G服务器成功！");
                        SetListBox(listBox1, "回调值：" + logininfo.jsession);
                        //getuserinfo(logininfo.jsession);

                        btn_StartSend.Enabled = true;
                        btn_StopSend.Enabled = false;

                        btn_logon.Enabled = false;
                        btn_logout.Enabled = true;


                    }
                    else
                    {
                        SetListBox(listBox1, "登录4G服务器失败！Error：" + logininfo.result.ToString());
                        
                    }

                }
                
            }
            else
            {
                SetListBox(listBox1, "服务器 [ " + tb_ServerIP.Text + " ] 不在线！");
            }
            //string ss = HttpGet("http://119.23.161.197/StandardApiAction_login.action?account=hengan&password=000000");
           // updateMessage(listBox1, "[登录]返回值：" + ss);
        }


        /// <summary>
        /// 获取用户设备信息
        /// </summary>
        /// <param name="callinfo"></param>
        private void getuserinfo(string callinfo)
        {
            if (PingDevice(tb_ServerIP.Text))
            { 
               // string command1 = "http://" + tb_ServerIP.Text + "/StandardApiAction_login.action?account=" + tb_UseName.Text + "&password=" + tb_Password.Text;
                string command = "http://" + tb_ServerIP.Text + "/StandardApiAction_queryUserVehicle.action?jsession=" + callinfo;
                string text = HttpGet(command);

                p4g.DeviceInfo deviceinfo = new p4g.DeviceInfo();
                deviceinfo = JsonConvert.DeserializeObject<p4g.DeviceInfo>(text);
                if (deviceinfo != null)
                  {
                      if (deviceinfo.result == 0)
                      {
                          SetListBox(listBox1, "获取设备信息成功！");

                          //获取单位为TJXGAJ的ID（单位ID）
                          string depname = "TJXGAJ";
                          int depid = -1;
                          for (int i = 0; i < deviceinfo.companys.Count; i++)
                          {
                              if (deviceinfo.companys[i].nm == depname)
                              {
                                  depid = deviceinfo.companys[i].id;//获取单位ID编号
                              }
                          }
                          List<string> Deviceid = new List<string>();
                          string deviceall = string.Empty;
                          //SetListBox(listBox1, "获取单位 [ " + depname + " ] 所有注册执法记录仪！");
                          if (depid != -1)
                          {
                              for (int i = 0; i < deviceinfo.vehicles.Count; i++)
                              {
                                  if (deviceinfo.vehicles[i].pid == depid)
                                  {
                                      Deviceid.Add(deviceinfo.vehicles[i].nm);
                                      deviceall += deviceinfo.vehicles[i].nm + ",";
                                      SetListBox(listBox1, deviceinfo.vehicles[i].nm);
                                  }
                              }

                              //获取在线设备ID
                              List<string> onlinedevice = new List<string>();
                              onlinedevice = getOnlineDevice(callinfo, deviceall);

                              if(onlinedevice.Count>0)
                              {
                                  for(int i=0;i<onlinedevice.Count;i++)
                                  {
                                      getdeviceGPS(callinfo, onlinedevice[i]);
                                  }

                                  if (File.Exists(FileNamePath))
                                  {
                                      UpDownFTP(FileNamePath);
                                  }
                                  else
                                  {
                                      SetListBox(listBox1, "未产生有效的位置信息文件，不进行推送！");
                                  }
                              }



                              
                          }




                          //SetListBox(listBox1, depid.ToString());

                          List<p4g.Vehicles> alldev = new List<p4g.Vehicles>();
                          alldev = deviceinfo.vehicles;


                          //SetListBox(listBox1, "回调值：" + deviceinfo.co);
                          //getuserinfo(logininfo.jsession);
                      }
                      else
                      {
                          SetListBox(listBox1, "获取用户设备信息失败！Error：" + deviceinfo.result.ToString());
                      }
                  }
            


            }
        }




        /// <summary>
        /// 获取在线设备编号
        /// </summary>
        /// <param name="deviceid"></param>
        /// <returns></returns>
        private List<string> getOnlineDevice(string callinfo, string deviceid)
        {
            List<string> online = new List<string>();
            //string command = "http://" + tb_ServerIP.Text + "/StandardApiAction_queryUserVehicle.action?jsession=" + callinfo;
            string command = "http://" + tb_ServerIP.Text + "/StandardApiAction_getDeviceOlStatus.action?jsession=" + callinfo + "&devIdno=" + deviceid;
            string text = HttpGet(command);

            p4g.OnlineInfo onlineinfo = new p4g.OnlineInfo();
            onlineinfo = JsonConvert.DeserializeObject<p4g.OnlineInfo>(text);
            if (onlineinfo.result == 0)
            {
                for (int i = 0; i < onlineinfo.onlines.Count; i++)
                {
                    if (onlineinfo.onlines[i].online == 1)
                    {
                        online.Add(onlineinfo.onlines[i].did);
                        SetListBox(listBox1, "获取在线设备编号：" + onlineinfo.onlines[i].did);
                    }
                }
            }
            else
            {
                SetListBox(listBox1, "获取在线设备编号！Error：" + onlineinfo.result.ToString());
            }


                return online;
        }


        /// <summary>
        /// 获取在线设备的GPS位置信息
        /// </summary>
        private void getdeviceGPS(string callinfo,string deviceid)
        {
           // http://119.23.161.197/StandardApiAction_getDeviceStatus.action?jsession=cf6b70a3-c82b-4392-8ab6-bbddce336222&devIdno=500000&toMap=2

            string command = "http://" + tb_ServerIP.Text + "/StandardApiAction_getDeviceStatus.action?jsession=" + callinfo + "&devIdno=" + deviceid + "&toMap=2";
            string text = HttpGet(command);

            p4g.GetGPSstatus getgpsstatus = new p4g.GetGPSstatus();
            getgpsstatus = JsonConvert.DeserializeObject<p4g.GetGPSstatus>(text);
            if (getgpsstatus.result == 0)
            {
                //获取GPS位置信息
                for (int i = 0; i < getgpsstatus.status.Count; i++)
                {
                    //获取有效GPS位置信息
                    if (getgpsstatus.status[i].lng != 0 && getgpsstatus.status[i].lat!=0)
                    {
                        SetListBox(listBox1, deviceid + " 地理位置：{ " + getgpsstatus.status[i].lng + " , " + getgpsstatus.status[i].lat + " } ");
                        SetGPSmsg(deviceid, getgpsstatus.status[i].lng, getgpsstatus.status[i].lat, Convert.ToDateTime(getgpsstatus.status[i].gt.Split('.')[0]));
                    }
                
                }
            }
            else
            {
                SetListBox(listBox1, "获取位置信息失败！Error：" + getgpsstatus.result.ToString());
            }

        
        
        
        }











        public static string HttpGet(string url)
        {
            StreamReader reader = null;
            try
            {
                //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                Encoding encoding = Encoding.UTF8;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.ContentType = "application/json";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                WriteLog.CreateExceptionLog(ex);
                return reader.ReadToEnd();
            }

        }

        /// <summary>
        /// ping IP 是否在线
        /// </summary>
        /// <param name="IPadrres">IP地址</param>
        /// <returns>成功，返回true，失败返回false</returns>
        public bool PingDevice(string IPadrres)
        {
            bool result = false;
            Ping ping = new Ping();
            try
            {
                PingReply pingReply = ping.Send(IPadrres);
                if (pingReply.Status == IPStatus.Success)
                {
                    result = true;
                }
            }
            catch (Exception)
            {
            }
            return result;
        }





        /// <summary>
        /// SetListBox控件跨线程安全
        /// </summary>
        /// <param name="listbox"></param>
        /// <param name="text"></param>
        public static void SetListBox(ListBox listbox, String text)
        {
            if (listbox.InvokeRequired)
            {

                listbox.BeginInvoke(new Action<String>((msg) =>
                {
                    if (listbox.Items.Count > 500)
                    {
                        listbox.Items.Clear();
                    }
                    bool scroll = false;
                    if (listbox.TopIndex == listbox.Items.Count - (int)(listbox.Height / listbox.ItemHeight))
                        scroll = true;
                    listbox.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + msg);
                    if (scroll)
                        listbox.TopIndex = listbox.Items.Count - (int)(listbox.Height / listbox.ItemHeight);

                }), text);
            }
            else
            {
                //this.listBox1.Items.Add(text);

                if (listbox.Items.Count > 500)
                {
                    listbox.Items.Clear();
                }
                bool scroll = false;
                if (listbox.TopIndex == listbox.Items.Count - (int)(listbox.Height / listbox.ItemHeight))
                    scroll = true;
                listbox.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + text);
                if (scroll)
                    listbox.TopIndex = listbox.Items.Count - (int)(listbox.Height / listbox.ItemHeight);
            }
        }

        /// <summary>
        /// ComboBoxEdit  添加项目 跨线程安全
        /// </summary>
        /// <param name="SBName">按钮控件名称</param>
        /// <param name="enable"></param>
        public static void SetComboBoxEditAddItem(ComboBox CBEName, string txtstr)
        {
            if (CBEName.InvokeRequired)
            {
                CBEName.BeginInvoke(new Action<string>((msg) =>
                {
                    CBEName.Items.Add(msg);
                }), txtstr);
            }
            else
            {
                CBEName.Items.Add(txtstr);
            }
        }


        /// <summary>
        /// TextEdit  跨线程安全
        /// </summary>
        /// <param name="SBName">按钮控件名称</param>
        /// <param name="enable"></param>
        public static void SetTextEdit(TextBox TEName, string txtstr)
        {
            if (TEName.InvokeRequired)
            {
                TEName.BeginInvoke(new Action<string>((msg) =>
                {
                    TEName.Text = msg;
                }), txtstr);
            }
            else
            {
                TEName.Text = txtstr;
            }
        }







        /// <summary>
        /// 按照JT808协议定位消息格式
        /// </summary>
        /// <param name="deviceid">执法记录仪ID</param>
        /// <param name="jingdu">经度</param>
        /// <param name="weidu">纬度</param>
        /// <param name="timestr">GPS上传时间</param>
        public void SetGPSmsg(string deviceid, long jingdu, long weidu, DateTime timestr)
        {
            
            //消息流水号
            int msgid = 1000;
            //消息流水号补全四位
            string msgidstr = string.Empty;
            string str = string.Empty;
            if (msgid.ToString().Length < 4)
            {
               
                for (int i = 0; i < (4 - msgid.ToString().Length); i++)
                {
                    str += "0";
                }
            }
            msgidstr = str + msgid;

            //数据消息头
            string headmsg = string.Empty;
            headmsg = "7E" + "0200" + "0051" + "01356" + deviceid + msgidstr.ToString();
            //7E020000510696987654310063
            //数据位基本信息

            //byte[] by = new byte[3];
            //by[0] = ConvertBCD(DateTime.Now.Year.ToString());
            //by[1] = ConvertBCD(DateTime.Now.Month.ToString());
           // by[2] = ConvertBCD(DateTime.Now.Date.ToString());

           // byte[] bvv = ConvertBCD(timestr.ToString());





            //00000000000C000301A68EB1065FEE79037500510050170621130113 
            string baseinfo = "00000000" + "000C0003" + weidu.ToString("x8") + jingdu.ToString("x8") + "0000" + "0000" + "0000" + timestr.ToString("yyyyMMddHHmmss").Substring(2, 12);
            //                01040001433F     0202023A     03020067     250400000000     300119     310113     A60A0004600485213ABA0C64     AA0D0101BBBBCC31767AD336000090                                                                                // 
           //20180814 164352
           // string addinfo = "010000000000" + "02000000" + "03000000" + "250000000000" + "300000" + "310000" + "A6HA00000000000000000000" + "AA0000000000000000000000000000" + "16" + "7E";
            //获取校验码
            string str2 = (headmsg + baseinfo).ToUpper();
            byte[] aaa = System.Text.Encoding.Default.GetBytes(str2);
            string sss = GetBCCXorCode(aaa);
            if (sss.Length == 1)
            {
             //   sss = "0" + sss;
            }
            //补齐位数
            string sssaa = sss.PadLeft(2, '0');

            SetListBox(listBox1, sssaa);


            string addinfo = sssaa + "7E";

            string devinfo = (headmsg + baseinfo + addinfo).ToUpper();

            SetListBox(listBox1, "数据：" + devinfo);
            //var strToBytes2 = System.Text.Encoding.Default.GetBytes(str2);
           
           
            //获取时间
           // GetCurrentTimeStamp()
            //文件名称 //十五秒变更一次
           
            //


            //string FileNamePath = te_path.Text + "\\" + FileName;

            if (!Directory.Exists(te_path.Text))
            {
                Directory.CreateDirectory(te_path.Text);
            }

            //向文本写入位置数据
            WriteFiles(FileNamePath, devinfo);
            //上传至FTP服务器
         


        }


        /// <summary>
        /// 字符转BCD
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte ConvertBCD(string str)
        {
            return Convert.ToByte(str);
        }











        ///校验码，校验码指从消息头开始，同后一字节异或，直到校验码前一个字节，占用一个字节
        /// <summary>
        /// BCC和校验代码返回16进制
        /// </summary>
        /// <param name="data">需要校验的数据包</param>
        /// <returns></returns>
        public string GetBCCXorCode(byte[] data)
        {

            byte CheckCode = 0;
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                CheckCode ^= data[i];
            }
            return Convert.ToString(CheckCode, 16).ToUpper();
  

           // return "";
        }








        /// <summary>
        /// 创建日志类
        /// </summary>
        public class WriteLog
        {
            /// <summary>
            /// 创建日志文件
            /// </summary>
            /// <param name="ex">异常类</param>
            public static void CreateExceptionLog(Exception ex)
            {
                string path = Application.StartupPath + "\\" + "Log\\ExceptionLog";
                if (!Directory.Exists(path))
                {
                    //创建日志文件夹
                    Directory.CreateDirectory(path);
                }
                //发生异常每天都创建一个单独的日子文件[*.log],每天的错误信息都在这一个文件里。方便查找
                path += "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                WriteLogInfo(ex, path);
            }
            /// <summary>
            /// 写日志信息
            /// </summary>
            /// <param name="ex">异常类</param>
            /// <param name="path">日志文件存放路径</param>
            private static void WriteLogInfo(Exception ex, string path)
            {
                using (StreamWriter sw = new StreamWriter(path, true, Encoding.Default))
                {
                    sw.WriteLine("*****************************************【"
                                   + DateTime.Now.ToLongTimeString()
                                   + "】*****************************************");
                    if (ex != null)
                    {
                        sw.WriteLine("【ErrorType】" + ex.GetType());
                        sw.WriteLine("【TargetSite】" + ex.TargetSite);
                        sw.WriteLine("【Message】" + ex.Message);
                        sw.WriteLine("【Source】" + ex.Source);
                        sw.WriteLine("【StackTrace】" + ex.StackTrace);
                    }
                    else
                    {
                        sw.WriteLine("Exception is NULL");
                    }
                    sw.WriteLine();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn_StartSend.Enabled = false;
            btn_StopSend.Enabled = false;
            btn_logon.Enabled = true;
            btn_logout.Enabled = false;


            




        }




        /// <summary>  
        /// 获取时间戳Timestamp    
        /// </summary>  
        /// <param name="dt"></param>  
        /// <returns></returns>  
        private int GetTimeStamp(DateTime dt)
        {
            DateTime dateStart = new DateTime(1970, 1, 1, 8, 0, 0);
            int timeStamp = Convert.ToInt32((dt - dateStart).TotalSeconds);
            return timeStamp;
        }


        private int GetCurrentTimeStamp()
        {
            DateTime dt = DateTime.Now;
            DateTime dateStart = new DateTime(1970, 1, 1, 8, 0, 0);
            int timeStamp = Convert.ToInt32((dt - dateStart).TotalSeconds);
            return timeStamp;
        }







         public class p4g
        {
            /// <summary>
            /// 4G平台登录信息
            /// </summary>
            public class LoginInfo
            {
                //0：正确返回；其它：失败。详细参见：错误码说明
                public int result
                {
                    get;
                    set;
                }
                //请求时所带的callback
                public string jsession
                {
                    get;
                    set;
                }
            }



            public class Companys
            {
                /// <summary>
                /// 组织ID
                /// </summary>
                public int id
                {
                    get;
                    set;
                }
                /// <summary>
                /// 组织名称
                /// </summary>
                public string nm
                {
                    get;
                    set;
                }
                /// <summary>
                /// 上级组织ID
                /// </summary>
                public int pid
                {
                    get;
                    set;
                }
            }


            public class Dl
            {
                /// <summary>
                /// 设备号
                /// </summary>
                public string id
                {
                    get;
                    set;
                }
                /// <summary>
                /// 设备所属公司
                /// </summary>
                public int pid
                {
                    get;
                    set;
                }
                /// <summary>
                /// IO数目
                /// </summary>
                public int ic
                {
                    get;
                    set;
                }
                /// <summary>
                /// IO名称,以','分隔
                /// </summary>
                public string io
                {
                    get;
                    set;
                }
                /// <summary>
                /// 通道数目
                /// </summary>
                public int cc
                {
                    get;
                    set;
                }
                /// <summary>
                /// 通道名称,以','分隔
                /// </summary>
                public string cn
                {
                    get;
                    set;
                }
                /// <summary>
                /// 温度传感器数目
                /// </summary>
                public int tc
                {
                    get;
                    set;
                }
                /// <summary>
                /// 温度传感器名称,以','分隔
                /// </summary>
                public string tn
                {
                    get;
                    set;
                }
                /// <summary>
                /// 外设参数,按位表示，每位表示一种外设，第一位为支持视频，第二位为油路控制，第三位为电路控制，第四位为tts语音，第五位为数字对讲，第六位为支持抓拍，第七位为支持监听，第八位为油量传感器，第九位为支持对讲，第十位为ODB外设。
                /// </summary>
                public int md
                {
                    get;
                    set;
                }
                /// <summary>
                /// SIM卡号
                /// </summary>
                public string sim
                {
                    get;
                    set;
                }


            }


            public class Vehicles
            {
                public int id
                {
                    get;
                    set;
                }
                public string nm
                {
                    get;
                    set;
                }
                public int ic
                {
                    get;
                    set;
                }
                public int pid
                {
                    get;
                    set;
                }
                public List<Dl> dl
                {
                    get;
                    set;
                }

            }


        

            public class DeviceInfo
            {
                /// <summary>
                /// 0：正确返回；其它：失败。详细参见：错误码说明
                /// </summary>
                public int result
                {
                    get;
                    set;
                }
                /// <summary>
                /// 单位信息
                /// </summary>
                public  List<Companys> companys 
                {
                    get;
                    set;
                }
                public List<Vehicles> vehicles
                {
                    get;
                    set;
                }
                     

            }



            /// <summary>
            /// 设备在线状态
            /// </summary>
            public class onlines
            {
                /// <summary>
                /// 设备号
                /// </summary>
                public string did
                {
                    get;
                    set;
                }
                /// <summary>
                /// 车牌号如果是用设备号查询，则为空。
                /// </summary>
                public string vid
                {
                    get;
                    set;
                }
                /// <summary>
                /// 在线状态,1表示在线，否则不在线。
                /// </summary>
                public int online
                {
                    get;
                    set;
                }
            }

            public class OnlineInfo
            {
                /// <summary>
                /// 0：正确返回；其它：失败。详细参见：错误码说明
                /// </summary>
                public int result
                {
                    get;
                    set;
                }

                /// <summary>
                /// 获取设备在线状态
                /// </summary>
                public List<onlines> onlines
                {
                    get;
                    set;
                }
            }

            /// <summary>
            /// 设备状态信息
            /// </summary>
            public class status
            {
                /// <summary>
                /// 设备号
                /// </summary>
                public string id
                {
                    get;
                    set;
                }
                /// <summary>
                /// 车牌号,如果是用设备号查询，则为空。
                /// </summary>
                public string vid
                {
                    get;
                    set;
                }
                /// <summary>
                /// 经度,如果设备定位无效，值为0。,例如：113231258，真实值为113.231258
                /// </summary>
                public long lng
                {
                    get;
                    set;
                }
                /// <summary>
                /// 纬度,如果设备定位无效，值为0。,例如：39231258，真实值为39.231258
                /// </summary>
                public long lat
                {
                    get;
                    set;
                }
                /// <summary>
                /// 厂家类型
                /// </summary>
                public int ft
                {
                    get;
                    set;
                }
                /// <summary>
                /// 速度,单位: km/h，使用中需先除以10。
                /// </summary>
                public int sp
                {
                    get;
                    set;
                }
                /// <summary>
                /// 在线状态,1表示在线，否则不在线。
                /// </summary>
                public int ol
                {
                    get;
                    set;
                }
                /// <summary>
                /// GPS上传时间
                /// </summary>
                public string gt
                {
                    get;
                    set;
                }
                /// <summary>
                /// 通信协议类型
                /// </summary>
                public int pt
                {
                    get;
                    set;
                }
                /// <summary>
                /// 硬盘类型,1表示SD卡，2表示硬盘，3表示SSD卡。
                /// </summary>
                public int dt
                {
                    get;
                    set;
                }
                /// <summary>
                /// 音频类型
                /// </summary>
                public int ac
                {
                    get;
                    set;
                }
                /// <summary>
                /// 厂家子类型
                /// </summary>
                public int fdt
                {
                    get;
                    set;
                }
                /// <summary>
                /// 网络类型,1表示3G，2表示WIFI
                /// </summary>
                public int net
                {
                    get;
                    set;
                }
                /// <summary>
                /// 网关服务器编号
                /// </summary>
                public string gw
                {
                    get;
                    set;
                }
                /// <summary>
                /// 状态 1,详细参见：设备状态说明
                /// </summary>
                public int s1
                {
                    get;
                    set;
                }
                /// <summary>
                /// 状态 2,详细参见：设备状态说明
                /// </summary>
                public int s2
                {
                    get;
                    set;
                }
                /// <summary>
                /// 状态 3,详细参见：设备状态说明
                /// </summary>
                public int s3
                {
                    get;
                    set;
                }
                /// <summary>
                /// 状态 4,详细参见：设备状态说明
                /// </summary>
                public int s4
                {
                    get;
                    set;
                }
                /// <summary>
                /// 温度传感器 1
                /// </summary>
                public int t1
                {
                    get;
                    set;
                }
                /// <summary>
                /// 温度传感器 2
                /// </summary>
                public int t2
                {
                    get;
                    set;
                }
                /// <summary>
                /// 温度传感器 3
                /// </summary>
                  public int t3
                {
                    get;
                    set;
                }
                /// <summary>
                  /// 温度传感器 4
                /// </summary>
                public int t4
                {
                    get;
                    set;
                }
                /// <summary>
                /// 方向,正北方向为0度，顺时针方向增大，最大值360度。
                /// </summary>
                public int hx
                {
                    get;
                    set;
                }
                /// <summary>
                /// 地图经度,经过转换后的经度
                /// </summary>
                public string mlng
                {
                    get;
                    set;
                }
                /// <summary>
                /// 图纬度,经过转换后的纬度
                /// </summary>
                public string mlat
                {
                    get;
                    set;
                }
                /// <summary>
                /// 停车时长,单位: 秒。
                /// </summary>
                public int pk
                {
                    get;
                    set;
                }
                /// <summary>
                /// 里程,单位: 米
                /// </summary>
                public int lc
                {
                    get;
                    set;
                }
                /// <summary>
                /// 油量,单位: 升，使用中需先除以100。
                /// </summary>
                public int yl
                {
                    get;
                    set;
                }
                /// <summary>
                /// 地理位置,解析后的地理位置 或者 (经过转换后的经度, 经过转换后的纬度)
                /// </summary>
                public string ps
                {
                    get;
                    set;
                }
 
            }


            /// <summary>
            /// 获取GPS状态
            /// </summary>
            public class GetGPSstatus
            {
                /// <summary>
                /// 0：正确返回；其它：失败。详细参见：错误码说明
                /// </summary>
                public int result
                {
                    get;
                    set;
                }

                /// <summary>
                /// 获取设备状态
                /// </summary>
                public List<status> status
                {
                    get;
                    set;
                }
            }



        }

        private void btn_OpenPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择存放位置文件所在文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                string file = dialog.SelectedPath;
                te_path.Text = dialog.SelectedPath;
                //this.LoadingText = "处理中...";
                //this.LoadingDisplay = true;
                //<string> a = DaoRuData;
                // a.BeginInvoke(dialog.SelectedPath, asyncCallback, a);
            }
        }







        /// <summary>
        /// 日志部分
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="type">日志类型</param>
        /// <param name="content">日志文本</param>
        private static void WriteFiles(string fileName, string content)
        {
            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (!File.Exists(fileName))
                    {
                        FileStream fs = File.Create(fileName);
                        fs.Close();
                    }
                    if (File.Exists(fileName))
                    {
                        StreamWriter sw = new StreamWriter(fileName, true, System.Text.Encoding.Default);
                        sw.WriteLine(content);
                        //  sw.WriteLine("----------------------------------------");
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                CreateExceptionLog(ex);
            }
        }

        /// <summary>
        /// 创建日志文件
        /// </summary>
        /// <param name="ex">异常类</param>
        public static void CreateExceptionLog(Exception ex)
        {
            string path = Application.StartupPath + "\\log\\Exception";
            if (!Directory.Exists(path))
            {
                //创建日志文件夹
                Directory.CreateDirectory(path);
            }
            //发生异常每天都创建一个单独的日子文件[*.log],每天的错误信息都在这一个文件里。方便查找
            path += "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            WriteLogInfo(ex, path);
        }
        /// <summary>
        /// 写日志信息
        /// </summary>
        /// <param name="ex">异常类</param>
        /// <param name="path">日志文件存放路径</param>
        private static void WriteLogInfo(Exception ex, string path)
        {
            using (StreamWriter sw = new StreamWriter(path, true, Encoding.Default))
            {
                sw.WriteLine("*****************************************【"
                               + DateTime.Now.ToLongTimeString()
                               + "】*****************************************");
                if (ex != null)
                {
                    sw.WriteLine("【ErrorType】" + ex.GetType());
                    sw.WriteLine("【TargetSite】" + ex.TargetSite);
                    sw.WriteLine("【Message】" + ex.Message);
                    sw.WriteLine("【Source】" + ex.Source);
                    sw.WriteLine("【StackTrace】" + ex.StackTrace);
                }
                else
                {
                    sw.WriteLine("Exception is NULL");
                }
                sw.WriteLine();
            }
        }


        private void WriteFile(object obj)
        {

            while (IsSend)
            {

                FileName = "SYS_HA_" + GetCurrentTimeStamp().ToString() + ".txt";

                FileNamePath = te_path.Text + "\\" + FileName;

                if (PingDevice(tb_ServerIP.Text))
                {
                    if (logininfo != null)
                    {
                        if (logininfo.result == 0)
                        {
                            SetListBox(listBox1, "回调值：" + logininfo.jsession);
                            getuserinfo(logininfo.jsession);
                            Thread.Sleep(1000 * 15);

                        }
                        else
                        {
                            SetListBox(listBox1, "登录4G服务器失败！Error：" + logininfo.result.ToString());

                        }

                    }

                }
            }

        }









        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();



            ThreadPool.QueueUserWorkItem(WriteFile);







           // timer1.Start();
        }


        private void btn_StopSend_Click(object sender, EventArgs e)
        {
            IsSend = false ;
            btn_StartSend.Enabled = true;
            btn_StopSend.Enabled = false;

        }

        private void btn_logout_Click(object sender, EventArgs e)
        {
             btn_StartSend.Enabled = false;
            btn_StopSend.Enabled = false;
            btn_logon.Enabled = true;
            btn_logout.Enabled = false;
        }

        private void btn_StartSend_Click(object sender, EventArgs e)
        {
            IsSend = true;
            if (IsSend)
            {
                btn_StartSend.Enabled = false;
                btn_StopSend.Enabled = true;
                ThreadPool.QueueUserWorkItem(WriteFile);
            }
        }



        private void UpDownFTP(string FilePath)
        {

            /*
            FTPHelper ftp = new FTPHelper();

            //FTPHelper.FtpServerIP = "192.168.2.113";
            //FTPHelper.FtpUserID = "Channing";
            //FTPHelper.FtpPassword = "A9a0Q7q8M9m1";

            FTPHelper.FtpServerIP = "118.122.207.186";
            FTPHelper.FtpUserID = "cdha";
            FTPHelper.FtpPassword = "bzbd0827";
            //文件路径 "/home/beidou/ftp"
            

            //string filenamea = @"C:\Users\Channing\Desktop\bin\avcodec-58.dll";
            FTPHelper.FtpUploadFile(filename, null);
            */

            string [] FileNameStr = FilePath.Replace("\\", "*").Split('*');
            int Num = FileNameStr.Count();
            string FileName = FileNameStr[Num - 1];
            string ObjectiveFilePath = tb_FtpObjectivePath.Text.Trim() + FileName;


            //SFTP.SFTPHelper sFTPHelper = new SFTP.SFTPHelper("118.122.207.186", "22", "cdha", "bzbd0827");
            try
            {

                SFTP.SFTPHelper sFTPHelper = new SFTP.SFTPHelper(te_IP.Text.Trim(), te_Port.Text.Trim(), te_username.Text.Trim(), te_Password.Text.Trim());
                sFTPHelper.Put(FilePath, ObjectiveFilePath);
                SetListBox(listBox1, "向北斗平台推送位置信息文件！");


                //sFTPHelper.Put("text.txt", "/home/cdha/测试/temp.txt");

                //SFTP.SFTPHelper sFTPHelper = new SFTP.SFTPHelper(te_IP.Text, te_Port.Text, te_username.Text, te_Password.Text);
                //sFTPHelper.Put("text.txt", tb_FtpObjectivePath.Text);
            }
            catch (Exception ex)
            {
                SetListBox(listBox1, "向北斗平台FTP服务器推送位置信息文件异常……！");
            }



        }

        private void button1_Click(object sender, EventArgs e)
        {
            //UpDownFTP();
            //callFtp();

            //SFTPHelper sFTPHelper = new SFTPHelper();
            //sFTPHelper.
            SFTP.SFTPHelper sFTPHelper = new SFTP.SFTPHelper("118.122.207.186", "22", "cdha", "bzbd0827");
            sFTPHelper.Put("text.txt", "/home/cdha/测试/temp.txt");

            //SFTP.SFTPHelper sFTPHelper = new SFTP.SFTPHelper(te_IP.Text, te_Port.Text, te_username.Text, te_Password.Text);
            //sFTPHelper.Put("text.txt", tb_FtpObjectivePath.Text);

           






        }









    }

    class SFTP
    {
        /// <summary>
        /// SFTP操作类
        /// </summary>
        public class SFTPHelper
        {
            #region 字段或属性
            private SftpClient sftp;
            /// <summary>
            /// SFTP连接状态
            /// </summary>
            public bool Connected { get { return sftp.IsConnected; } }
            #endregion

            #region 构造
            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="ip">IP</param>
            /// <param name="port">端口</param>
            /// <param name="user">用户名</param>
            /// <param name="pwd">密码</param>
            public SFTPHelper(string ip, string port, string user, string pwd)
            {
                sftp = new SftpClient(ip, Int32.Parse(port), user, pwd);
            }
            #endregion

            #region 连接SFTP
            /// <summary>
            /// 连接SFTP
            /// </summary>
            /// <returns>true成功</returns>
            public bool Connect()
            {
                try
                {
                    if (!Connected)
                    {
                        sftp.Connect();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("连接SFTP失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("连接SFTP失败，原因：{0}", ex.Message));
                }
            }
            #endregion

            #region 断开SFTP
            /// <summary>
            /// 断开SFTP
            /// </summary> 
            public void Disconnect()
            {
                try
                {
                    if (sftp != null && Connected)
                    {
                        sftp.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("断开SFTP失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("断开SFTP失败，原因：{0}", ex.Message));
                }
            }
            #endregion

            #region SFTP上传文件
            /// <summary>
            /// SFTP上传文件
            /// </summary>
            /// <param name="localPath">本地路径</param>
            /// <param name="remotePath">远程路径</param>
            public void Put(string localPath, string remotePath)
            {
                try
                {
                    using (var file = File.OpenRead(localPath))
                    {
                        Connect();
                        sftp.UploadFile(file, remotePath);
                        Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("SFTP文件上传失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("SFTP文件上传失败，原因：{0}", ex.Message));
                }
            }
            #endregion

            #region SFTP获取文件
            /// <summary>
            /// SFTP获取文件
            /// </summary>
            /// <param name="remotePath">远程路径</param>
            /// <param name="localPath">本地路径</param>
            public void Get(string remotePath, string localPath)
            {
                try
                {
                    Connect();
                    var byt = sftp.ReadAllBytes(remotePath);
                    Disconnect();
                    File.WriteAllBytes(localPath, byt);
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("SFTP文件获取失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("SFTP文件获取失败，原因：{0}", ex.Message));
                }

            }
            #endregion

            #region 删除SFTP文件
            /// <summary>
            /// 删除SFTP文件 
            /// </summary>
            /// <param name="remoteFile">远程路径</param>
            public void Delete(string remoteFile)
            {
                try
                {
                    Connect();
                    sftp.Delete(remoteFile);
                    Disconnect();
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("SFTP文件删除失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("SFTP文件删除失败，原因：{0}", ex.Message));
                }
            }
            #endregion

            #region 获取SFTP文件列表
            /// <summary>
            /// 获取SFTP文件列表
            /// </summary>
            /// <param name="remotePath">远程目录</param>
            /// <param name="fileSuffix">文件后缀</param>
            /// <returns></returns>
            public ArrayList GetFileList(string remotePath, string fileSuffix)
            {
                try
                {
                    Connect();
                    var files = sftp.ListDirectory(remotePath);
                    Disconnect();
                    var objList = new ArrayList();
                    foreach (var file in files)
                    {
                        string name = file.Name;
                        if (name.Length > (fileSuffix.Length + 1) && fileSuffix == name.Substring(name.Length - fileSuffix.Length))
                        {
                            objList.Add(name);
                        }
                    }
                    return objList;
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("SFTP文件列表获取失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("SFTP文件列表获取失败，原因：{0}", ex.Message));
                }
            }
            #endregion

            #region 移动SFTP文件
            /// <summary>
            /// 移动SFTP文件
            /// </summary>
            /// <param name="oldRemotePath">旧远程路径</param>
            /// <param name="newRemotePath">新远程路径</param>
            public void Move(string oldRemotePath, string newRemotePath)
            {
                try
                {
                    Connect();
                    sftp.RenameFile(oldRemotePath, newRemotePath);
                    Disconnect();
                }
                catch (Exception ex)
                {
                    // TxtLog.WriteTxt(CommonMethod.GetProgramName(), string.Format("SFTP文件移动失败，原因：{0}", ex.Message));
                    throw new Exception(string.Format("SFTP文件移动失败，原因：{0}", ex.Message));
                }
            }
            #endregion

        }
    }







}
