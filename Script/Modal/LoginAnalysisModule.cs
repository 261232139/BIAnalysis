using BIAnalysis.Script.BI;
using BIAnalysis.Script.Data;
using BIAnalysis.Script.Modal;
using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BIAnalysis.Script.Modal
{
    public static class ProgressConst
    {
        public static readonly float BI_INIT = 0;
        public static readonly float LAUNCH = 1;
        public static readonly float SPLASH = 2;

        public static readonly float START_CHECK_VERSION = 3;
        public static readonly float NEED_UPDATE_GAME_VER = 3.1f;
        public static readonly float NO_NEED_UPDATE_GAME_VER = 3.2f;
        //public static readonly float DIALOG_FORCE_UPDATE = 3.3f;
        //public static readonly float DIALOG_CHOOSE_FORCE_UPDATE = 3.4f;

        public static readonly float START_UPDATE_VERSION = 4;
        public static readonly float UPDATE_SUCCESS = 4.1f;
        public static readonly float UPDATE_VERSION_FAILED = 4.2f;

        public static readonly float START_CHECK_RES = 5;
        public static readonly float NEED_UPDATE_RES = 5.1f;
        public static readonly float NO_NEED_UPDATE_RES = 5.2f;
        public static readonly float START_UPDATE_RES = 6f;

        public static readonly float FSM_PRELOAD = 7;
        public static readonly float FSM_LOGIN = 8;
        public static readonly float FSM_CHANGE_SCENE = 9;
        public static readonly float FSM_MAIN = 10;

        public static readonly float LOAD_MAIN = 11;
        public static readonly float LOAD_ACTOR = 12;
        public static readonly float LOADING_COMPLETE = 13;
    }

    public class LoginAnalysisModule
    {
        protected Dictionary<string, LoginUserData> dicUserData = new Dictionary<string, LoginUserData>();
        protected DataTable dataTable;
        private string mExcelFilePath;
        private float[] mUpdateResCondition = new float[] { ProgressConst.START_UPDATE_RES };
        private ExcelPackage excelPackage;
        private Dictionary<string, WorksheetWriter> dicWorkSheet = new Dictionary<string, WorksheetWriter>();
        private readonly string SAVE_EXCEL_PATH = @"D:\mabi.xlsx";

        private static LoginAnalysisModule _instance;
        public static LoginAnalysisModule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoginAnalysisModule();
                }
                return _instance;
            }
        }

        virtual public void Start()
        {
            OpenFileDialog folderBrowser = new OpenFileDialog
            {
                // Set validate names and check file exists to false otherwise windows will
                // not let you select "Folder Selection."
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                Multiselect = true,
                Filter = "Excel Files(.xlsx)|*.xlsx| Excel Files(.xls)|*.xls| Excel Files(*.xlsm)|*.xlsm | Excel Files(.csv)|*.csv",
                // Always default to Folder Selection.
                FileName = "Folder Selection."
            };
            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            excelPackage = new ExcelPackage();
            StartLoadAllFiles(folderBrowser.FileNames);
        }

        virtual protected void StartLoadAllFiles(string[] fileNames)
        {
            foreach (var excelFilePath in fileNames)
            {
                LoadExcel(excelFilePath); //load excel file
            }
            excelPackage.SaveAs(new FileInfo(this.SAVE_EXCEL_PATH));
            Console.WriteLine("保存路径:" + this.SAVE_EXCEL_PATH);
            excelPackage.Dispose();
        }
        private WorksheetWriter GetExcelWorksheet(string name, string[] colHeaders = null, string[] rowHeaders = null)
        {
            if (!this.dicWorkSheet.TryGetValue(name, out WorksheetWriter worksheetWriter))
            {
                var workSheet = excelPackage.Workbook.Worksheets.Add(name);
                worksheetWriter = new WorksheetWriter(workSheet, colHeaders, rowHeaders);
                dicWorkSheet.Add(name, worksheetWriter);
            }
            return worksheetWriter;
        }

        virtual protected void LoadExcel(string excelFilePath)
        {
            this.ClearLog();
            this.mExcelFilePath = excelFilePath;
            dicUserData = new Dictionary<string, LoginUserData>();

            //ExcelDataReader
            this.Log($"Excel is loading...  {Path.GetFileName(this.mExcelFilePath)}");
            dataTable = ResourceManager.LoadExcel(this.mExcelFilePath);//set header names

            int rowCount = dataTable.Rows.Count;
            for (int row = 1; row < rowCount; row++)
            {
                string jsonData = GetCellValue(row, 5);
                jsonData = jsonData.Replace("params", "param");
                BIJsonData data = JsonConvert.DeserializeObject<BIJsonData>(jsonData);
                string deviceID = data.body.device.userid;
                if (this.dicUserData.TryGetValue(deviceID, out LoginUserData userData))
                {
                    userData.AddData(data);
                }
                else
                {
                    userData = new LoginUserData(deviceID);
                    userData.AddData(data);
                    this.dicUserData.Add(deviceID, userData);
                }
            }

            foreach (var kv in dicUserData)
            {
                var userData = kv.Value;
                userData.Sort();
                //if (userData.list[0].body.param.GameLevel == 0 && string.IsNullOrEmpty(userData.list[0].body.param.userID))
                //{
                //    continue;
                //}
                userData.Analysis();
            }

            //卡的次数
            WorksheetWriter worksheetWriter = this.GetExcelWorksheet("卡住", null, new string[] { "progress" });
            this.CalculateBreakTimes(worksheetWriter, null);
            this.CalculateBreakTimes(worksheetWriter, this.mUpdateResCondition);

            //各阶段时间
            worksheetWriter = this.GetExcelWorksheet("各阶段耗时", null, new string[] { "progress" });
            this.CalculateProgressAvgTimes(worksheetWriter, null);
            this.CalculateProgressAvgTimes(worksheetWriter, this.mUpdateResCondition);
            this.CalculateProgressAvgTimes(worksheetWriter, null, ProgressConst.START_UPDATE_RES);
            this.CalculateProgressAvgTimes(worksheetWriter, new float[] { ProgressConst.NEED_UPDATE_RES });

            this.Log("-----------------------------------------------------");

            //登录成功率
            worksheetWriter = this.GetExcelWorksheet("登录成功率", null);
            this.CalculateEnterSuccessPercent(worksheetWriter, null);
            //this.CalculateEnterSuccessPercent(worksheetWriter, this.mUpdateResCondition);
            //this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.START_CHECK_RES, ProgressConst.NEED_UPDATE_RES, ProgressConst.START_UPDATE_RES, ProgressConst.FSM_PRELOAD });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.BI_INIT });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.LAUNCH });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.SPLASH });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.START_CHECK_VERSION });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.NEED_UPDATE_GAME_VER });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.NO_NEED_UPDATE_GAME_VER });

            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.START_UPDATE_VERSION });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.UPDATE_SUCCESS });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.UPDATE_VERSION_FAILED });

            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.START_CHECK_RES });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.NEED_UPDATE_RES });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.NO_NEED_UPDATE_RES });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.START_UPDATE_RES });

            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.FSM_PRELOAD });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.FSM_LOGIN });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.FSM_CHANGE_SCENE });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.FSM_MAIN });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.LOAD_MAIN });
            this.CalculateEnterSuccessPercent(worksheetWriter, new float[] { ProgressConst.LOAD_ACTOR });

            this.SaveLog();
        }

        private string GetConditon(float[] conditon)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var f in conditon)
            {
                sb.Append(f.ToString());
                sb.Append("|");
            }
            return sb.ToString();
        }

        private void CalculateBreakTimes(WorksheetWriter worksheetWriter, float[] condition)
        {
            var list = this.GetLoginDatas(condition);

            string conditionStr;
            if (condition != null)
            {
                conditionStr = $"\n过滤条件: {this.GetConditon(condition)}   数据量:{list.Count}";
            }
            else
            {
                conditionStr = $"n过滤条件:无   数据量:{list.Count}";
            }
            this.Log(conditionStr);

            Dictionary<float, int> dicBreakTotal = new Dictionary<float, int>();
            foreach (var loginData in list)
            {
                float progress = loginData.GetBreakProgress();
                if (progress < 0)
                {
                    continue;
                }
                if (dicBreakTotal.ContainsKey(progress))
                {
                    ++dicBreakTotal[progress];
                }
                else
                {
                    dicBreakTotal.Add(progress, 1);
                }
            }

            this.Log("-----------------------------------------------------");
            worksheetWriter.AddHeader(this.ExcelName + "_" + conditionStr);
            foreach (var kv in dicBreakTotal)
            {
                worksheetWriter.WriteRowData(kv.Key.ToString(), kv.Value.ToString());
                this.Log($"各阶段卡的次数{kv.Key}:-----     {kv.Value}");
            }
            worksheetWriter.ChangeNextCol();
        }

        //各阶段平均时间
        private void CalculateProgressAvgTimes(WorksheetWriter worksheetWriter, float[] condition, float unUsedCondition = -1)
        {
            var list = this.GetLoginDatas(condition, unUsedCondition);
            Dictionary<float, AverageValue> dicPhaseTime = new Dictionary<float, AverageValue>();

            this.Log("\n-----------------------------------");
            string conditionStr;
            if (condition != null)
            {
                conditionStr = $"\n过滤条件: 使用{this.GetConditon(condition)}   去除{unUsedCondition} 数据量:{list.Count}";
            }
            else
            {
                conditionStr = $"筛选条件:无      去除{unUsedCondition}      数据量:{list.Count}";
            }
            this.Log(conditionStr);

            List<float> dashabi1 = new List<float>();
            Dictionary<string, int> shabiTimes = new Dictionary<string, int>();
            Dictionary<string, int> timeZoneTimes = new Dictionary<string, int>();

            foreach (var loginData in list)
            {
                foreach (var jsonData in loginData.datas)
                {
                    float progress = jsonData.body.param.progress;
                    float time = jsonData.body.param.Time;
                    if (time > 700)
                    {
                        continue;
                    }
                    if (progress == 3.1f)
                    {
                        string timezone = jsonData.body.device.timezone;
                        if (time > 5)
                        {
                            dashabi1.Add(time);
                            if (!shabiTimes.ContainsKey(timezone))
                            {
                                shabiTimes.Add(timezone, 1);
                            }
                            else
                            {
                                shabiTimes[timezone]++;
                            }
                        }
                        if (!timeZoneTimes.ContainsKey(timezone))
                        {
                            timeZoneTimes.Add(timezone, 1);
                        }
                        else
                        {
                            timeZoneTimes[timezone]++;
                        }
                    }
                    if (!dicPhaseTime.TryGetValue(progress, out AverageValue averageValue))
                    {
                        averageValue = new AverageValue();
                        dicPhaseTime.Add(progress, averageValue);
                    }
                    averageValue.Add(time);
                }
            }

            this.Log("-----------------------------------------------------");
            worksheetWriter.AddHeader(this.ExcelName + "_" + conditionStr);
            foreach (var kv in dicPhaseTime)
            {
                worksheetWriter.WriteRowData(kv.Key.ToString(), kv.Value.Average.ToString());
                this.Log($"平均耗时{kv.Key}:-----     {kv.Value.Average}");
            }
            AverageValue av = new AverageValue();
            dashabi1.Sort();
            foreach (var v in dashabi1)
            {
                av.Add(v);
            }
            this.Log("3.1超5秒的次数:" + dashabi1.Count + "    平均时间:" + av.Average);
            worksheetWriter.ChangeNextCol();
        }

        private void CalculateEnterSuccessPercent(WorksheetWriter worksheetWriter, float[] condition, float unUsedCondition = -1)
        {
            var list = this.GetLoginDatas(condition);

            this.Log("-----------------------------------------------------");
            string conditionStr;
            if (condition != null)
            {
                conditionStr = $"\n过滤条件: 使用{this.GetConditon(condition)}   去除{unUsedCondition} 数据量:{list.Count}";
            }
            else
            {
                conditionStr = $"筛选条件:无      去除{unUsedCondition}      数据量:{list.Count}";
            }
            this.Log(conditionStr);

            int mTotalLaunchTimes = 0;
            int mTotalLoginTimes = 0;

            foreach (var loginData in list)
            {
                mTotalLaunchTimes++;

                if (loginData.ContainesProgress(ProgressConst.LOADING_COMPLETE))
                {
                    mTotalLoginTimes++;
                }
            }

            this.Log($"登录成功率 launchTimes={mTotalLaunchTimes}  EnterGameTimes={mTotalLoginTimes}  Percent={(mTotalLoginTimes * 1.0f / mTotalLaunchTimes) * 100}%\n");
            worksheetWriter.Write(new string[] { this.ExcelName + "_" + conditionStr, mTotalLaunchTimes.ToString(), mTotalLoginTimes.ToString(), ((mTotalLoginTimes * 1.0f / mTotalLaunchTimes) * 100).ToString() });
        }

        private List<LoginData> GetLoginDatas(float[] usedConditions, float unUsedCondition = -1)
        {
            List<LoginData> list = new List<LoginData>();
            foreach (var kv in dicUserData)
            {
                var userData = kv.Value;
                foreach (var loginData in userData.loginDatas)
                {
                    if (unUsedCondition >= 0 && loginData.ContainesProgress(unUsedCondition))
                    {
                        continue;
                    }
                    if (usedConditions == null || loginData.ContainesProgress(usedConditions))
                    {
                        list.Add(loginData);
                    }
                }
            }
            return list;
        }


        virtual protected string GetCellValue(int row, int col)
        {
            return (dataTable.Rows[row][col]).ToString();
        }

        private StringBuilder sb = new StringBuilder();

        private void Log(string str)
        {
            Console.WriteLine(str);
            sb.Append(str);
            sb.Append("\n");
        }

        private string ExcelName => Path.GetFileNameWithoutExtension(this.mExcelFilePath);

        private void SaveLog()
        {
            string fileName = Path.GetFileNameWithoutExtension(this.mExcelFilePath);
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/Log" + fileName + ".txt", sb.ToString());

            this.ClearLog();
        }
        private void ClearLog()
        {
            sb = new StringBuilder();
        }
    }
}

