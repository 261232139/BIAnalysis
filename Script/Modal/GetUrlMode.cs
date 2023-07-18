using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;

namespace BIAnalysis.Script.Modal
{
    public static class GetUrlMode
    {
        static private Timer timer = new System.Timers.Timer();
        static private int mTimeCount = 0;

        static public void RequestBIData(string dateTime = null)
        {
            //input datetime
            Console.WriteLine("\n请输入日期：例:  2021-10-25");
            string appID = "100112";
            string strURL = "https://crawler.nuclearport.com/user_event";
            if(string.IsNullOrEmpty(dateTime))
            {
                do
                {
                    dateTime = Console.ReadLine();
                }
                while (string.IsNullOrEmpty(dateTime));
            }

            if (!IsInputInvalid(dateTime))
            {
                Console.WriteLine("日期格式非法，重新开始输入");
                RequestBIData();
                return;
            }

            //Initialize http request data.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "POST";
            request.ContentType = "application/json";

            string orgToken = appID + dateTime + "/user_eventuserevent";
            Console.WriteLine($"token origin value=  {orgToken}");

            //calculate token
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(orgToken));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            string token = sb.ToString();
            sb.Clear();
            Console.WriteLine($"Start Request url=  {strURL}");
            Console.WriteLine($"token=  {token}");

            //Start Request
            HttpRequestData requestData = new HttpRequestData
            {
                app_id = appID,
                auth_token = token,
                date_time = dateTime,
                request_type = "userevent",
            };

            string json = JsonConvert.SerializeObject(requestData);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(buffer, 0, buffer.Length);
                Console.WriteLine("请求中");
                StartTimer();

                //接收相应
                HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
                Stream respStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(respStream, Encoding.UTF8);
                string result = reader.ReadToEnd();
                reader.Close();
                resp.Close();
                resp.Dispose();

                HttpResponseData responseData = JsonConvert.DeserializeObject<HttpResponseData>(result);
                if (responseData.code != 200)
                {
                    throw new Exception(responseData.code.ToString(), new Exception(responseData.msg));
                }
                else
                {
                    System.Diagnostics.Process.Start(responseData.data);
                    Console.WriteLine($"获取成功\n{responseData.data}");
                }
                StopTimer();
            }
        }

        static private bool IsInputInvalid(string dateTime)
        {
            //2021-10-25
            string[] arr = dateTime.Split('-');
            if (arr.Length != 3)
            {
                return false;
            }
            foreach (var str in arr)
            {
                if (!int.TryParse(str, out int result))
                {
                    if (result <= 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        static private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            mTimeCount += 1;
            if (mTimeCount >= 30)
            {
                Console.WriteLine("请求超时，估计是正在生成数据，建议等会儿再请求");
                StopTimer();
            }
        }

        static private void StartTimer()
        {
            //设置timer可用
            timer.Enabled = true;

            //设置timer
            timer.Interval = 1000;

            //设置是否重复计时，如果该属性设为False,则只执行OnTimer方法一次。
            timer.AutoReset = true;

            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
        }

        static private void StopTimer()
        {
            timer.Enabled = false;
            mTimeCount = 0;
        }
    }

    struct HttpRequestData
    {
        public string app_id;
        public string auth_token;
        public string date_time;
        public string request_type;
    }

    struct HttpResponseData
    {
        public int code;
        public string msg;
        public string state;
        public string data;
    }
}
