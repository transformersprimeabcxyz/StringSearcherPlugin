using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetResolver.Plugins;
using System.Threading;
using TUP.AsmResolver;
using TUP.AsmResolver.NET;
using TUP.AsmResolver.NET.Specialized;
using TUP.AsmResolver.NET.Specialized.MSIL;
namespace StringSearcher
{
    public partial class StringSearcherControl : UserControl
    {
        IConnector connector;
        Win32Assembly currentAssembly;
        int currentSearchIndex = 0;

        public StringSearcherControl(IConnector connector)
        {
            InitializeComponent();
            this.connector = connector;
            comboBox1.SelectedIndex = 2;
            comboBox2.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentAssembly = connector.CurrentAssembly;
            if (currentAssembly == null)
            {
                MessageBox.Show("Please select a member or assembly.", "String Searcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Thread searchThread = new Thread(SearchAsync)
                {
                    Priority = (ThreadPriority)comboBox1.SelectedIndex
                };
                searchThread.Start();
            }
        }



        private void SearchAsync()
        {
            List<ListViewItem> items = new List<ListViewItem>();

            NETHeader netHeader = currentAssembly.NETHeader;

            MetaDataTable methodTable = netHeader.TablesHeap.GetTable(MetaDataTableType.Method);
            

            foreach (MetaDataMember member in methodTable.Members)
            {
                MethodDefinition methodDef = member as MethodDefinition;

                string methodName;
                string fullName;
                try
                {
                    methodName = methodDef.Name;
                }
                catch
                {
                    methodName = string.Empty;
                }
                try
                {
                    fullName = methodDef.FullName;
                }
                catch
                {
                    fullName = string.Empty;
                }

                Invoke(new Action<string, double,double>(ProcessCallBack), "Scanning " + (fullName == string.Empty ? methodDef.MetaDataToken.ToString("X8") : fullName), member.MetaDataToken - ((int)member.TableType << 24), methodTable.Members.Count);
                if (methodDef.HasBody)
                {
                    try
                    {
                        MSILDisassembler disassembler = new MSILDisassembler(methodDef.Body);
                        MSILInstruction[] instructions = disassembler.Disassemble();

                        foreach (MSILInstruction instruction in instructions)
                        {
                            if (instruction.OpCode.Code == MSILCode.Ldstr)
                            {
                                string value = instruction.Operand as string;
                                items.Add(new ListViewItem(new string[] {
                                    value,
                                    "IL_" + instruction.Offset.ToString("X4"),
                                    methodDef.MetaDataToken.ToString("X8"),
                                    methodName,
                                    fullName,
                                }) { Tag = methodDef });
                            }
                        }
                    }
                    catch
                    {
                        // handle
                    }
                }

                
            }

            Invoke(new Action<List<ListViewItem>>(SearchCallBack), items);
        }

        private void ProcessCallBack(string status, double currentItem, double maximum)
        {
            label1.Text = status;
            double percent = currentItem / maximum * 100;
            progressBar1.Value = (int)percent;
        }
        private void SearchCallBack(List<ListViewItem> items)
        {
            listView1.Items.Clear();
            listView1.Items.AddRange(items.ToArray());
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                
                MethodDefinition methodDef = listView1.SelectedItems[0].Tag as MethodDefinition;
                AssemblyDefinition asmDef = connector.CurrentAssemblyDefinition;
                if (methodDef != null)
                {
                    connector.Navigator.NavigateToMember(asmDef, methodDef.MetaDataToken);
                    
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string keyWord = textBox1.Text.ToLower();
            int columnIndex = comboBox2.SelectedIndex;

            for (int i = currentSearchIndex; i < listView1.Items.Count; i++) 
            {
                if (listView1.Items[i].SubItems[columnIndex].Text.ToLower().Contains(keyWord))
                {
                    listView1.Items[i].Selected = true;
                    listView1.TopItem = listView1.Items[i];
                    currentSearchIndex = i + 1;
                    listView1.Focus();
                    return;
                }
            }

            currentSearchIndex = 0;
            MessageBox.Show("No more results where found", "String Searcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
