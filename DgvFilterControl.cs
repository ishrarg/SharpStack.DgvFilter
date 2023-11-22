using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq.Dynamic;

using System.Drawing;


using System.CodeDom;
using Timer = System.Windows.Forms.Timer;

namespace SharpStack.Controls
{
    public partial class DgvFilterControl : System.Windows.Forms.Panel
    {
        #region Constructors
        public DgvFilterControl()
        {
            InitializeComponent();
            PositionSelf();
            tmrFilter.Interval = 500;
            tmrFilter.Tick += TmrFilter_Tick;

        }

        public DgvFilterControl(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            tmrFilter.Interval = 500;
            tmrFilter.Tick += TmrFilter_Tick;

        }


        #endregion

        #region Types Definition
        class FilterType
        {
            public string Column { get; set; }
            public string Value { get; set; }
            public string Filter { get; set; }
        }
        #endregion

        #region Private Variables 
        object OriginalDatasource = null;
        object OriginalBindingDataSource = null;
        bool DatasourceChangeByFilterControl = false;
        Timer tmrFilter = new Timer();
        Timer tmr = new Timer();
        Control ActiveControl = null;

        #endregion

        #region Public Properties
        DataGridView l_Dgv = null;
        public DataGridView GridView
        {
            get { return l_Dgv; }
            set
            {
                if (value.Dock != DockStyle.None)
                {
                    MessageBox.Show("Docked Datagridview is not supported. Please set None in Dock Property.");
                    return;
                }


                l_Dgv = value;

                l_Dgv.DataSourceChanged += L_Dgv_DataSourceChanged;
                l_Dgv.ColumnStateChanged += L_Dgv_ColumnStateChanged;
                l_Dgv.KeyDown += L_Dgv_KeyDown;
                l_Dgv.ColumnDisplayIndexChanged += L_Dgv_ColumnDisplayIndexChanged;

                l_Dgv.Resize += L_Dgv_Resize;
                l_Dgv.Scroll += L_Dgv_Scroll;
                PositionSelf();
            }
        }

        private List<string> mExcludedColumns = new List<string>();
        public List<string> ExcludedColumns
        {
            get { return mExcludedColumns; }
            set { mExcludedColumns = value; }
        }

        #endregion

        #region Private Events 

        #region Timer Events
        private void TmrFilter_Tick(object sender, EventArgs e)
        {
            tmrFilter.Stop();
            FilterGrid();
        }
        private void Tmr_Tick(object sender, System.EventArgs e)
        {
            RePositionFilterControls();
        }
        #endregion

