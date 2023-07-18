using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;
using BIAnalysis.Script.BI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BIAnalysis.Script
{
    class ResourceManager
    {
        static public DataTable LoadExcel(string strFilePath)
        {
            //NPOI
            //XSSFWorkbook workbook = new XSSFWorkbook(new FileStream(excelFilePath, FileMode.Open));
            //OPCPackage pkg = OPCPackage.Open(new FileInfo(excelFilePath));
            //XSSFWorkbook wb = new XSSFWorkbook(pkg);

            //EPPlus
            //var package = new ExcelPackage(new FileInfo(excelFilePath));
            //ExcelWorksheet sheet = package.Workbook.Worksheets[1];

            //ExcelDataReader.
            if (Path.GetExtension(strFilePath) == ".csv")
            {
                return ConvertCSVtoDataTable(strFilePath);
            }
            else
            {
                using (FileStream stream = File.Open(strFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                    {
                        DataSet dataSet = excelReader.AsDataSet();
                        Console.WriteLine("Excel Load Complete!!");
                        return dataSet.Tables[0];
                    }
                }
            }
        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();

            DataRow dr = dt.NewRow();
            int i = 0;
            foreach (string header in BIConst.CSVHeaders)
            {
                dt.Columns.Add(header);
                dr[i] = header;
                i++;
            }

            Dictionary<string, string> dicLogin = new Dictionary<string, string>();
            dt.Rows.Add(dr);
            using (StreamReader sr = new StreamReader(strFilePath))
            {
                while (!sr.EndOfStream)
                {
                    string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    dr = dt.NewRow();
                    string evtName = string.Empty;
                    string userID = "";
                    for (i = 0; i < BIConst.CSVHeaders.Length; i++)
                    {
                        if(dt.Columns[i].ColumnName == "user_id")
                        {
                            userID = rows[i];
                        }
                        if(dt.Columns[i].ColumnName == "event_name")
                        {
                            evtName = rows[i];
                        }
                        if (dt.Columns[i].ColumnName == "params")
                        {
                            string _params = rows[i];
                            if(_params.Length > 2)
                            {
                                _params = _params.Replace("\"\"", "\"");
                                _params = _params.Substring(1, _params.Length - 2);
                            }
                            dr[i] = _params;
                        }
                        else
                        {
                            dr[i] = rows[i];
                        }
                    }
                    //if (evtName == "Goods" || evtName == "mergeProp" || evtName == "LevelStatus" || evtName == "LevelResult"
                    //    || evtName == "Version_Updating" || evtName == "Guide_Succeed" || evtName == "Guide_Succeed"
                    //    || evtName == "LevelAimsBeforeAddStep" || evtName == "LevelAimOnEnterLevel" || evtName == "LevelGoods")
                    //{
                    //    continue;
                    //}
                    if(evtName == "Login")
                    {
                        if(!dicLogin.ContainsKey(userID))
                        {
                            dicLogin.Add(userID, userID);
                        }
                    }
                    dt.Rows.Add(dr);
                }
            }
            Console.WriteLine($"日活：{dicLogin.Count}");
            dicLogin.Clear();
            return dt;
        }
    }
}
