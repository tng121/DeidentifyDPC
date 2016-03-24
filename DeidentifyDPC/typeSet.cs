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
        private SettingData sd_;
        TextBox[] tboxs;
        bool listBoxEventEnable = true;

        public TypeSet()
        {
            InitializeComponent();
        }

        public void initialize()
        {
            sd_ = SettingData.load();
            sd_.types.Keys.ToList().ForEach(ftn => listBox1.Items.Add(ftn));
            tboxs = new TextBox[] { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8, textBox9, textBox10 };
        }

        public string selectedFileTypeName()
        {
            return listBox1.SelectedIndex == -1 ? null : listBox1.SelectedItem.ToString();
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!listBoxEventEnable) return;
            tboxs.ToList().ForEach(tb => tb.ReadOnly = true);
            button3.Text = "編集";
            if (listBox1.SelectedIndex == -1) return;

            FileType selft = sd_.types[listBox1.SelectedItem.ToString()];
            textBox1.Text = selft.typeName;
            textBox2.Text = selft.idColumn.ToString();
            textBox3.Text = selft.birthDateColumn.ToString();
            textBox4.Text = selft.postalCodeColumn.ToString();
            textBox5.Text = selft.birthDateConditionColumn.ToString();
            textBox6.Text = selft.birthDateConditionMatch;
            textBox7.Text = selft.postalCodeConditionColumn.ToString();
            textBox8.Text = selft.postalCodeConditionMatch;
            textBox9.Text = selft.admissionDateColumn.ToString();
            textBox10.Text = selft.namingRule;

        }

        //編集（登録）ボタン
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.ReadOnly)
            {
                if (listBox1.SelectedIndex == -1) return;
                listBoxEventEnable = false;
                //編集可にする
                tboxs.ToList().ForEach(tb => tb.ReadOnly = false);
                button3.Text = "登録";
                listBoxEventEnable = true;
            }
            else
            {
                //登録する
                uint idcp, bdcp, pccp, bdccp, pcccp, adcp;
                if (!validateInt(textBox2, out idcp, label3)) return;
                if (!validateInt(textBox3, out bdcp, label4)) return;
                if (!validateInt(textBox4, out pccp, label5)) return;
                if (!validateInt(textBox5, out bdccp, label6)) return;
                if (!validateInt(textBox7, out pcccp, label8)) return;
                if (!validateInt(textBox9, out adcp, label10)) return;

                listBoxEventEnable = false;
                FileType selft = sd_.types[listBox1.SelectedItem.ToString()];
                if (!sd_.types.ContainsKey(textBox1.Text))
                {
                    sd_.types.Remove(listBox1.SelectedItem.ToString());
                    sd_.types.Add(textBox1.Text, selft);
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                }
                selft.typeName = textBox1.Text;
                selft.idColumn = idcp;
                selft.birthDateColumn = bdcp;
                selft.postalCodeColumn = pccp;
                selft.birthDateConditionColumn = bdccp;
                selft.birthDateConditionMatch = textBox6.Text;
                selft.postalCodeConditionColumn = pcccp;
                selft.postalCodeConditionMatch = textBox8.Text;
                selft.admissionDateColumn = adcp;
                selft.namingRule = textBox10.Text;

                sd_.serialize();

                tboxs.ToList().ForEach(tb => tb.ReadOnly = true);
                button3.Text = "編集";
                listBoxEventEnable = true;

            }

        }

        private bool validateInt(TextBox tb, out uint val, Label label)
        {
            bool res = uint.TryParse(tb.Text, out val);
            if (!res) MessageBox.Show(label.Text + "が数字ではありません。");
            return res;
        }

        //追加ボタン
        private void button4_Click(object sender, EventArgs e)
        {
            string newname = "新規ファイルタイプ";
            if (sd_.types.ContainsKey(newname)) return;

            listBoxEventEnable = false;
            listBox1.Items.Add(newname);
            sd_.types.Add(newname, new FileType(newname, "", 0, 0, 0, "", 0, 0, "", 0));
            sd_.serialize();
            listBoxEventEnable = true;

            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            tboxs.ToList().ForEach(tb => tb.ReadOnly = false);
            button3.Text = "登録";
        }

        //削除ボタン
        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("ファイルタイプ\"" + listBox1.SelectedItem + "\"を削除しますか？", "削除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
            if (res == DialogResult.Yes)
            {
                listBoxEventEnable = false;
                sd_.types.Remove(listBox1.SelectedItem.ToString());
                listBox1.Items.Remove(listBox1.SelectedItem);
                listBox1.SelectedIndex = -1;
                tboxs.ToList().ForEach(tb => { tb.Text = ""; tb.ReadOnly = true; });
                button3.Text = "編集";
                sd_.serialize();
                listBoxEventEnable = true;
            }
        }
    }
}
