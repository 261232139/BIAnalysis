using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BIAnalysis.Script.BI
{
    class BIAnalysisDataManager
    {
        public static int Player_Count = 0; //总玩家数量
        public static Dictionary<string, AnalysisCountryData> dicCountryLoginData = new Dictionary<string, AnalysisCountryData>(); //所有国家登录信息
        public static Dictionary<string, AnalysisPlayerData> dicPlayerLoginData = new Dictionary<string, AnalysisPlayerData>(); //登录失败玩家信息
        public static Dictionary<string, List<AnalysisErrorData>> dicErrorData = new Dictionary<string, List<AnalysisErrorData>>();
        public static int RECORD_NUM = 6;

        public static void StartAnalysisLogin(Dictionary<string, Queue<JObject>> playerDataQueue)
        {
            Console.WriteLine("开始分析数据" + DateTime.Now);
            Console.WriteLine($"玩家数量{playerDataQueue.Count}");
            while (playerDataQueue.Count > 0)
            {
                var playerID = playerDataQueue.First().Key;
                var jsonQueue = playerDataQueue.First().Value;

                PlayerAnalysis analysis = new PlayerAnalysis(playerID, LaunchCallBack, ReloginCallBack, LoginSuccessCallBack);
                analysis.StartAnalysis(jsonQueue);

                playerDataQueue.Remove(playerID);
            }

            foreach (var kv in dicCountryLoginData)
            {
                //游戏启动人数
                //登录成功人数
                //游戏启动次数
                //登录成功次数
            }

            Console.WriteLine("数据分析结束，结果请查询数据库" + DateTime.Now);
        }
        public static void StartAnalysisErrorMsg(Dictionary<string, Queue<JObject>> playerDataQueue)
        {
            Console.WriteLine("开始分析ErrorMsg数据" + DateTime.Now);
            Console.WriteLine($"玩家数量{playerDataQueue.Count}");
            while (playerDataQueue.Count > 0)
            {
                string playerID = playerDataQueue.First().Key;
                Queue<JObject> jsonQueue = playerDataQueue.First().Value;

                AnalysisError(playerID, jsonQueue);

                playerDataQueue.Remove(playerID);
            }
            Console.WriteLine("出现error次数" + dicErrorData.Count);
            Console.WriteLine("数据分析结束" + DateTime.Now);
        }
        static private void AnalysisError(string playerID, Queue<JObject> jsonQueue)
        {
            Queue<JObject> preQueue = new Queue<JObject>();
            int afterNum = -1;
            AnalysisErrorData data = null;
            string errorName = string.Empty;
            string msg = string.Empty;
            while (jsonQueue.Count > 0)
            {
                JObject json = jsonQueue.Dequeue();
                if (afterNum >= 0)
                {
                    afterNum++;
                    data.afterBI.Enqueue(new ErrorSubData(json));
                    if (afterNum == RECORD_NUM)
                    {
                        afterNum = -1;
                        preQueue.Clear();
                        AddErrorMsg(errorName, data);
                    }
                    continue;
                }
                if (json["event_name"].ToString() == "ErrorMsg")
                {
                    afterNum = 0;
                    msg = json["msg"].ToString();
                    errorName = msg.Length > 30 ? msg.Substring(0, 30) : msg;
                    data = new AnalysisErrorData(playerID, msg);
                    int num = 0;
                    while (preQueue.Count > 0 && num < RECORD_NUM)
                    {
                        data.preBI.Enqueue(new ErrorSubData(preQueue.Dequeue()));
                        num++;
                    }
                }
                else
                {
                    preQueue.Enqueue(json);
                }
            }
            if (data != null)
            {
                preQueue.Clear();
                AddErrorMsg(errorName, data);
            }
        }

        static private void AddErrorMsg(string errorName, AnalysisErrorData data)
        {
            if(!dicErrorData.TryGetValue(errorName, out List<AnalysisErrorData> list))
            {
                list = new List<AnalysisErrorData>();
                dicErrorData.Add(errorName, list);
            }
            list.Add(data);
        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="country"></param>
        private static void LaunchCallBack(string playerID, string country)
        {
            if (!dicCountryLoginData.TryGetValue(country, out AnalysisCountryData countryData))
            {
                countryData = new AnalysisCountryData();
                dicCountryLoginData.Add(country, countryData);
            }
            if (countryData.dicLaunchPlayers.TryGetValue(playerID, out int times))
            {
                countryData.dicLaunchPlayers[playerID] = ++times;
            }
            else
            {
                countryData.dicLaunchPlayers.Add(playerID, 1);
            }
            countryData.launchTimes++;

            if (!dicPlayerLoginData.TryGetValue(playerID, out AnalysisPlayerData playerData))
            {
                playerData = new AnalysisPlayerData();
                dicPlayerLoginData.Add(playerID, playerData);
            }
            playerData.launchTimes++;
        }
        /// <summary>
        /// 登录成功
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="country"></param>
        private static void LoginSuccessCallBack(string playerID, string country)
        {
            if (!dicCountryLoginData.TryGetValue(country, out AnalysisCountryData countryData))
            {
                countryData = new AnalysisCountryData();
                dicCountryLoginData.Add(country, countryData);
            }
            if (countryData.dicLoginPlayers.TryGetValue(playerID, out int times))
            {
                countryData.dicLoginPlayers[playerID] = ++times;
            }
            else
            {
                countryData.dicLoginPlayers.Add(playerID, 1);
            }
            countryData.loginSuccessTimes++;

            if (!dicPlayerLoginData.TryGetValue(playerID, out AnalysisPlayerData playerData))
            {
                playerData = new AnalysisPlayerData();
                dicPlayerLoginData.Add(playerID, playerData);
            }
            playerData.loginSuccessTimes++;
        }
        /// <summary>
        /// 登录失败，重登
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="country"></param>
        /// <param name="retryTimes"></param>
        private static void ReloginCallBack(string playerID, string country, int retryTimes)
        {
            if (!dicCountryLoginData.TryGetValue(country, out AnalysisCountryData countryData))
            {
                countryData = new AnalysisCountryData();
                dicCountryLoginData.Add(country, countryData);
            }
            if (countryData.dicReloginPlayers.TryGetValue(playerID, out int times))
            {
                countryData.dicReloginPlayers[playerID] = ++times;
            }
            else
            {
                countryData.dicReloginPlayers.Add(playerID, 1);
            }
            countryData.reloginTimes++;

            if (!dicPlayerLoginData.TryGetValue(playerID, out AnalysisPlayerData playerData))
            {
                playerData = new AnalysisPlayerData();
                dicPlayerLoginData.Add(playerID, playerData);
            }
            playerData.reloginTimes++;
        }
    }

    public class BaseAnalysisData
    {
        public int launchTimes;
        public int loginSuccessTimes;
        public int reloginTimes;
    }
    public class AnalysisCountryData : BaseAnalysisData
    {
        public Dictionary<string, int> dicLaunchPlayers = new Dictionary<string, int>();
        public Dictionary<string, int> dicLoginPlayers = new Dictionary<string, int>();
        public Dictionary<string, int> dicReloginPlayers = new Dictionary<string, int>();
    }

    public class AnalysisPlayerData : BaseAnalysisData
    {

    }

    public class AnalysisErrorData
    {
        public string playerID;
        public string msg;
        public Queue<ErrorSubData> preBI = new Queue<ErrorSubData>();
        public Queue<ErrorSubData> afterBI = new Queue<ErrorSubData>();

        public AnalysisErrorData(string playerID, string msg)
        {
            this.playerID = playerID;
            this.msg = msg;
        }
    }

    public class ErrorSubData
    {
        public string eventName = "";
        public JObject json;

        public ErrorSubData(JObject json)
        {
            this.json = json;
            this.eventName = json["event_name"].ToString();
        }
    }
}