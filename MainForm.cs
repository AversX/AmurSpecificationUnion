using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace SpecificationPack
{
    public partial class MainForm : Form
    {
        private Excel.Application excel;        

        struct Unit
        {
            private string pos;
            private string code;
            private string name;
            private string manufacture;
            private CupBoard[] cupBoard;
            private string measure;
            private Color errorColor;

            private double count;

            public string Code { get => code; set => code = value; }
            public string Name { get => name; set => name = value; }
            public string Manufacture { get => manufacture; set => manufacture = value; }
            public CupBoard[] CupBoard { get => cupBoard; set => cupBoard = value; }
            public string Measure { get => measure; set => measure = value; }
            public Color ErrorColor { get => errorColor; set => errorColor = value; }
            public double Count { get => count; set => count = value; }
            public string Pos { get => pos; set => pos = value; }
        }

        public struct CupBoard
        {
            private double num;
            private string fileName;

            public string FileName { get => fileName; set => fileName = value; }
            public double Num { get => num; set => num = value; }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void AddSpecBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "(*.xlsx); (*.xls)|*.xlsx; *.xls";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string fileName in ofd.FileNames)
                    specListBox.Items.Add(fileName);
            }
        }

        private void SpecListBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Effect == DragDropEffects.Move)
            {
                string[] objects = (string[])e.Data.GetData(DataFormats.FileDrop);
                for (int i = 0; i < objects.Length; i++)
                {
                    specListBox.Items.Add(objects[i]);
                }
            }
        }

        private void specListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && ((e.AllowedEffect & DragDropEffects.Move) == DragDropEffects.Move))
                e.Effect = DragDropEffects.Move;
        }

        private void clearSpecBtn_Click(object sender, EventArgs e)
        {
            specListBox.Items.Clear();
        }

        private void deleteSpecBtn_Click(object sender, EventArgs e)
        {
            if (specListBox.SelectedIndex >= 0)
                specListBox.Items.RemoveAt(specListBox.SelectedIndex);
        }

        private void formBtn_Click(object sender, EventArgs e)
        {
            List<Unit> Units = new List<Unit>();
            for (int i = 0; i < specListBox.Items.Count; i++)
            {
                Units.AddRange(LoadDataSpec(specListBox.Items[i].ToString(), i));
            }
            Units = consolidate(Units);
            UploadData(Units);
        }

        private List<Unit> consolidate(List<Unit> units)
        {
            for (int i = 0; i < units.Count; i++)
                for (int j = i + 1; j < units.Count; j++)
                    if (units[i].Code != "")
                    {
                        if (units[i].Code == units[j].Code)
                        {
                            if (units[j].Measure.Replace(".", "") == units[i].Measure.Replace(".", ""))
                            {
                                Unit unit = units[i];
                                for (int k = 0; k < unit.CupBoard.Length; k++)
                                {
                                    unit.CupBoard[k].Num += units[j].CupBoard[k].Num;
                                    if (units[j].CupBoard[k].FileName != null || unit.CupBoard[k].FileName == null)
                                        unit.CupBoard[k].FileName = units[j].CupBoard[k].FileName;
                                }
                                units.RemoveAt(j);
                                j--;
                                unit.ErrorColor = Color.Empty;
                                units[i] = unit;
                            }
                            else
                            {
                                Unit unit = units[j];
                                unit.ErrorColor = Color.Yellow;
                                units[j] = unit;
                            }
                        }
                        else if (units[i].Name == units[j].Name)
                        {
                            Unit unit = units[j];
                            unit.ErrorColor = Color.Magenta;
                            units[j] = unit;
                        }

                    }
                    else if (units[i].Name == units[j].Name)
                    {
                        Unit unit = units[i];
                        for (int k = 0; k < unit.CupBoard.Length; k++)
                        {
                            unit.CupBoard[k].Num += units[j].CupBoard[k].Num;
                            if (units[j].CupBoard[k].FileName != null || unit.CupBoard[k].FileName == null)
                                unit.CupBoard[k].FileName = units[j].CupBoard[k].FileName;
                        }
                        units.RemoveAt(j);
                        j--;
                        unit.ErrorColor = Color.Empty;
                        units[i] = unit;
                    }
            return units;
        }

        private List<Unit> LoadDataSpec(string path, int index)
        {
            List<Unit> units = new List<Unit>();
            DataSet dataSet = new DataSet("EXCEL");
            string connectionString;
            connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0;HDR=YES'";
            OleDbConnection connection = new OleDbConnection(connectionString);
            connection.Open();

            System.Data.DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            string sheet1 = (string)schemaTable.Rows[0].ItemArray[2];

            string select = String.Format("SELECT * FROM [{0}]", sheet1);
            OleDbDataAdapter adapter = new OleDbDataAdapter(select, connection); 
            adapter.Fill(dataSet);
            connection.Close();

            for (int row = 0; row < dataSet.Tables[0].Rows.Count; row++)
            {
                if (dataSet.Tables[0].Rows[row][3].ToString().Length > 0)
                {
                    Unit unit = new Unit();
                    unit.Pos = dataSet.Tables[0].Rows[row][1].ToString().Trim();
                    unit.Code = dataSet.Tables[0].Rows[row][2].ToString().Trim();
                    unit.Name = dataSet.Tables[0].Rows[row][3].ToString().Trim();

                    CupBoard[] cB = new CupBoard[specListBox.Items.Count];
                    for (int i = 0; i < cB.Length; i++)
                    {
                        if (i == index)
                        {
                            cB[index].Num = double.Parse(dataSet.Tables[0].Rows[row][4].ToString().Trim());
                        }
                        else cB[i].Num = 0;
                        cB[i].FileName = Path.GetFileNameWithoutExtension(specListBox.Items[i].ToString());
                    }
                    unit.CupBoard = cB;
                    unit.Measure = dataSet.Tables[0].Rows[row][5].ToString().Trim();
                    unit.Manufacture = dataSet.Tables[0].Rows[row][6].ToString().Trim();
                    units.Add(unit);
                }
            }
            return units;
        }

        private void UploadData(List<Unit> units)
        {
            excel = new Excel.Application();
            excel.SheetsInNewWorkbook = 1;
            excel.Workbooks.Add(Type.Missing);
            Excel.Worksheet sheet = (Excel.Worksheet)excel.Sheets.get_Item(1);
            Excel.Range autoFit;

            sheet.Cells[1, 1] = "Код";
            sheet.Columns[1].NumberFormat = "@";

            sheet.Cells[1, 2] = "Наименование";
            sheet.Columns[2].NumberFormat = "@";

            sheet.Cells[1, 3] = "Завод изготовитель";
            sheet.Columns[3].NumberFormat = "@";

            sheet.Cells[1, 4] = "Ед. изм.";
            sheet.Columns[4].NumberFormat = "@";

            int curColumn = 5;
            int curMaxColumn = curColumn - 1;
            for (int i = 0; i < units.Count; i++)
            {
                sheet.Cells[i + 2, 1] = units[i].Code;
                sheet.Cells[i + 2, 2] = units[i].Name;
                sheet.Cells[i + 2, 3] = units[i].Manufacture;
                sheet.Cells[i + 2, 4] = units[i].Measure;

                for (int j = 0; j < units[i].CupBoard.Length; j++)
                {
                    sheet.Cells[i + 2, curColumn + j] = units[i].CupBoard[j].Num;
                    if (curColumn + j > curMaxColumn)
                    {
                        curMaxColumn++;
                        sheet.Cells[1, curMaxColumn].NumberFormat = "#";
                        sheet.Cells[1, curMaxColumn] = units[i].CupBoard[j].FileName;
                    }
                }
            }

            sheet.Cells[1, curMaxColumn + 1] = "Сумма по шкафам";
            for (int i = 0; i < units.Count; i++)
            {
                Excel.Range c1 = (Excel.Range)sheet.Cells[i + 2, 5];
                Excel.Range c2 = (Excel.Range)sheet.Cells[i + 2, curMaxColumn];
                Excel.Range r = (Excel.Range)sheet.Range[c1, c2];
                ((Excel.Range)sheet.Cells[i + 2, curMaxColumn + 1]).FormulaLocal = "=СУММ(" + r.Address.ToString() + ")";
                autoFit = (Excel.Range)sheet.Cells[i + 2, curMaxColumn + 1];
                double d = autoFit.Value2;
                if (d - Math.Truncate(d) != 0)
                {
                    autoFit = (Excel.Range)sheet.Cells[i + 2, curMaxColumn + 1];
                    autoFit.NumberFormat = "#,#0.0";
                }
                autoFit = (Excel.Range)sheet.Rows[i + 2];
                if (units[i].ErrorColor != Color.Empty) autoFit.EntireRow.Interior.Color = units[i].ErrorColor;
                autoFit.EntireRow.AutoFit();
            }
            excel.Visible = true;
        }
    }
}
