using OfficeOpenXml;
using System;
using System.Collections.Generic;

namespace BIAnalysis.Script.Data
{
    public class WorksheetWriter
    {
        public ExcelWorksheet worksheet;
        public int totalCol;
        public int curRow = 1;
        public int curCol = 1;
        private Dictionary<string, int> dicRow = new Dictionary<string, int>();

        public WorksheetWriter(ExcelWorksheet _worksheet, string[] colHeaders = null, string[] rowHeaders = null)
        {
            worksheet = _worksheet;

            if (colHeaders != null)
            {
                this.AddHeaders(colHeaders);
            }
            if (rowHeaders != null)
            {
                this.AddRowHeaders(rowHeaders);
            }
        }

        public void AddHeaders(string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                this.AddHeader(headers[i]);
            }
        }
        public void AddHeader(string header)
        {
            this.totalCol++;
            worksheet.Cells[1, this.totalCol].Value = header;
            this.curRow = 2;
        }

        public void AddRowHeaders(string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[i + 1, 1].Value = headers[i];
                dicRow.Add(headers[i], i + 1);
            }
            this.totalCol = 1;
            this.ChangeNextCol();
        }

        public void ChangeNextCol()
        {
            this.curCol++;
            this.curRow = 2;
        }

        public void Write(string content)
        {
            worksheet.Cells[this.curRow, this.curCol].Value = content;
            this.curRow++;
        }

        public void WriteRowData(string key, string value)
        {
            if (!this.dicRow.TryGetValue(key, out int row))
            {
                row = this.curRow++;
                this.dicRow.Add(key, row);
                worksheet.Cells[row, 1].Value = key;
            }
            worksheet.Cells[row, this.curCol].Value = value;
        }
        public void Write(string[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                worksheet.Cells[this.curRow, this.curCol + i].Value = list[i];
            }
            this.curRow++;
        }
    }
}
