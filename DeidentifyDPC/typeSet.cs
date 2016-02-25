using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeidentifyDPC
{
    public partial class TypeSet : Form
    {
        public TypeSet()
        {
            InitializeComponent();
        }

        public void setTypes(List<string> ftnames)
        {
            foreach (string ftn in ftnames)
            {
                listBox1.Items.Add(ftn);
            }
        }

        public string selectedFileTypeName()
        {
            return listBox1.SelectedIndex == -1 ? null : listBox1.SelectedItem.ToString();
        }
    }
}
