using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpDataExchangeService
{
    public partial class Service1 : ServiceBase
    {
        private TcpListener oTcpListener = null;
        private LogManager oLogManager = null;
        private Thread oThread = null;
        private Thread oClientThread = null;
        private bool bStopping = false;

        private const string MES_ATT_STOP = "Attempt to stop service";
        private const string MES_STOP = "Service stopped";
        private const string MES_ER = "Exception: ";
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            const string MES_ATT_START = "Attempt to start service";
            const string MES_START = "Service started";
            const string MES_CONNECT = "Connected to client";
            const string MES_TIMEOUT = "Connection to host closed - Timed out";
            const string MES_SENT = "Sent: ";
            const string MES_RECEIVED = "Received: ";
            const string MES_STOP_CLOSE = "Connection to host closed - Stopped";
            const string MES_ERROR = "Connection to host closed - Error";

            const string sIP = "ip";
            const string sPORT = "port";
            const string sLOGDIR = "log_dir";
            const string sLOGSINGLE = "log_single";
            const string sLOGSIZE = "log_size";
            const string sLOGDATE = "log_date";
            const string sLOGSEP = "log_sep";
            const string sTIMEOUT = "timeout";
            const string sSTOP = "stop";
            const string sSTOP_DEFAULT = "exit";

            string sCONFIG_PATH = $@"{AppDomain.CurrentDomain.BaseDirectory}\config.txt";

            try
            {
                //Debugger.Launch();
                oLogManager = new LogManager("", true, "10k", true, Environment.NewLine);

                string[] arLines = File.ReadAllLines(sCONFIG_PATH);

                Dictionary<string, string> arConfig = new Dictionary<string, string>();

                foreach (string p in arLines)
                {
                    string[] arPairs = p.Split(Char.Parse(":"));

                    if (arPairs.Length == 2)
                        arConfig[arPairs[0]] = arPairs[1];
                }

                string sFilePath = arConfig[sLOGDIR];

                if (sFilePath != "" && sFilePath != null)
                    Directory.CreateDirectory(sFilePath);

                if (!bool.TryParse(arConfig[sLOGSINGLE], out bool bSingle))
                    bSingle = true;

                if (!bool.TryParse(arConfig[sLOGDATE], out bool bLogDate))
                    bLogDate = true;

                string sMaxSize = arConfig[sLOGSIZE];

                string sSep = arConfig[sLOGSEP];
                if (sSep.Trim() == "" || sSep.Trim() == null)
                    sSep = Environment.NewLine;

                oLogManager = new LogManager(sFilePath, bSingle, sMaxSize, bLogDate, sSep);

                oLogManager.Log(MES_ATT_START);

                string sIp = arConfig[sIP];
                int nPort = int.Parse(arConfig[sPORT]);

                if (!int.TryParse(arConfig[sTIMEOUT], out int nTimeOut))
                    nTimeOut = 30000;

                string sStop = arConfig[sSTOP];
                if (sStop.Trim() == "")
                    sStop = sSTOP_DEFAULT;

                oTcpListener = new TcpListener(IPAddress.Parse(sIp), nPort);
                oTcpListener.Start();

                oThread = new Thread(
                () =>
                {
                    while (!this.bStopping)
                    {
                        if (oTcpListener.Pending())
                        {
                            oClientThread = new Thread(
                            () =>
                            {
                                TcpClient oClient = null;
                                NetworkStream oStream = null;
                                LogManager oNewLogManager = null;

                                try
                                {
                                    if (oTcpListener.Pending())
                                    {
                                        oClient = oTcpListener.AcceptTcpClient();
                                        oStream = oClient.GetStream();

                                        IPEndPoint oEndPoint = oClient.Client.RemoteEndPoint as IPEndPoint;
                                        string sAddress = oEndPoint.Address.ToString();

                                        oNewLogManager = new LogManager(sFilePath, bSingle, sMaxSize, bLogDate, sSep, sAddress, Guid.NewGuid().ToString());

                                        oNewLogManager.Log(MES_CONNECT);

                                        byte[] arRead = new byte[8192];
                                        string sRead = null;
                                        string sReadFull = null;

                                        byte[] arConnected = Encoding.UTF8.GetBytes("Connected" + Environment.NewLine + Environment.NewLine);
                                        oStream.Write(arConnected, 0, arConnected.Length);

                                        byte[] arSent = Encoding.UTF8.GetBytes(MES_SENT);
                                        oStream.Write(arSent, 0, arSent.Length);

                                        Task oTimeoutTask = Task.Delay(TimeSpan.FromMilliseconds(nTimeOut));

                                        while (oStream.CanRead)
                                        {
                                            Task<int> oReadTask = oStream.ReadAsync(arRead, 0, arRead.Length);

                                            Task.WaitAny(oReadTask, oTimeoutTask);

                                            if (oTimeoutTask.Status == TaskStatus.RanToCompletion)
                                            {
                                                byte[] arTimedOut = Encoding.UTF8.GetBytes(Environment.NewLine + Environment.NewLine + MES_TIMEOUT);
                                                oStream.Write(arTimedOut, 0, arTimedOut.Length);
                                                oStream.Close();
                                                oClient.Close();

                                                oNewLogManager.Log(MES_TIMEOUT);

                                                break;
                                            }
                                            else
                                            {
                                                if ((sRead = Encoding.UTF8.GetString(arRead, 0, oReadTask.Result)) != Environment.NewLine)
                                                {
                                                    sReadFull += sRead;
                                                }
                                                else
                                                {
                                                    oNewLogManager.Log(MES_SENT + sReadFull);

                                                    if (sReadFull == sStop)
                                                    {
                                                        byte[] arTimedOut = Encoding.UTF8.GetBytes(Environment.NewLine + Environment.NewLine + MES_STOP_CLOSE);
                                                        oStream.Write(arTimedOut, 0, arTimedOut.Length);

                                                        oStream.Close();
                                                        oClient.Close();

                                                        oNewLogManager.Log(MES_STOP_CLOSE);

                                                        break;
                                                    }

                                                    byte[] arReceived = Encoding.UTF8.GetBytes(MES_RECEIVED + sReadFull + Environment.NewLine + Environment.NewLine);
                                                    oStream.Write(arReceived, 0, arReceived.Length);

                                                    oNewLogManager.Log(MES_RECEIVED + sReadFull);

                                                    oStream.Write(arSent, 0, arSent.Length);
                                                    sReadFull = null;
                                                    sRead = null;
                                                    oTimeoutTask = Task.Delay(TimeSpan.FromMilliseconds(nTimeOut));
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    oNewLogManager.Log(MES_ER + ex);

                                    if (oStream != null)
                                    {
                                        byte[] arClosed = Encoding.UTF8.GetBytes(MES_ERROR + Environment.NewLine + MES_ER + ex);
                                        oStream.Write(arClosed, 0, arClosed.Length);
                                        oStream.Close();
                                    }

                                    if (oClient != null)
                                    {
                                        oClient.Close();
                                        oNewLogManager.Log(MES_ERROR);
                                    }
                                }
                            });

                            oClientThread.IsBackground = true;
                            oClientThread.Start();
                        }
                    }
                });

                oThread.Start();

            }
            catch (Exception ex)
            {
                oLogManager.Log(MES_ER + ex);
            }

            oLogManager.Log(MES_START);
        }

        protected override void OnStop()
        {
            try
            {
                oLogManager.Log(MES_ATT_STOP);

                this.bStopping = true;

                if (oThread != null)
                    oThread.Join();

                if (oTcpListener != null)
                    oTcpListener.Stop();

                oLogManager.Log(MES_STOP);
            }
            catch (Exception ex)
            {
                oLogManager.Log(MES_ER + ex);
            }
        }
    }
}