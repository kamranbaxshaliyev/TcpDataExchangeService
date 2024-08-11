using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TcpDataExchangeService
{
    internal class LogManager
    {
        private string sDirPath;
        private bool bSingleFile;
        private double nMaxSize;
        private bool bLogDate;
        private string sSeparator;
        private string sGuid;
        private string sAddress;
        public LogManager(string sDirPath, bool bSingleFile, string sMaxSize, bool bLogDate, string sSeparator)
        {
            this.sDirPath = sDirPath;
            this.bSingleFile = bSingleFile;
            this.nMaxSize = getMaxSize(sMaxSize);
            this.bLogDate = bLogDate;
            this.sSeparator = sSeparator;
            this.sAddress = "";
            this.sGuid = "";
        }
        public LogManager(string sDirPath, bool bSingleFile, string sMaxSize, bool bLogDate, string sSeparator, string sAddress, string sGuid)
        {
            this.sDirPath = sDirPath;
            this.bSingleFile = bSingleFile;
            this.nMaxSize = getMaxSize(sMaxSize);
            this.bLogDate = bLogDate;
            this.sSeparator = sSeparator;
            this.sAddress = "_" + sAddress + "_";
            this.sGuid = sGuid;
        }

        public bool Log(string sLog)
        {
            try
            {
                string sDateTime = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                byte[] arLogSize = Encoding.UTF8.GetBytes($"{sDateTime} - " + sLog + this.sSeparator);
                FileStream oFileStream = getFileStream(arLogSize.LongLength);
                StreamWriter oStreamWriter;

                if (oFileStream == null)
                    throw new Exception();
                else
                    oStreamWriter = new StreamWriter(oFileStream);

                oStreamWriter.AutoFlush = true;

                if (this.bLogDate)
                    oStreamWriter.WriteLine($"{sDateTime} - " + sLog + this.sSeparator);
                else
                    oStreamWriter.WriteLine(sLog + this.sSeparator);

                oStreamWriter.Close();
                oFileStream.Close();

                return true;
            }

            catch (Exception e)
            {
                return false;
            }
        }

        private FileStream getFileStream(long nLogSize)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.sDirPath))
                    this.sDirPath = $@"{AppDomain.CurrentDomain.BaseDirectory}\log";

                if (this.bSingleFile)
                {
                    if (File.Exists(this.sDirPath + @"\log" + this.sAddress + this.sGuid + ".txt"))
                    {
                        FileInfo oFileInfo = new FileInfo(this.sDirPath + @"\log" + this.sAddress + this.sGuid + ".txt");

                        if (nLogSize > this.nMaxSize)
                            File.WriteAllText(oFileInfo.FullName, string.Empty);

                        else if (oFileInfo.Length + nLogSize > this.nMaxSize)
                        {
                            double nRequiredSize = this.nMaxSize - nLogSize;

                            while (oFileInfo.Length > nRequiredSize)
                            {
                                string[] arLines = File.ReadAllLines(oFileInfo.FullName);

                                int i;

                                for (i = 0; i < arLines.Length; i++)
                                {
                                    if (arLines[i].Contains(this.sSeparator))
                                        break;
                                }

                                string sTempFilePath = Path.GetTempFileName();
                                File.WriteAllLines(sTempFilePath, arLines.Skip(i + 1));
                                File.Delete(oFileInfo.FullName);
                                File.Move(sTempFilePath, oFileInfo.FullName);
                                oFileInfo = new FileInfo(this.sDirPath + @"\log" + this.sAddress + this.sGuid + ".txt");
                            }
                        }
                    }

                    return new FileStream(this.sDirPath + @"\log" + this.sAddress + this.sGuid + ".txt", FileMode.Append, FileAccess.Write);
                }

                else
                {
                    FileStream oFileStream;

                    if (File.Exists(this.sDirPath + @"\log" + this.sAddress + this.sGuid + "_0.txt"))
                    {
                        DirectoryInfo oDirInfo = new DirectoryInfo(this.sDirPath);
                        FileInfo[] arFiles = oDirInfo.GetFiles();
                        IOrderedEnumerable<FileInfo> oSortedFiles = arFiles.OrderByDescending(file => file.Name.Contains("log" + this.sAddress + this.sGuid));
                        FileInfo oLastFileInfo = oSortedFiles.First();

                        if (oLastFileInfo.Length + nLogSize > this.nMaxSize)
                        {
                            string sFileName = oLastFileInfo.Name;
                            string sIndex = sFileName.Substring(sFileName.LastIndexOf(".") - 1, 1);
                            int nIndex = int.Parse(sIndex) + 1;

                            oFileStream = new FileStream(this.sDirPath + @"\log" + this.sAddress + this.sGuid + "_" + nIndex + ".txt", FileMode.Append, FileAccess.Write);
                        }

                        else
                            oFileStream = new FileStream(oLastFileInfo.FullName, FileMode.Append, FileAccess.Write);

                        return oFileStream;
                    }
                    else
                    {
                        oFileStream = new FileStream(this.sDirPath + @"\log" + this.sAddress + this.sGuid + "_0.txt", FileMode.Append, FileAccess.Write);

                        return oFileStream;
                    }
                }
            }

            catch (Exception e)
            {
                return null;
            }
        }

        private double getMaxSize(string sMaxSize)
        {
            double nDefault = 10 * 1024.0;

            if (Regex.IsMatch(sMaxSize, @"^\d{1,}[A-Za-z]{1}$"))
            {
                string[] arUnits = new string[] { "B", "K", "M", "G" };

                for (int i = 0; i < arUnits.Length; i++)
                {
                    if (sMaxSize.ToUpper().Contains(arUnits[i]))
                    {
                        string sSize = Regex.Match(sMaxSize, @"\d{1,}").Value;
                        double nSize = double.Parse(sSize) * Math.Pow(1024.0, i);
                        return nSize;
                    }
                }

                return nDefault;
            }

            else if (Regex.IsMatch(sMaxSize, @"^\d{1,}$"))
            {
                string sSize = Regex.Match(sMaxSize, @"\d{1,}").Value;

                return double.Parse(sSize);
            }

            else
                return nDefault;
        }
    }
}
