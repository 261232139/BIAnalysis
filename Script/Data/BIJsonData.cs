using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIAnalysis.Script.Data
{
    public class BIJsonData
    {
        public string date;
        public string remote_addr;
        public string hostname;
        public string appid;
        public string user_agent;
        public JsonBody body;
    }

    public class JsonBody
    {
        public string name;
        public string action;
        public string id;
        public JsonDevice device;
        public JsonParams param;
    }

    public class JsonParams
    {
        public string NetworkReachability;
        public string AllianceID;
        public string DeviceId;
        public string PayStage;
        public string userID;
        public string GameVersion;
        public int Coin;
        public int CacheLife;
        public int is_return;
        public int Star;
        public int UserStage;
        public string state;
        public string CurTaskID;
        public int GameLevel;
        public int TicketNew;
        public int PlayingSecondTicks;
        public int Ticket;
        public string CurAssetVersion;
        public int TimerTicket;
        public int Clover;
        public int UID;
        public float updateSize;
        public int Life;
        public float progress;
        public string time;
        public string playerid;
        public bool DebugUser;

        public float Time
        {
            get
            {
                if(float.TryParse(time, out float a))
                {
                    return a;
                }
                return 0;
            }
        }
    }

    public class JsonDevice
    {
        public string syslanguage;
        public string timezone;
        public string appver;
        public string sysver;
        public string targetSdkVersion;
        public string userid;
        public string unityver;
        public string appvercode;
        public string devicemodel;
        public int timestamp;
    }

}
