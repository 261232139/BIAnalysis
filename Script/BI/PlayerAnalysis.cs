using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIAnalysis.Script.BI
{
    class PlayerAnalysis
    {
        public string mPlayerID;
        private Action<string, string> LaunchCallBack; //启动
        private Action<string, string, int> ReloginCallBack; //登录失败，重登
        private Action<string, string> LoginSuccessCallBack; //登录成功

        public PlayerAnalysis(string playerID, Action<string, string>  LaunchCallBack, Action<string, string, int>  ReloginCallBack, Action<string, string>  LoginSuccessCallBack)
        {
            this.LaunchCallBack = LaunchCallBack;
            this.ReloginCallBack = ReloginCallBack;
            this.LoginSuccessCallBack = LoginSuccessCallBack;
            this.mPlayerID = playerID;
        }
        public void StartAnalysis(Queue<JObject> jsonQueue)
        {
            int retryTimes = 0;
            while (jsonQueue.Count > 0)
            {
                var json = jsonQueue.Dequeue();
                if (IsLaunch(json))
                {
                    retryTimes = 0;
                    LaunchCallBack.Invoke(this.mPlayerID, this.GetCountry(json));
                }
                if (IsLoginSuccess(json))
                {
                    string country = this.GetCountry(json);
                    
                    if (retryTimes > 0)
                    {
                        ReloginCallBack(this.mPlayerID, country, retryTimes);
                    }
                    else
                    {
                        LoginSuccessCallBack.Invoke(this.mPlayerID, country);
                    }
                    retryTimes++;
                }
            }
        }

        private bool IsLaunch(JObject json)
        {
            if (json["event_name"].ToString() != "LoadProgress")
            {
                return false;
            }
            return json["type"].ToString() == "1";
        }

        private bool IsLoginSuccess(JObject json)
        {
            return json["event_name"].ToString() == "Login";
        }

        private string GetCountry(JObject json)
        {
            return json["country_id"].ToString();
        }
    }
}

