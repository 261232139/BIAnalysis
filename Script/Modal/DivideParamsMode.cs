using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIAnalysis.Script.Modal
{
    public class DivideParamsMode : BaseAnalysisMode
    {
        private static DivideParamsMode _instance;
        public static DivideParamsMode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DivideParamsMode();
                }
                return _instance;
            }
        }

        public DivideParamsMode()
        {
            Sort_Key = "report_time";
        }
    }
}
