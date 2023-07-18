using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIAnalysis.Script.BI
{
    public class BIConst
    {
        public static HashSet<string> UnusedParamNames = new HashSet<string>
        {
            "params",
            "user_id_b",
            //"player_id",
            "app_version",
            //"country_id",
            "account_id",
            "log_time",
            "remark",
            "device_country",
            "event_id_custom",
            "role_id",
            "server_id",
        };

        //表头
        public static readonly string[] CSVHeaders = new string[] { "user_id", "user_id_b", "player_id", "app_id", "app_version",
            "os_id", "os_version", "os_language", "device_mode", "device_type", "country_id", "event_name", "event_time",
            "datastate", "params", "date_time"};
    }
}