        #region Textbox Events

        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                string columnName = ((TextBox)sender).Tag.ToString();
                if (l_Dgv.DataSource != null && l_Dgv.Rows.Count > 0)
                {
                    l_Dgv.Rows[0].Cells[columnName].Selected = true;
                    l_Dgv.CurrentCell = l_Dgv.Rows[0].Cells[columnName];
                    l_Dgv.Focus();
                }

            }
        }

        private void Txt_Enter(object sender, EventArgs e)
        {
            ActiveControl = (TextBox)sender;
            ((TextBox)sender).SelectAll();

        }

        private void Txt_MouseHover(object sender, EventArgs e)
        {
            try
            {
                TextBox txt = sender as TextBox;
                if (txt.Text.Length == 0)
                {
                    txt.CreateGraphics().DrawString(l_Dgv.Columns[txt.Tag.ToString()].HeaderText, txt.Font, Brushes.Gray, 0, 0);
                }
            }
            catch
            {

            }
        }

      

        private void Txt_TextChanged(object sender, EventArgs e)
        {
            tmrFilter.Stop();
            tmrFilter.Start();
        }

        #endregion



        #region Dgv Events
        private void L_Dgv_DataSourceChanged(object sender, EventArgs e)
        {
            if (l_Dgv != null && !DatasourceChangeByFilterControl)
            {
                OriginalDatasource = l_Dgv.DataSource;
                foreach (DataGridViewColumn col in l_Dgv.Columns)
                {
                    if (col.Visible)
                    {

                        if (this.Controls.Find(ControlName(col.Name), true).Length == 0)
                        {
                            if (mExcludedColumns.Contains(col.Name))
                            {
                                continue;
                            }
                            TextBox txt = new TextBox();
                            txt.Tag = col.Name;
                            txt.Name = ControlName(col.Name);
                            txt.Visible = false;
                            txt.TextChanged += Txt_TextChanged;
                            
                            txt.MouseHover += Txt_MouseHover;
                            txt.KeyDown += Txt_KeyDown;
                            txt.Enter += Txt_Enter;

                            this.Controls.Add(txt);

                        }
                    }
                }
                RePositionFilterControls();
            }
            DatasourceChangeByFilterControl = false;

        }
        private void L_Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                if (l_Dgv.CurrentCell != null && l_Dgv.CurrentCell.RowIndex == 0)
                {
                    string controlName = ControlName(l_Dgv.CurrentCell.OwningColumn.Name);

                    var control = this.Controls[controlName];
                    if (control != null)
                    {
                        control.Focus();
                    }
                }
            }
        }

        private void L_Dgv_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            tmr.Start();


        }

        private void L_Dgv_ColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
        {
            if (e.StateChanged == DataGridViewElementStates.Visible)
            {
                var control = FindControl(e.Column.Name);
                if (control != null)
                    control.Visible = e.Column.Visible;

            }
        }

        private void L_Dgv_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                RePositionFilterControls();
                tmr.Stop();
                tmr.Start();
            }


        }

        private void L_Dgv_Resize(object sender, EventArgs e)
        {
            PositionSelf();
            RePositionFilterControls();
        }
        #endregion

        #endregion

        #region Private Methods
        private void PositionSelf()
        {
            try
            {
                this.Height = this.Font.Height + 15;
                this.Left = l_Dgv.Left;
                if (l_Dgv != null)
                {
                    this.Width = l_Dgv.Width;
                    this.Top = l_Dgv.Top - this.Height - 5;

                    if (this.Top < 0)
                    {
                        this.Top = 5;
                        l_Dgv.Top = this.Top + this.Height + 5;
                        l_Dgv.Height -= l_Dgv.Top;
                    }
                }
            }
            catch
            {


            }
        }
        private void FilterGrid()
        {
            if (l_Dgv != null)
            {

                List<FilterType> filters = new List<FilterType>();
                TypeInfo tx = l_Dgv.DataSource.GetType().GetTypeInfo();
                string Filter = "";
                foreach (DataGridViewColumn column in l_Dgv.Columns)
                {
                    if (column.Visible)
                    {
                        TextBox txt = FindControl(column.Name);

                        if (txt != null && (txt.Text.Trim().Length > 0))
                        {
                            string filter = " Convert([" + column.Name + "], System.String) like '%" + txt.Text + "%'";
                            FilterType f = new FilterType();
                            f.Filter = filter;
                            f.Column = column.Name;
                            f.Value = txt.Text;
                            filters.Add(f);
                        }

                    }
                }
                Filter = "1=1 ";
                foreach (var item in filters)
                {
                    Filter += " and " + item.Filter;

                }
                if (OriginalDatasource is DataTable)
                {
                    DataTable tbl = l_Dgv.DataSource as DataTable;
                    tbl.DefaultView.RowFilter = Filter;
                }
                else if (OriginalDatasource is BindingSource)
                {
                    BindingSource bs = OriginalDatasource as BindingSource;
                    if (OriginalBindingDataSource == null)
                    {
                        OriginalBindingDataSource = bs.DataSource;
                    }

                    if (OriginalBindingDataSource is DataTable)
                    {
                        bs.Filter = Filter;
                        try
                        {
                            l_Dgv.AutoGenerateColumns = false;
                            bs.DataSource = bs.List.AsQueryable().Where(Filter);
                        }
                        catch (Exception ex)
                        {

                            MessageBox.Show(ex.Message);
                        }
                    }

                 

                }
                
                
            }
        }

        private void AssignDataSource(object l)
        {
            DatasourceChangeByFilterControl = true;

            int HorizontalScroll = l_Dgv.HorizontalScrollingOffset;
            l_Dgv.DataSource = l;
            l_Dgv.HorizontalScrollingOffset = HorizontalScroll;
            RePositionFilterControls();
            if (ActiveControl != null)
            {
                ActiveControl.Focus();
            }
        }

        public void RePositionFilterControls()
        {
            tmr.Stop();
            if (l_Dgv != null)
            {

                foreach (DataGridViewColumn col in l_Dgv.Columns)
                {
                    try
                    {
                        string colName = l_Dgv.Name + "_" + col.Name;
                        TextBox txt = FindControl(col.Name);

                        if (txt != null)
                        {
                            txt.TabIndex = col.DisplayIndex + 1;
                            if (col.Displayed && col.Visible)
                            {
                                txt.Visible = true;
                                txt.Top = 2;
                                var rect = l_Dgv.GetColumnDisplayRectangle(col.Index, false);
                                txt.Left = rect.Left;
                                txt.Width = rect.Width;

                            }
                            else if (!col.Visible)
                            {
                                this.Controls.Remove(txt);
                            }
                            else
                            {
                                //If user is already typing on this textbox, it will lose the focus on the textbox.
                                if (ActiveControl != null)
                                {
                                    if (txt != ActiveControl)
                                    {
                                        txt.Visible = false;
                                    }
                                }
                                else
                                    txt.Visible = false;
                            }
                        }
                    }
                    catch
                    {


                    }
                }
                if (ActiveControl != null)
                    ActiveControl.Focus();
            }

        }
        //public IList<T> searchingList;
        //private IList<T> ApplyFilterOnList<T>(List<FilterType> filters)
        //{
        //    //List<T> searchingList = abc;
        //    foreach (var item in filters)
        //    {
        //        try
        //        {

        //            if (l_Dgv.Columns[item.Column].Visible)
        //            {
        //                Type valueType = l_Dgv.Columns[item.Column].ValueType;
        //                bool IsNullableType = valueType.Name.Contains("Nullable");
        //                string SearchValue = item.Value.ToLower();
        //                if (IsNullableType)
        //                {
        //                    valueType = valueType.GenericTypeArguments[0];
        //                }
        //                if (valueType == typeof(string))
        //                {

        //                    searchingList = searchingList.Where(item.Column + " !=null && " + item.Column + ".ToLower().Contains(@0)", SearchValue);
        //                }
        //                else
        //                {
        //                    if (IsNullableType)
        //                    {

        //                        searchingList = searchingList.Where(item.Column + " !=null && " + item.Column + ".Value.ToString().ToLower().Contains(@0)", SearchValue);
        //                    }
        //                    else
        //                    {
        //                        searchingList = searchingList.Where(item.Column + " != null && " + item.Column + ".ToString().ToLower().Contains(@0)", SearchValue);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception exp)
        //        {

        //        }
        //    }
        //    return searchingList;
        //}

        private List<T> ApplyFilterOnList<T>(List<T> searchingList, List<FilterType> filters)
        {
            foreach (var item in filters)
            {
                try
                {

                    if (l_Dgv.Columns[item.Column].Visible)
                    {
                        Type valueType = l_Dgv.Columns[item.Column].ValueType;
                        bool IsNullableType = valueType.Name.Contains("Nullable");
                        string SearchValue = item.Value.ToLower();
                        if (IsNullableType)
                        {
                            valueType = valueType.GenericTypeArguments[0];
                        }
                        if (valueType == typeof(string))
                        {
                            searchingList = searchingList.Where(item.Column + " !=null && " + item.Column + ".ToLower().Contains(@0)", SearchValue).ToList();
                        }
                        else
                        {
                            if (IsNullableType)
                            {
                                searchingList = searchingList.Where(item.Column + " !=null && " + item.Column + ".Value.ToString().ToLower().Contains(@0)", SearchValue).ToList();
                            }
                            else
                            {
                                searchingList = searchingList.Where(item.Column + " != null && " + item.Column + ".ToString().ToLower().Contains(@0)", SearchValue).ToList();
                            }
                        }
                    }
                }
                catch (Exception exp)
                {

                }
            }
            return searchingList;
        }
        private TextBox FindControl(string ColumnName)
        {
            if (this.Controls.Find(ControlName(ColumnName), true).Length > 0)
            {
                return this.Controls.Find(ControlName(ColumnName), true)[0] as TextBox;
            }
            else return null;
        }
        private string ControlName(string ColumnName)
        {
            return l_Dgv.Name + "_" + ColumnName;
        }
        #endregion


    }
}
