using BIAnalysis.Script.BI;
using ExcelDataReader;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BIAnalysis.Script.Modal
{
    public class BaseAnalysisMode
    {
        protected Dictionary<string, string> dicParamNames = new Dictionary<string, string>();
        protected int PARAM_INDEX = 22;
        protected List<string> HeaderNames;
        protected DataTable dataTable;
        protected IMongoCollection<BsonDocument> collection;
        protected BsonDocument[] documentList;
        protected IMongoDatabase database;
        protected readonly int BLOCK_SIZE = 10000000;
        protected string Sort_Key = "event_time";//"report_time";

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

            StartLoadAllFiles(folderBrowser.FileNames);
        }

        virtual protected void StartLoadAllFiles(string[] fileNames)
        {
            ConnectMangoDB();
            foreach (var excelFilePath in fileNames)
            {
                InitDBCollection(excelFilePath);
                LoadExcel(excelFilePath); //load excel file
                WriteJsonToMangoDB(); //write json to mangoDB
            }
        }

        virtual protected void LoadExcel(string excelFilePath)
        {
            HeaderNames = new List<string>();

            //NPOI
            //XSSFWorkbook workbook = new XSSFWorkbook(new FileStream(excelFilePath, FileMode.Open));
            //OPCPackage pkg = OPCPackage.Open(new FileInfo(excelFilePath));
            //XSSFWorkbook wb = new XSSFWorkbook(pkg);

            //EPPlus
            //var package = new ExcelPackage(new FileInfo(excelFilePath));
            //ExcelWorksheet sheet = package.Workbook.Worksheets[1];

            //ExcelDataReader
            Console.WriteLine($"Excel is loading...  {Path.GetFileName(excelFilePath)}");
            dataTable = ResourceManager.LoadExcel(excelFilePath);//set header names

            Console.WriteLine("Excel Load Complete!!");
            int colCount = dataTable.Columns.Count;
            for (int col = 0; col < colCount; col++)
            {
                string name = GetCellValue(0, col);
                if (dataTable.Columns[col].ColumnName == "params" || name == "params")
                {
                    name = "params";
                    PARAM_INDEX = col;
                }
                HeaderNames.Add(name);
            }

            SortData();
        }

        //排序
        virtual protected void SortData()
        {
            int rowCount = dataTable.Rows.Count;
            int colCount = dataTable.Columns.Count;
            int row = 1;
            documentList = new BsonDocument[rowCount - 1];
            while (row < rowCount)
            {
                Console.WriteLine("添加数据到数组  " + DateTime.Now);
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

                for (int i = 0; i < list.Length; i++)
                {
                    var jsonObj = list[i];
                    SaveDocumentToList(JsonConvert.SerializeObject(jsonObj), i);
                }
                row = endIndex;
            }
        }

        virtual protected string GetCellValue(int row, int col)
        {
            return (dataTable.Rows[row][col]).ToString();
        }

        virtual protected string GetHeaderName(int col)
        {
            return HeaderNames[col];
        }
        virtual protected bool IsUsefullParam(int index)
        {
            string name = GetHeaderName(index);
            return !BIConst.UnusedParamNames.Contains(name);
        }
        virtual protected bool IsUsefullParam(string name)
        {
            return !BIConst.UnusedParamNames.Contains(name);
        }

        //统计所有params属性，只统计一次
        virtual protected void GetAllParamNames()
        {
            dicParamNames = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            int len = dataTable.Rows.Count;
            for (int i = 1; i < len; i++)
            {
                string _params = (string)dataTable.Rows[i][PARAM_INDEX];

                JObject jsonObj = JsonConvert.DeserializeObject(_params) as JObject;
                foreach (var item in jsonObj)
                {
                    if (!dicParamNames.ContainsKey(item.Key))
                    {
                        dicParamNames.Add(item.Key, "");
                    }
                }
            }
            foreach (var kv in dicParamNames)
            {
                sb.Append(kv.Key + "\n");
            }
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/BIParams.txt", sb.ToString());
        }

        virtual protected void ConnectMangoDB()
        {
            var client = new MongoClient("mongodb://192.168.1.94:27017");
            database = client.GetDatabase("bi");
        }
        virtual protected void InitDBCollection(string excelFilePath)
        {
            string excelName = Path.GetFileNameWithoutExtension(excelFilePath);
            collection = database.GetCollection<BsonDocument>($"BICollections_{excelName}");
        }

        virtual protected void SaveDocumentToList(string json, int index)
        {
            MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
            //collection.InsertOne(document);//同步
            documentList[index] = document;
        }
        virtual protected void WriteJsonToMangoDB()
        {
            collection.InsertMany(documentList);
        }
        virtual protected void WriteJsonToMangoDB(string json)
        {
            MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
            collection.InsertOne(document);//同步
        }

        virtual protected void SortByTime(JObject[] list)
        {
            //MergeSort(list, 0, list.Length - 1, new JObject[list.Length]);
            HeapSort(list); //堆排内存开销小，速度都差不多，平均(O)nlogn
        }

        virtual protected bool Compare(JObject a, JObject b)
        {
            DateTime d1 = Convert.ToDateTime(a[Sort_Key].ToString());
            DateTime d2 = Convert.ToDateTime(b[Sort_Key].ToString());
            return d1 > d2;
        }

        #region
        protected void MergeSort(JObject[] arr, int left, int right, JObject[] temp)
        {
            if (right - left == 1)
            {
                Merge(arr, left, right, right, temp);
            }
            else if (left == right)
            {
                temp[right] = arr[right];
            }
            else if (left < right)
            {
                int mid = (left + right) / 2;
                MergeSort(arr, left, mid, temp);
                MergeSort(arr, mid + 1, right, temp);
                Merge(arr, left, mid, right, temp);
            }
        }
        //数组合并，降序排列
        protected void Merge(JObject[] arr, int left, int mid, int right, JObject[] temp)
        {
            //1,5,3,4,2 //2
            //1,5,3 //L=1,5 Merge=5,1// R=3 Merge=3
            //Merge=
            int L = left, R = right;
            int index = L;
            while (L < mid && R <= right)
            {
                if (Compare(arr[L], arr[R]))
                {
                    temp[index] = arr[L];
                    L++;
                }
                else
                {
                    temp[index] = arr[R];
                    R++;
                }
                ++index;
            }
            //将左边剩余元素填充进temp中
            while (L <= left)
            {
                temp[index] = arr[L];
                L++;
                ++index;
            }
            //将右边剩余元素填充进temp中
            while (R <= right)
            {
                temp[index] = arr[R];
                R++;
                ++index;
            }
        }
        #endregion Merge Sort

        #region
        /**
        * 堆排序
        * @param array 待排序数组
        * @return 已排序数组
        */
        protected JObject[] HeapSort(JObject[] array)
        {
            //这里元素的索引是从0开始的,所以最后一个非叶子结点array.length/2 - 1
            for (int i = array.Length / 2 - 1; i >= 0; i--)
            {
                AdjustHeap(array, i, array.Length);  //调整堆
            }

            // 上述逻辑，建堆结束
            // 下面，开始排序逻辑
            for (int j = array.Length - 1; j > 0; j--)
            {
                // 元素交换,作用是去掉大顶堆
                // 把大顶堆的根元素，放到数组的最后；换句话说，就是每一次的堆调整之后，都会有一个元素到达自己的最终位置
                Swap(array, 0, j);
                // 元素交换之后，毫无疑问，最后一个元素无需再考虑排序问题了。
                // 接下来我们需要排序的，就是已经去掉了部分元素的堆了，这也是为什么此方法放在循环里的原因
                // 而这里，实质上是自上而下，自左向右进行调整的
                AdjustHeap(array, 0, j);
            }
            return array;
        }

        /**
        * 整个堆排序最关键的地方
        * @param array 待组堆
        * @param i 起始结点
        * @param length 堆的长度
        */
        protected void AdjustHeap(JObject[] array, int i, int length)
        {
            // 先把当前元素取出来，因为当前元素可能要一直移动
            JObject temp = array[i];
            for (int k = 2 * i + 1; k < length; k = 2 * k + 1)
            {  //2*i+1为左子树i的左子树(因为i是从0开始的),2*k+1为k的左子树
               // 让k先指向子节点中最大的节点
                if (k + 1 < length && !Compare(array[k], array[k + 1]))
                {  //如果有右子树,并且右子树大于左子树
                    k++;
                }
                //如果发现结点(左右子结点)大于根结点，则进行值的交换
                if (Compare(array[k], temp))
                {
                    Swap(array, i, k);
                    // 如果子节点更换了，那么，以子节点为根的子树会受到影响,所以，循环对子节点所在的树继续进行判断
                    i = k;
                }
                else
                {  //不用交换，直接终止循环
                    break;
                }
            }
        }

        protected void Swap(JObject[] arr, int i, int j)
        {
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        #endregion Heap Sort HeapSort
    }
}