public class AverageValue
{
    public float total;
    public int num;

    public void Add(float value)
    {
        this.num++;
        total += value;
    }

    public float Average => this.total / this.num;
}


public class LoginUserData
{
    public List<BIJsonData> list = new List<BIJsonData>();
    public string deviceID;
    public List<LoginData> loginDatas = new List<LoginData>();

    public LoginUserData(string _deviceID)
    {
        deviceID = _deviceID;
    }

    public void AddData(BIJsonData data)
    {
        list.Add(data);
    }
    public void Sort()
    {
        list.Sort(delegate (BIJsonData data1, BIJsonData data2)
        {
            if (data2.body.device.timestamp == data1.body.device.timestamp)
            {
                if (data1.body.param.progress == data2.body.param.progress)
                {
                    return 0;
                }
                return data1.body.param.progress > data2.body.param.progress ? 1 : -1;
            }

            return data1.body.device.timestamp > data2.body.device.timestamp ? 1 : -1;
        });

        //foreach (var data in list)
        //{
        //    this.Log(deviceID + "  " + data.body.device.timestamp + "    " + data.body.param.progress);
        //}
    }

    public void Analysis()
    {
        LoginData loginData = null;

        List<float> arr = new List<float>();
        for (int i = 0; i < list.Count; i++)
        {
            var data = list[i];
            float curProgress = data.body.param.progress;
            arr.Add(curProgress);

            if (loginData == null || curProgress == ProgressConst.BI_INIT)
            {
                if (curProgress >= ProgressConst.FSM_CHANGE_SCENE)
                {
                    continue;
                }
                loginData = new LoginData();
                loginData.AddStep(data);
                loginDatas.Add(loginData);
            }
            else
            {
                loginData.AddStep(data);
                if (curProgress == ProgressConst.LOADING_COMPLETE)
                {
                    loginData = null;
                }
            }
        }

        //this.launchTimes = loginDatas.Count;
        //foreach (var data in loginDatas)
        //{
        //    if (data.ContainesProgress(ProgressConst.LOADING_COMPLETE))
        //    {
        //        this.loginTimes++;
        //    }
        //}
    }
}

public class LoginData
{
    public List<BIJsonData> datas = new List<BIJsonData>();
    private HashSet<float> progressList = new HashSet<float>();

    public void AddStep(BIJsonData data)
    {
        datas.Add(data);
        progressList.Add(data.body.param.progress);
    }

    public bool ContainesProgress(float progress)
    {
        return progressList.Contains(progress);
    }

    public bool ContainesProgress(float[] list)
    {
        foreach (var progress in list)
        {
            if (!this.ContainesProgress(progress))
            {
                return false;
            }
        }
        return true;
    }

    public float GetBreakProgress()
    {
        float lastProgress = datas[datas.Count - 1].body.param.progress;
        if (lastProgress != ProgressConst.LOADING_COMPLETE)
        {
            return lastProgress;
        }
        return -1;
    }
}