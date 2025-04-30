using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace FuzzyNetworkAnalysis2
{
    public partial class Form1 : Form
    {
        List<FuzzyWork> works = new List<FuzzyWork>();
        List<FuzzyCustomer> customers = new List<FuzzyCustomer>();
        bool valueChange1 = false;
        bool valueChange2 = false;
        public Form1()
        {
            InitializeComponent();
        }
        #region ввод данных
        async private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.OpenOrCreate))
                    {
                        works = (await JsonSerializer.DeserializeAsync<FuzzyWork[]>(fs)).ToList();
                    }
                    valueChange1 = true;
                    dataGridView1.Rows.Clear();
                    dataGridView1.Rows.Add(works.Count);
                    for (int i = 0; i < works.Count; i++)
                    {
                        dataGridView1.Rows[i].Cells[0].Value = works[i]?.Start?.ToString() ?? "";
                        dataGridView1.Rows[i].Cells[1].Value = works[i]?.End?.ToString() ?? "";
                        dataGridView1.Rows[i].Cells[2].Value = works[i]?.R?.ToString() ?? "";
                        dataGridView1.Rows[i].Cells[3].Value = works[i]?.r?.ToString() ?? "";
                    }
                    valueChange1 = false;
                }
                catch { }
            }
        }
        async private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync<FuzzyWork[]>(fs, works.ToArray());
                }
            }
        }
        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!valueChange1)
                works.Add(new FuzzyWork());
        }
        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if(!valueChange1 && works.Count != 0)
            {
                works.RemoveAt(e.RowIndex);
            }
        }
        private void dataGridView2_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if(!valueChange2)
                customers.Add(new FuzzyCustomer());
        }
        private void dataGridView2_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if(!valueChange2 && customers.Count != 0)
                customers.RemoveAt(e.RowIndex);
        }
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (valueChange1 || e.RowIndex == -1)
                return;
            string temp = dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString();
            valueChange1 = true;

            switch (e.ColumnIndex)
            {
                case 0:
                    if (int.TryParse(temp, out int start))
                    {
                        works[e.RowIndex].Start = start;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.Start?.ToString() ?? "";
                    }
                    break;
                case 1:
                    if (int.TryParse(temp, out int end))
                    {
                        works[e.RowIndex].End = end;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.End?.ToString() ?? "";
                    }
                    break;
                case 2:
                    if (double.TryParse(temp, out double R))
                    {
                        works[e.RowIndex].R = R;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.R?.ToString() ?? "";
                    }
                    break;
                case 3:
                    if (double.TryParse(temp, out double r))
                    {
                        works[e.RowIndex].r = r;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.r?.ToString() ?? "";
                    }
                    break;
            }
            valueChange1 = false;
        }
        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (valueChange2 || e.RowIndex == -1)
                return;
            string temp = dataGridView2[e.ColumnIndex, e.RowIndex].Value.ToString();
            valueChange2 = true;

            switch (e.ColumnIndex)
            {
                case 0:
                    if (int.TryParse(temp, out int id))
                    {
                        customers[e.RowIndex].id = id;
                    }
                    else
                    {
                        dataGridView2[e.ColumnIndex, e.RowIndex].Value = customers[e.RowIndex].id?.ToString() ?? "";
                    }
                    break;
                case 1:
                    if (double.TryParse(temp, out double d))
                    {
                        customers[e.RowIndex].d = d;
                    }
                    else
                    {
                        dataGridView2[e.ColumnIndex, e.RowIndex].Value = customers[e.RowIndex].d?.ToString() ?? "";
                    }
                    break;
                case 2:
                    if (double.TryParse(temp, out double D))
                    {
                        customers[e.RowIndex].D = D;
                    }
                    else
                    {
                        dataGridView2[e.ColumnIndex, e.RowIndex].Value = customers[e.RowIndex].D?.ToString() ?? "";
                    }
                    break;
            }
            valueChange2 = false;
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            if (works.Count == 0)
                return;
            bool failed = false;
            foreach (var work in works)
                failed = failed || work == null || work.Start == null || work.End == null || work.r == null || work.R == null;
            if (failed)
            {
                label1.Text = "Вычислить время выполнения проекта\n\nВ таблице не должно быть пустых ячеек";
                return;
            }
            label1.Text = "Вычислить время выполнения проекта";

            List<(int, double)> earlyDeadlineR = new List<(int, double)>();
            List<List<int>> CriticalTrack = new List<List<int>>();
            earlyDeadlineR.Add((works.Min(w => w.Start) ?? 0, 0));
            int iterator = 0;
            while (iterator < works.Count)
            {
                for(int i= 0;i < works.Count; i++)
                {
                    if (works[i].Start == earlyDeadlineR[iterator].Item1)
                    {
                        int index = earlyDeadlineR.FindIndex(d => works[i].End == d.Item1);
                        if (index == -1)
                        {
                            earlyDeadlineR.Add((works[i].End ?? 0, 0));
                            index = earlyDeadlineR.Count - 1;
                            CriticalTrack.Add(new List<int>());
                        }
                        if (earlyDeadlineR[iterator].Item2 + works[i].R > earlyDeadlineR[index].Item2)
                        {
                            earlyDeadlineR[index] = (earlyDeadlineR[index].Item1, earlyDeadlineR[iterator].Item2 + works[i].R ?? 0);
                            CriticalTrack[index] = CriticalTrack[iterator].Union(new List<int>() { works[i].End ?? 0 }).ToList();
                        }
                    }
                }
                iterator++;
            }

            List<(int, double)> earlyDeadliner = new List<(int, double)>();
            earlyDeadliner.Add((works.Min(w => w.Start) ?? 0, 0));
            iterator = 0;
            while (iterator < works.Count)
            {
                for (int i = 0; i < works.Count; i++)
                {
                    if (works[i].Start == earlyDeadliner[iterator].Item1)
                    {
                        int index = earlyDeadliner.FindIndex(d => works[i].End == d.Item1);
                        if (index == -1)
                        {
                            earlyDeadliner.Add((works[i].End ?? 0, 0));
                            index = earlyDeadliner.Count - 1;
                        }
                        if (earlyDeadliner[iterator].Item2 + works[i].r > earlyDeadliner[index].Item2)
                        {
                            earlyDeadliner[index] = (earlyDeadliner[index].Item1, earlyDeadliner[iterator].Item2 + works[i].r ?? 0);
                        }
                    }
                }
                iterator++;
            }
        }
    }
}
