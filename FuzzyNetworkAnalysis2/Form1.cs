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
        double R_ = -10, r_ = -10;
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
                        dataGridView1.Rows[i].Cells[2].Value = works[i]?.r_small?.ToString() ?? "";
                        dataGridView1.Rows[i].Cells[3].Value = works[i]?.r_big?.ToString() ?? "";
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
                    if (double.TryParse(temp, out double r))
                    {
                        works[e.RowIndex].r_small = r;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.r_small?.ToString() ?? "";
                    }
                    break;
                case 3:
                    if (double.TryParse(temp, out double R))
                    {
                        works[e.RowIndex].r_big = R;
                    }
                    else
                    {
                        dataGridView1[e.ColumnIndex, e.RowIndex].Value = works[e.RowIndex]?.r_big?.ToString() ?? "";
                    }
                    break;
            }
            valueChange1 = false;
        }
        #endregion
        private void button1_Click(object sender, EventArgs e)
        {
            if (works.Count == 0)
                return;
            bool failed = false;
            foreach (var work in works)
                failed = failed || work == null || work.Start == null || work.End == null || work.r_small == null || work.r_big == null;
            if (failed)
            {
                label1.Text = "Вычислить время\nвыполнения проекта\n\nВ таблице не должно быть пустых ячеек";
                return;
            }
            label1.Text = "Вычислить время\nвыполнения проекта";
            #region критический путь R
            List<(int, double)> earlyDeadlineR = new List<(int, double)>();
            List<List<int>> CriticalTrack = new List<List<int>>() { new List<int>() { works.Min(w => w.Start) ?? 0 } };
            earlyDeadlineR.Add((works.Min(w => w.Start) ?? 0, 0));
            int iterator = 0;
            while (iterator < earlyDeadlineR.Count)
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
                        if (earlyDeadlineR[iterator].Item2 + works[i].r_big > earlyDeadlineR[index].Item2)
                        {
                            earlyDeadlineR[index] = (earlyDeadlineR[index].Item1, earlyDeadlineR[iterator].Item2 + works[i].r_big ?? 0);
                            CriticalTrack[index] = CriticalTrack[iterator].Union(new List<int>() { works[i].End ?? 0 }).ToList();
                        }
                    }
                }
                iterator++;
            }
            R_ = earlyDeadlineR.Find(d => d.Item1 == earlyDeadlineR.Max(d2 => d2.Item1)).Item2;
            #endregion
            #region критический путь r
            List<(int, double)> earlyDeadliner = new List<(int, double)>();
            earlyDeadliner.Add((works.Min(w => w.Start) ?? 0, 0));
            iterator = 0;
            while (iterator < earlyDeadliner.Count)
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
                        if (earlyDeadliner[iterator].Item2 + works[i].r_small > earlyDeadliner[index].Item2)
                        {
                            earlyDeadliner[index] = (earlyDeadliner[index].Item1, earlyDeadliner[iterator].Item2 + works[i].r_small ?? 0);
                        }
                    }
                }
                iterator++;
            }
            r_ = earlyDeadliner.Find(d => d.Item1 == earlyDeadliner.Max(d2 => d2.Item1)).Item2;
            #endregion
            #region оценка совместимости
            chart1.Series[1].Points.Clear();
            if (double.TryParse(textBox1.Text, out double d_big) && double.TryParse(textBox2.Text, out double d_small))
            {
                if (0 <= d_big && d_big <= d_small)
                {
                    chart1.Series[1].Points.AddXY(0, 1);
                    chart1.Series[1].Points.AddXY(d_big, 1);
                    chart1.Series[1].Points.AddXY(d_small, 0);
                    chart1.Series[1].Points.AddXY(d_small + 5, 0);
                    double ri = earlyDeadliner.Last().Item2, Ri = earlyDeadlineR.Last().Item2;
                    double copaibabilitycompatibility;
                    if (d_small < r_)
                        copaibabilitycompatibility = 0;
                    else if (d_big > R_)
                        copaibabilitycompatibility = 100;
                    else
                        copaibabilitycompatibility = 100*((d_small * (Ri - ri) - ri * (d_big - d_small)) / (Ri - ri + d_small - d_big) - ri) / (Ri - ri);
                    if (double.TryParse(textBox3.Text, out double neededCopaibabilitycompatibility))
                    {
                        label5.Text = $"Совместимость проекта {copaibabilitycompatibility.ToString("0.##")}%" +
                            ((neededCopaibabilitycompatibility <= copaibabilitycompatibility) ? "\nЮлиана одобряет проект" : "\nnЮлиана такое не одобрит");
                    }
                    else
                        label5.Text = $"Совместимость проекта {copaibabilitycompatibility.ToString("0.##")}%" +
                            $"\nНеобходимая совместимость не задана";
                }
                else
                    label5.Text = "Параметры заказчика введены не правильно";
            }
            else
                label5.Text = "Параметры заказчика не введены";
            #endregion
            #region plot
            chart1.Series[0].Points.Clear();
            if (r_ != -10)
            {
                chart1.Series[0].Points.AddXY(0, 0);
                chart1.Series[0].Points.AddXY(r_, 0);
                chart1.Series[0].Points.AddXY(R_, 1);
                chart1.Series[0].Points.AddXY(R_ + 5, 1);
            }
            #endregion
        }
    }
}
