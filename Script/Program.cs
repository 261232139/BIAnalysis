using BIAnalysis.Script.Modal;
using System;

namespace BIAnalysis
{
    enum EmSelectMode
    {
        DIVIDE_PARAMS,
        ANALYSIS_LOGIN,
        GET_DOWNLOAD_URL,
        ANALYSIS_ERROR,
    }

    class Program
    {
        static private EmSelectMode Mode = EmSelectMode.DIVIDE_PARAMS;

        [STAThread]
        static void Main(string[] args)
        {
            var now = DateTime.Now;
            DateTime time = now.AddSeconds(-86400 * 2);
            string year = time.Year.ToString();
            string month = time.Month < 10 ? "0" + time.Month : time.Month.ToString();
            string day = time.Day < 10 ? "0" + time.Day : time.Day.ToString();
            string date = $"{year}-{month}-{day}";
            GetUrlMode.RequestBIData(date);
            if (!string.IsNullOrEmpty(date))
            {
                return;
            }

            Console.WriteLine("请选择:\n");
            Console.WriteLine("******************************1.拆分param并存到数据库******************************\n");
            Console.WriteLine("******************************2.分析登录联网数据***********************************\n");
            Console.WriteLine("******************************3.获取某天BI下载地址*********************************\n");
            Console.WriteLine("******************************4.分析ErrorMsg前后关系*********************************\n");

            bool isSelect = false;
            ConsoleKey response;
            do
            {
                response = Console.ReadKey(false).Key;
                if (response == ConsoleKey.D1)
                {
                    Mode = EmSelectMode.DIVIDE_PARAMS;
                    isSelect = true;
                }
                else if (response == ConsoleKey.D2)
                {
                    Mode = EmSelectMode.ANALYSIS_LOGIN;
                    isSelect = true;
                }
                else if (response == ConsoleKey.D3)
                {
                    Mode = EmSelectMode.GET_DOWNLOAD_URL;
                    isSelect = true;
                }
                else if (response == ConsoleKey.D4)
                {
                    Mode = EmSelectMode.ANALYSIS_ERROR;
                    isSelect = true;
                }
            }
            while (!isSelect);
            Start();
        }

        static private void Start()
        {
            Console.WriteLine($"Program start running!" + DateTime.Now);
            if (Mode == EmSelectMode.DIVIDE_PARAMS)
            {
                DivideParamsMode.Instance.Start();
            }
            else if (Mode == EmSelectMode.ANALYSIS_LOGIN)
            {
                LoginAnalysisModule.Instance.Start();
                //AnalysisLoginMode.Instance.Start();
            }
            else if (Mode == EmSelectMode.GET_DOWNLOAD_URL)
            {
                GetUrlMode.RequestBIData();
            }
            else if (Mode == EmSelectMode.ANALYSIS_ERROR)
            {
                AnalysisErrorMode.Instance.Start();
            }
            else
            {
                throw new Exception("模式选择错误");
            }
            Console.WriteLine($"Complete!" + DateTime.Now);
            Console.ReadKey();
        }
    }
}