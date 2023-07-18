using BIAnalysis.Script.BI;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BIAnalysis.Script.Modal
{
    public class AnalysisErrorMode : BaseAnalysisMode
    {
        private Dictionary<string, Queue<JObject>> dicPlayerData;

        private static AnalysisErrorMode _instance;
        public static AnalysisErrorMode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnalysisErrorMode();
                }
                return _instance;
            }
        }

        public AnalysisErrorMode()
        {
            Sort_Key = "event_time";
        }

        override protected void InitDBCollection(string excelFilePath)
        {
            string excelName = Path.GetFileNameWithoutExtension(excelFilePath);
            collection = database.GetCollection<BsonDocument>($"ErrorAnalysis_{excelName}");
        }

        override protected void LoadExcel(string excelFilePath)
        {
            base.LoadExcel(excelFilePath);

            BIAnalysisDataManager.StartAnalysisErrorMsg(dicPlayerData);
        }

        override protected void WriteJsonToMangoDB()
        {
            
        }

        //排序
        override protected void SortData()
        {
            dicPlayerData = new Dictionary<string, Queue<JObject>>();

            int rowCount = dataTable.Rows.Count;
            int colCount = dataTable.Columns.Count;
            int row = 1;
            while (row < rowCount)
            {
                int endIndex = row + BLOCK_SIZE > rowCount ? rowCount : row + BLOCK_SIZE;
                JObject[] list = new JObject[endIndex - row];
                int num = 0;
                for (; row < endIndex; row++)
                {
                    string _params = (string)dataTable.Rows[row][PARAM_INDEX];
                    JObject jsonObj = JsonConvert.DeserializeObject(_params) as JObject;
                    for (int col = 0; col < colCount; col++)
                    {
                        //"\"{\"\"messageName\"\":\"\"SyncPlayData\"\",\"\"Ticket\"\":\"\"3\"\",\"\"NetworkReachability\"\":\"\"2\"\",\"\"CurAssetVersion\"\":\"\"53\"\",\"\"userID\"\":\"\"16d6ef10b06b4fd880e97256a926905b\"\",\"\"GameVersion\"\":\"\"1.5.0\"\",\"\"UID\"\":\"\"82036\"\",\"\"Coin\"\":\"\"2812\"\",\"\"Star\"\":\"\"0\"\",\"\"Ping\"\":\"\"307\"\",\"\"Life\"\":\"\"5\"\",\"\"useTime\"\":\"\"333\"\",\"\"CurTaskID\"\":\"\"10035,\"\",\"\"GameLevel\"\":\"\"44\"\",\"\"playerid\"\":\"\"82036-16d6ef10b06b4fd880e97256a926905b\"\",\"\"DebugUser\"\":\"\"False\"\"}\""
                        //"{\"Ticket\":\"0\",\"placementId\":\"1219\",\"NetworkReachability\":\"2\",\"CurAssetVersion\":\"24\",\"userID\":\"553464d761ec4f65b7b2b9fb15b6e069\",\"scene\":\"2\",\"RewardTimes\":\"0\",\"GameVersion\":\"1.4.2\",\"UID\":\"78463\",\"Coin\":\"5\",\"Star\":\"1\",\"Times\":\"0\",\"Ping\":\"1434\",\"Life\":\"5\",\"step\":\"0\",\"CurTaskID\":\"10053,\",\"levelAdAddErrorNum\":\"0\",\"GameLevel\":\"72\",\"playerid\":\"78463-553464d761ec4f65b7b2b9fb15b6e069\",\"DebugUser\":\"False\"}"
                        string name = GetHeaderName(col);
                        if (IsUsefullParam(name) && jsonObj[name] == null)
                        {
                            jsonObj[name] = GetCellValue(row, col);
                        }
                    }
                    list[num] = jsonObj;
                    ++num;
                }
                Console.WriteLine("开始排序  " + DateTime.Now);
                SortByTime(list);
                Console.WriteLine("排序完成  " + DateTime.Now);

                Console.WriteLine("开始数据按玩家划分  " + DateTime.Now);
                for (int i = 0; i < list.Length; i++)
                {
                    var jsonObj = list[i];

                    //Add To PlayerDictionary
                    string userID = jsonObj["user_id"].ToString();
                    if (!dicPlayerData.TryGetValue(userID, out Queue<JObject> jsonQueue))
                    {
                        jsonQueue = new Queue<JObject>();
                        dicPlayerData.Add(userID, jsonQueue);
                    }
                    jsonQueue.Enqueue(jsonObj);
                    //string json = JsonConvert.SerializeObject(jsonObj);
                    //SaveDocumentToList(json, i);
                    list[i] = null;
                }
                Console.WriteLine("数据划分完成  " + DateTime.Now);
                row = endIndex;
            }
        }

        override protected bool Compare(JObject a, JObject b)
        {
            DateTime d1 = Convert.ToDateTime(a[Sort_Key].ToString());
            DateTime d2 = Convert.ToDateTime(b[Sort_Key].ToString());
            if (d1 == d2 && a["type"] != null && b["type"] != null)
            {
                if (a["event_name"] == b["event_name"] && a["event_name"].ToString() == "LoadProgress")
                {
                    return int.Parse(a["type"].ToString()) > int.Parse(b["type"].ToString());
                }
            }
            return d1 > d2;
        }
    }
}
