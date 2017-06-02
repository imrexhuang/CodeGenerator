﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Banana.AutoCode.DbSchema;
using WeifenLuo.WinFormsUI.Docking;

namespace Banana.AutoCode.Forms
{
    public partial class DbPanel : DockContent, IDisposable
    {
        const Int32 ROOT_NODE_LEVEL = 0;
        const Int32 DB_NODE_LEVEL = 1;
        const Int32 TABLE_NODE_LEVEL = 2;
        const String DB_ON_ICON = "database";
        const String DB_OFF_ICON = "databaseOff";

        protected ImageList IconList;
        protected ConnectionStringSettings CurrentConnSetting;

        protected TreeNode LastDbNode;

        protected Hashtable ManagerMap;
        protected Hashtable TableMap;

        public DbPanel()
        {
            InitializeComponent();
            Init();
        }

        protected void Init()
        {
            ManagerMap = new Hashtable();
            TableMap = new Hashtable();
            IconList = new ImageList();

            IconList.Images.Add("databases", Properties.Resources.datas);
            IconList.Images.Add("database", Properties.Resources.data);
            IconList.Images.Add("databaseOff", Properties.Resources.data_off);
            IconList.Images.Add("tables", Properties.Resources.tables);
            
            tvDb.ImageList = IconList;
            tvDb.DrawMode = TreeViewDrawMode.OwnerDrawText;
            tvDb.DrawNode += new DrawTreeNodeEventHandler(tvDb_DrawNode);
            tvDb.NodeMouseClick += new TreeNodeMouseClickEventHandler(tvDb_NodeMouseClick);
            tvDb.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(tvDb_NodeMouseDoubleClick);
            
            RefreshTreeView();
        }

        protected void RefreshTreeView()
        {
            cbConnStrings.Items.Clear();
            tvDb.Nodes.Clear();
            
            foreach (DictionaryEntry item in TableMap)
            {
                var tableForm = item.Value as TablePanel;
                if (tableForm == null)
                {
                    continue;
                }

                tableForm.Close();
            }

            TableMap.Clear();
            cbConnStrings.Text = String.Empty;

            ConfigurationManager.RefreshSection("connectionStrings");
            foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
            {
                cbConnStrings.Items.Add(css.Name);
            }
        }

        private void ResetDbNodeIcon(TreeNode dbNode)
        {
            if (dbNode == null || dbNode == LastDbNode)
            {
                return;
            }

            if (LastDbNode != null)
            {
                LastDbNode.ImageKey = DB_OFF_ICON;
                LastDbNode.SelectedImageKey = DB_OFF_ICON;
            }
            LastDbNode = dbNode;
            LastDbNode.ImageKey = DB_ON_ICON;
            LastDbNode.SelectedImageKey = DB_ON_ICON;
        }

        void tvDb_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var table = e.Node.Tag as Table;

            if (e.Node.Level == DB_NODE_LEVEL)
            {
                ResetDbNodeIcon(e.Node);
            }

            if (e.Node.Level == TABLE_NODE_LEVEL)
            {
                ResetDbNodeIcon(e.Node.Parent);
                
                if (table == null)
                {
                    return;
                }

                var tableMapKey = cbConnStrings.Text + "." + table.ToString();
                var tableForm = TableMap[tableMapKey] as TablePanel;
                if (tableForm == null)
                {
                    tableForm = new TablePanel(table, CurrentConnSetting);
                    TableMap[tableMapKey] = tableForm;
                    tableForm.Text = e.Node.Text;
                    tableForm.Show(((Main)this.ParentForm).MainDockPanel);
                }
                else
                {
                    tableForm.Activate();
                }
            }
        }

        void tvDb_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Level != DB_NODE_LEVEL)
            {
                return;
            }

            var dbNode = e.Node;

            var db = dbNode.Tag as Database;
            if (db == null)
            {
                return;
            }

            if (db.Tables == null)
            {
                var tables = GetManager().GetTables(db);
                db.Tables = tables;

                foreach (var table in tables)
                {
                    var tableNode = new TreeNode(table.Name, 2, 2);
                    tableNode.Tag = table;
                    tableNode.ToolTipText = string.IsNullOrWhiteSpace(table.Comment) ? table.Name : table.Comment;
                    tableNode.ImageKey = "tables";
                    tableNode.SelectedImageKey = tableNode.ImageKey;
                    dbNode.Nodes.Add(tableNode);
                }
            }

            e.Node.Expand();
        }
        
        void tvDb_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node.Level == ROOT_NODE_LEVEL || e.Node.Level == TABLE_NODE_LEVEL)
            {
                e.Node.HideCheckBox();
            }

            e.DrawDefault = true;
        }

        private void cbConnStrings_SelectedIndexChanged(object sender, EventArgs e)
        {
            tvDb.Nodes.Clear();
            CurrentConnSetting = ConfigurationManager.ConnectionStrings[cbConnStrings.SelectedItem.ToString()];
            var dbSchemaManager = GetManager();

            var dbs = dbSchemaManager.GetDatabases();
            var root = new TreeNode(CurrentConnSetting.Name, 0, 0);
            root.ToolTipText = CurrentConnSetting.ConnectionString;
            root.ImageKey = "databases";
            root.SelectedImageKey = root.ImageKey;
            //root.TreeView.CheckBoxes = false;
            tvDb.Nodes.Add(root);
            tvDb.CheckBoxes = true;
            
            foreach (var db in dbs)
            {
                var dbNode = new TreeNode(db.Name, 1, 1);
                dbNode.Tag = db;
                dbNode.ToolTipText = db.Name;
                dbNode.ImageKey = "databaseOff";
                dbNode.SelectedImageKey = dbNode.ImageKey;
                
                root.Nodes.Add(dbNode);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshTreeView();
        }

        private DbSchemaManager GetManager()
        {
            var dbSchemaManager = ManagerMap[CurrentConnSetting] as DbSchemaManager;
            if (dbSchemaManager == null)
            {
                dbSchemaManager = new DbSchemaManager(CurrentConnSetting);
                ManagerMap[CurrentConnSetting] = dbSchemaManager;
            }

            return dbSchemaManager;
        }

        void IDisposable.Dispose()
        {
            this.ManagerMap.Clear();
            this.ManagerMap = null;

            this.TableMap.Clear();
            this.TableMap = null;

            this.Dispose(true);
        }
    }
}
