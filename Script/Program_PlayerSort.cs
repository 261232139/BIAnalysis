using BIAnalysis.Script.BI;
using ExcelDataReader;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using NPOI.OpenXml4Net.OPC;
//using NPOI.SS.UserModel;
//using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BIAnalysis
{
    class Program_PlayerSort
    {
        static private List<string> paramsNameList;
        static private Dictionary<string, string> dicParamNames = new Dictionary<string, string>();
        static private Dictionary<int, string> dicUnusedParamNames = new Dictionary<int, string>();
        static private int PARAM_INDEX = 22;
        static private List<string> HeaderNames;
        static private DataTable dataTable;
        static private IMongoCollection<BsonDocument> collection;
        static private IMongoDatabase database;

        [STAThread]
        static void Main1(string[] args)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog
            {
                // Set validate names and check file exists to false otherwise windows will
                // not let you select "Folder Selection."
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                Multiselect = true,
                Filter = "Excel Files(.xlsx)|*.xlsx| Excel Files(.xls)|*.xls| Excel Files(*.xlsm)|*.xlsm",
                // Always default to Folder Selection.
                FileName = "Folder Selection."
            };
            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Console.WriteLine($"Program start running!" + DateTime.Now);
            ConnectMangoDB();
            string[] fileNames = folderBrowser.FileNames;
            foreach (var excelFilePath in fileNames)
            {
                RefreshCollection(excelFilePath);
                LoadExcel(excelFilePath); //load excel file
            }
            Console.WriteLine($"Complete!" + DateTime.Now);
            Console.ReadKey();
        }

        static private void LoadExcel(string excelFilePath)
        {
            paramsNameList = new List<string>();
            Console.WriteLine($"Excel is loading...  {Path.GetFileName(excelFilePath)}");

            //NPOI
            //XSSFWorkbook workbook = new XSSFWorkbook(new FileStream(excelFilePath, FileMode.Open));
            //OPCPackage pkg = OPCPackage.Open(new FileInfo(excelFilePath));
            //XSSFWorkbook wb = new XSSFWorkbook(pkg);

            //EPPlus
            //var package = new ExcelPackage(new FileInfo(excelFilePath));
            //ExcelWorksheet sheet = package.Workbook.Worksheets[1];

            //ExcelDataReader
            using (FileStream stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {
                    HeaderNames = new List<string>();

                    var dataSet = excelReader.AsDataSet();
                    dataTable = dataSet.Tables[0];
                    //string _params = (string)dataTable.Rows[1][22];
                    //JObject jsonObj = JsonConvert.DeserializeObject(_params) as JObject;
                    //GetAllParamNames();

                    //set header names
                    dicUnusedParamNames = new Dictionary<int, string>();
                    int colCount = dataTable.Columns.Count;
                    for (int col = 0; col < colCount; col++)
                    {
                        string name = GetCellValue(0, col);
                        if (name == "params")
                        {
                            PARAM_INDEX = col;
                        }
                        HeaderNames.Add(name);
                    }

                    //write json to mangoDB
                    WriteData();
                }
            }
        }

        static private void WriteData()
        {
            Console.WriteLine("统计玩家数据");
            Dictionary<string, List<JObject>> dicPlayerData = new Dictionary<string, List<JObject>>();

            int rowCount = dataTable.Rows.Count;
            int colCount = dataTable.Columns.Count;
            int blockSize = 50000;
            int row = 1;
            while (row < rowCount)
            {
                int endIndex = row + blockSize > rowCount ? rowCount : row + blockSize;
                for (; row < endIndex; row++)
                {
                    string _params = (string)dataTable.Rows[row][PARAM_INDEX];
                    JObject jsonObj = JsonConvert.DeserializeObject(_params) as JObject;
                    for (int col = 0; col < colCount; col++)
                    {
                        string name = GetHeaderName(col);
                        if (IsUsefullParam(name) && jsonObj[name] == null)
                        {
                            jsonObj[name] = GetCellValue(row, col);
                        }
                    }

                    //add data to list
                    JToken token = jsonObj["userID"];
                    string userID = token != null ? token.ToString() : string.Empty;
                    if (!dicPlayerData.TryGetValue(userID, out List<JObject> list))
                    {
                        list = new List<JObject>();
                        dicPlayerData.Add(userID, list);
                    }
                    list.Add(jsonObj);
                }
                row = endIndex;
            }

            Console.WriteLine("开始排序  " + DateTime.Now);
            foreach (var kv in dicPlayerData)
            {
                SortByTime(kv.Value);
                Console.WriteLine("排序结束  " + kv.Key);

                Console.WriteLine("开始写入  " + kv.Key);
                List<BsonDocument> documentList = new List<BsonDocument>();
                foreach (JObject jsonObject in kv.Value)
                {
                    string json = JsonConvert.SerializeObject(jsonObject);
                    MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                    //collection.InsertOne(document);//同步
                    documentList.Add(document);
                }
                //一起存DB
                WriteJsonToMangoDB(documentList);
                documentList.Clear();
                Console.WriteLine("写入结束  " + kv.Key);
            }
        }

        static private void SortByTime(List<JObject> list)
        {
            JObject[] temp = new JObject[list.Count];
            Sort(list, 0, list.Count - 1, temp);
        }
        static private void Sort(List<JObject> arr, int left, int right, JObject[] temp)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;
                Sort(arr, left, mid, temp);
                Sort(arr, right, mid, temp);
                merge(arr, left, mid, right, temp);
            }
        }

        //数组合并，降序排列
        private static void merge(List<JObject> arr, int left, int mid, int right, JObject[] temp)
        {
            int i = left;
            int j = mid + 1;
            int t = 0;
            while (i < mid && j <= right)
            {
                //降序排列
                if (Compare(arr[i], arr[j]))
                {
                    temp[t++] = arr[i++];
                }
                else
                {
                    temp[t++] = arr[j++];
                }
            }
            while (i <= mid)
            {
                //将左边剩余元素填充进temp中
                temp[t++] = arr[i++];
            }
            while (j <= right)
            {
                //将右序列剩余元素填充进temp中
                temp[t++] = arr[j++];
            }
            t = 0;
            //将temp中的元素全部拷贝到原数组中
            while (left <= right)
            {
                arr[left++] = temp[t++];
            }
        }
        static private bool Compare(JObject a, JObject b)
        {
            DateTime d1 = Convert.ToDateTime(a["report_time"].ToString());
            DateTime d2 = Convert.ToDateTime(b["report_time"].ToString());
            return d1 > d2;
        }

        static private string GetCellValue(int row, int col)
        {
            return (dataTable.Rows[row][col]).ToString();
        }

        static private string GetHeaderName(int col)
        {
            return HeaderNames[col];
        }
        static private bool IsUsefullParam(int index)
        {
            string name = GetHeaderName(index);
            return !BIConst.UnusedParamNames.Contains(name);
        }
        static private bool IsUsefullParam(string name)
        {
            return !BIConst.UnusedParamNames.Contains(name);
        }

        //统计所有params属性，只统计一次
        static private void GetAllParamNames()
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

        static private void ConnectMangoDB()
        {
            var client = new MongoClient("mongodb://192.168.1.94:27017");
            database = client.GetDatabase("bi");
        }
        static private void RefreshCollection(string excelFilePath)
        {
            string excelName = Path.GetFileNameWithoutExtension(excelFilePath);
            collection = database.GetCollection<BsonDocument>($"BICollections_{excelName}");
        }

        static private void WriteJsonToMangoDB(List<BsonDocument> documentList)
        {
            var arr = documentList.ToArray();
            collection.InsertMany(arr);
        }
        static private void WriteJsonToMangoDB(string json)
        {
            MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
            collection.InsertOne(document);//同步
        }












        int[] quickSort(int[] arr, int left, int right)
        {
            int len = arr.Length;
            int partitionIndex;
            if (left < right)
            {
                partitionIndex = partition(arr, left, right);
                quickSort(arr, left, partitionIndex - 1);
                quickSort(arr, partitionIndex + 1, right);
            }
            return arr;
        }

        int partition(int[] arr, int left, int right)
        {     // 分区操作
            int pivot = left;                      // 设定基准值（pivot）
            int index = pivot + 1;
            for (var i = index; i <= right; i++)
            {
                if (arr[i] < arr[pivot])
                {
                    swap(arr, i, index);
                    index++;
                }
            }
            swap(arr, pivot, index - 1);
            return index - 1;
        }

        void swap(int[] arr, int i, int j)
        {
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        int partition2(int[] arr, int low, int high)
        {
            int pivot = arr[low];
            while (low < high)
            {
                while (low < high && arr[high] > pivot)
                {
                    --high;
                }
                arr[low] = arr[high];
                while (low < high && arr[low] <= pivot)
                {
                    ++low;
                }
                arr[high] = arr[low];
            }
            arr[low] = pivot;
            return low;
        }

        int[] quickSort2(int[] arr, int low, int high)
        {
            if (low < high)
            {
                int pivot = partition2(arr, low, high);
                quickSort2(arr, low, pivot - 1);
                quickSort2(arr, pivot + 1, high);
            }
            return arr;
        }











        /**
        * 选择排序-堆排序
        * @param array 待排序数组
        * @return 已排序数组
        */
        public static JObject[] heapSort(JObject[] array)
        {
            //这里元素的索引是从0开始的,所以最后一个非叶子结点array.length/2 - 1
            for (int i = array.Length / 2 - 1; i >= 0; i--)
            {
                adjustHeap(array, i, array.Length);  //调整堆
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
                adjustHeap(array, 0, j);
            }
            return array;
        }

        /**
        * 整个堆排序最关键的地方
        * @param array 待组堆
        * @param i 起始结点
        * @param length 堆的长度
        */
        public static void adjustHeap(JObject[] array, int i, int length)
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

        static private void Swap(JObject[] arr, int i, int j)
        {
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
    }
}
