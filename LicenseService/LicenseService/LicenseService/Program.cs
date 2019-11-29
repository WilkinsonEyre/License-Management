using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace LicenseService
{
    class Program
    {


        public static void Main(string[] args)
        {
            Console.WriteLine("Parser Started");

            int a = 0;
            while (a == 0)
            {
                ReadProjectFiles();
                Console.WriteLine("Log Last Checked at " + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt"));
                Console.WriteLine("----------------");
                System.Threading.Thread.Sleep(60000);
                
            }
        }
        
        static string ProjectsFolder = null;

        //place your server name
        static string ServerName = "";
        //place your Database name
        static string DatabaseName = "";
        //place your User Name to Access the database 
        static string UserName = "";
        //place your Password to Access the database
        static string Password = "";
        //place your Path for your license Log File
        static string LicenseLogPath = "";

        static string connectionString = "SERVER=" + ServerName + ";" + "DATABASE=" + DatabaseName + ";" + "UID=" + UserName + ";" + "PASSWORD=" + Password + ";";


        static void ReadProjectFiles()
        {

            List<string> Lines = new List<string>();
            
            DateTime FirstDay = new DateTime(2019, 10, 18);
            DateTime CurrentDay = FirstDay;

            int lastExportedHour = 24; bool lastExportedHourFirstTime = true;

            int counter = 0;
            string line;

            String applicationPath = System.IO.Directory.GetCurrentDirectory();

            string AutodeskLog = LicenseLogPath;

            System.IO.FileStream fs = new System.IO.FileStream(@AutodeskLog, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);

            System.IO.StreamReader sr = new System.IO.StreamReader(fs);

            List<DataLines> DataLinesFromFile = new List<DataLines>();

            string PreviousTime = "";
            string PreviousUserName = "";
            string PreviousAction = "";
            int UserSequence = 0;
            int LineCounter = 0;
            int ParsingLine = 0;


            while ((line = sr.ReadLine()) != null)
            {
                LineCounter++;

                if (line.Length != 0)
                {
                    if (line.Contains("TIMESTAMP "))
                    {
                        int pos = line.IndexOf("TIMESTAMP ");

                        string day = line.Substring(pos+10, line.Length - pos-10);
                        string [] split = day.Split('/');
                        CurrentDay = new DateTime(Int32.Parse(split[2]), Int32.Parse(split[0]), Int32.Parse(split[1]));
                    }
                    if (line.Contains("Date and Time"))
                    {
                        CurrentDay = Convert.ToDateTime(line.Substring(52, 11));
                        int debug = 0;
                    }
                    
                    if (line=="=== Reread Info ===")
                    {
                        int debug = 0;
                    }

                    if (line.Substring(0, 1)!="-" & line.Substring(0, 1) != "=" & line.Substring(0, 4) != "Shut" & line.Substring(0, 1) != "N" & line.Substring(0, 1) != "R") //To avoid the "Shut down FlexNet adskflex license server system on machine BGL-LIC-P-0775"
                    {
                        string exportedTime_Only = line.Substring(0, 8);
                        exportedTime_Only = exportedTime_Only.Replace(" ", "0");
                        string exportedHour = exportedTime_Only.Substring(0, 2);

                        if (lastExportedHourFirstTime)
                        {
                            lastExportedHour = Int32.Parse(exportedHour);
                            lastExportedHourFirstTime = false;
                        }

                        if (Int32.Parse(exportedHour) < lastExportedHour)
                        {
                            //AllDays.Add(new DateTime(CurrentDay.Year, CurrentDay.Month, CurrentDay.Day));
                            CurrentDay = new DateTime (CurrentDay.Year, CurrentDay.Month, CurrentDay.Day).AddDays(+1);
                        }

                        lastExportedHour = Int32.Parse(exportedHour);

                        if (line.IndexOf(": \"") != -1)
                        {
                            if (line.IndexOf("@") != -1)
                            {
                                //Action

                                string exportedTime = CurrentDay.ToString("yyyy-MM-dd") + " " + exportedTime_Only;// +"."+ UserSequence.ToString("000");
                                string exportedTimeWithDecimals="";

                                List<int> pos1 = AllIndexesOf(line, ")");
                                List<int> pos2 = AllIndexesOf(line, ":");
                                string exportedAction = line.Substring(pos1[0] + 2, pos2[2] - pos1[0] - 2);

                                //LicenseNumber
                                List<int> pos3 = AllIndexesOf(line, "\"");
                                string exportedLicenseNumber = line.Substring(pos3[0] + 1, pos3[1] - pos3[0] - 1);

                                //UserName
                                List<int> pos4 = AllIndexesOf(line, "@");
                                string exportedUserName = line.Substring(pos3[1] + 2, pos4[0] - pos3[1] - 2);



                                if (exportedUserName.IndexOf(")") != -1)
                                {
                                    int pos4_1 = exportedUserName.IndexOf(")");
                                    exportedUserName = exportedUserName.Substring(pos4_1 + 2, exportedUserName.Length - pos4_1 - 2);
                                }

                                //ComputerName
                                string exportedComputerName = line.Substring(pos4[0] + 1, 8);

                                //Details
                                string exportedDetails = "";
                                List<int> pos5 = AllIndexesOf(line, "(");
                                if (pos5.Count == 2)
                                    //exportedDetails = line.Substring(pos5[1] + 1, pos1[1] - pos5[1] - 1);
                                    exportedDetails = line.Substring(pos5[1] + 1, line.Length - pos5[1] - 2);

                                if (ParsingLine==0)
                                {
                                    if (exportedTime.Equals(PreviousTime))
                                    {
                                        UserSequence++;
                                        exportedTimeWithDecimals = CurrentDay.ToString("yyyy-MM-dd") + " " + exportedTime_Only + "." + UserSequence.ToString("000");
                                    }
                                    else
                                    {
                                        UserSequence = 0;
                                        exportedTimeWithDecimals = CurrentDay.ToString("yyyy-MM-dd") + " " + exportedTime_Only + "." + UserSequence.ToString("000");
                                    }
                                    
                                    DataLinesFromFile.Add(new DataLines(exportedTimeWithDecimals, exportedAction, exportedLicenseNumber, exportedUserName, exportedComputerName, exportedDetails));

                                    if (exportedAction != "UNSUPPORTED")
                                    {
                                        ParsingLine = 1;
                                    }
                                }
                                else
                                {
                                    exportedTimeWithDecimals = CurrentDay.ToString("yyyy-MM-dd") + " " + exportedTime_Only + "." + UserSequence.ToString("000");
                                    DataLinesFromFile[DataLinesFromFile.Count - 1].setAdditionalData(exportedAction, exportedLicenseNumber, exportedDetails);

                                    ParsingLine = 0;
                                }

                                PreviousTime = exportedTime;
                                PreviousUserName = exportedUserName;
                                PreviousAction = exportedAction;
                            }
                        }
                    }

                }

            }
            sr.Close();

            bool ItemExists = false;

            int PreviousLastEntry = 0;

            //read the last entry (if this exists)
            if (System.IO.File.Exists(applicationPath + "\\lastEntry"))
            {
                using (System.IO.BinaryReader b = new System.IO.BinaryReader(System.IO.File.Open(applicationPath + "\\lastEntry", System.IO.FileMode.Open)))
                {

                    int pos = 0;

                    int length = (int)b.BaseStream.Length;

                        int v = b.ReadInt32();

                        PreviousLastEntry=v;
                        PreviousLastEntry++;
                }

            }

            int NewEntry;
            if (PreviousLastEntry < DataLinesFromFile.Count) NewEntry = PreviousLastEntry; else NewEntry = 0;
            
            for (int ii = NewEntry; ii < DataLinesFromFile.Count; ii++)
            {

                {


                    DataLines i = DataLinesFromFile[ii];

                    try
                    {
                        MySqlConnection connection = new MySqlConnection(connectionString);

                        string query_revit_licence_autodesk = "INSERT INTO input_table (`DateTime`,`User`,`Computer`,`Action`,`LicenseCode`, `ApplicationName`)  VALUES (\"" + i.Time + "\",\"" + i.UserName + "\",\"" + i.ComputerName + "\",\"" + i.Action + "\",\"" + i.LicenseNumber + "\",\"" + i.Version + "\")";

                        connection.Open();
                        MySqlCommand cmd = new MySqlCommand(query_revit_licence_autodesk, connection);
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine(query_revit_licence_autodesk);
                    }
                    catch (Exception e)
                    {
                        ItemExists = true;
                        try
                        {
                            Console.WriteLine(e.ToString());

                        }
                        catch (Exception e2)
                        {

                        }
                    }

                }

                //storing the last entry of the database
                System.IO.FileStream fs_=null;
                fs_ = new System.IO.FileStream(@applicationPath + "\\lastentry", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
                using (fs_)
                {
                    string lastEntry = ii.ToString();
                    int lastEntryDigits = lastEntry.Length;
                    string info_ = "";

                    for (int i= 0;i<10-lastEntry.Length; i++)
                    {
                        info_ = info_ + "0";
                    }
                    info_ = info_ + lastEntry;

                    byte[] info = BitConverter.GetBytes(ii);
                    fs_.Write(info, 0, info.Length);
                }

            }

        }

        class DataLines
        {
            public string Time = "";
            public string Action ="";
            public string LicenseNumber = "";
            public string UserName = "";
            public string ComputerName = "";
            public string Details = "";
            public string Version = "";

            public DataLines(string exportedTime, string exportedAction, string exportedLicenseNumber, string exportedUserName, string exportedComputerName, string exportedDetails)
            {
                Time = exportedTime;
                UserName = exportedUserName;
                ComputerName = exportedComputerName;

                {
                    //expand those if necessary
                    if (exportedLicenseNumber.IndexOf("2014") != -1 ||
                    exportedLicenseNumber.IndexOf("2015") != -1 ||
                    exportedLicenseNumber.IndexOf("2016") != -1 ||
                    exportedLicenseNumber.IndexOf("2017") != -1 ||
                    exportedLicenseNumber.IndexOf("2018") != -1 ||
                    exportedLicenseNumber.IndexOf("2019") != -1 ||
                    exportedLicenseNumber.IndexOf("2020") != -1
                    )
                        Version = exportedLicenseNumber;
                    else
                        LicenseNumber = exportedLicenseNumber;

                    Action = exportedAction;
                    Details = exportedDetails;
                }
            }

            public void setAdditionalData(string exportedAction_next, string exportedLicenseNumber, string exportedDetails)
            {
                if (!exportedAction_next.Equals("DENIED") && !exportedAction_next.Equals("UNSUPPORTED")) 
                {
                    if (Action.Equals(""))
                    {
                        Action = exportedAction_next;
                        Details = exportedDetails;
                        if (exportedLicenseNumber.IndexOf("2014") != -1 ||
                        exportedLicenseNumber.IndexOf("2015") != -1 ||
                        exportedLicenseNumber.IndexOf("2016") != -1 ||
                        exportedLicenseNumber.IndexOf("2017") != -1 ||
                        exportedLicenseNumber.IndexOf("2018") != -1 ||
                        exportedLicenseNumber.IndexOf("2019") != -1 ||
                        exportedLicenseNumber.IndexOf("2020") != -1
                        )
                            Version = exportedLicenseNumber;
                        else
                            LicenseNumber = exportedLicenseNumber;
                    }
                    else if (exportedAction_next.Equals(Action))
                    {
                        if (exportedLicenseNumber.IndexOf("2014") != -1 ||
                            exportedLicenseNumber.IndexOf("2015") != -1 ||
                            exportedLicenseNumber.IndexOf("2016") != -1 ||
                            exportedLicenseNumber.IndexOf("2017") != -1 ||
                            exportedLicenseNumber.IndexOf("2018") != -1 ||
                            exportedLicenseNumber.IndexOf("2019") != -1 ||
                            exportedLicenseNumber.IndexOf("2020") != -1
                            )
                            Version = exportedLicenseNumber;
                        else
                            LicenseNumber = exportedLicenseNumber;
                    }
                }

            }
        }
        public static List<int> AllIndexesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }


    }
}
