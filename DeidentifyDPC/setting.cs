using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace DeidentifyDPC
{
    public partial class SettingDialog : Form
    {
        private SettingData sd_;
        private List<string> initialAvailableTransRule_ = new List<string>();

        public SettingDialog()
        {
            InitializeComponent();
            SettingData sd = SettingData.load();
            
            if(sd.encryptStrings != null)
            {
                sd.encryptStrings.ForEach(es => listBox1.Items.Add(es));
            }
            if(sd.selectedEnc != null)
            {
                textBox1.Text = sd.selectedEnc;
            }
            checkBox1.Checked = sd.encryptId;
            checkBox2.Checked = sd.ambiguousBirthDate;
            checkBox3.Checked = sd.ambiguousPostalCode;

            string[] bddes = BirthDateModifier.descriptions();
            comboBox1.Items.AddRange(bddes);
            comboBox1.Text = (BirthDateModifier.TypedConstructor(sd.birthDateModifyType)).ToString();

            string[] cpdes = PostalCodeModifier.descriptions();
            comboBox2.Items.AddRange(cpdes);
            comboBox2.Text = (PostalCodeModifier.TypedConstructor(sd.postalCodeModifyType)).ToString();

            initialAvailableTransRule_.Add(textBox1.Text);
            foreach(string str in listBox1.Items)
            {
                initialAvailableTransRule_.Add(str);
            }

        }

        //ランダム作成ボタン
        private void button1_Click(object sender, EventArgs e)
        {
            ModularExponentiateTransformation me = new ModularExponentiateTransformation();
            LeftRotateTransformation lr = new LeftRotateTransformation();
            LinearTransformation ln = new LinearTransformation();
            string exp = me.ToString() + lr.ToString() + ln.ToString();
            textBox1.Text = exp;
        }

        //変換キーの登録ボタン
        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "") return;
            foreach(string str in listBox1.Items)
            {
                if (str == textBox1.Text) return;
            }
            listBox1.Items.Add(textBox1.Text);
        }

        //OKボタン
        private void button3_Click(object sender, EventArgs e)
        {
            if (!initialAvailableTransRule_.All(tr => tr == textBox1.Text || listBox1.Items.Contains(tr)))
            {
                DialogResult res = MessageBox.Show("変換キーが削除されています。削除された変換キーは二度と復元できませんが、よろしいですか？", "設定保存確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }
            if (!listBox1.Items.Contains(textBox1.Text))
            {
                MessageBox.Show("選択されている変換キーが候補一覧に含まれていません。候補一覧に追加します。", "設定保存確認");
                listBox1.Items.Add(textBox1.Text);
            }

            sd_ = new SettingData();
            foreach (string str in listBox1.Items)
            {
                sd_.encryptStrings.Add(str);
            }
            sd_.selectedEnc = textBox1.Text;
            sd_.encryptId = checkBox1.Checked;
            sd_.ambiguousBirthDate = checkBox2.Checked;
            sd_.ambiguousPostalCode = checkBox3.Checked;
            sd_.birthDateModifyType = BirthDateModifier.getTypeNum(comboBox1.Text);
            sd_.postalCodeModifyType = PostalCodeModifier.getTypeNum(comboBox2.Text);
            sd_.serialize();
        }

        //変換キーの削除ボタン
        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            DialogResult res = MessageBox.Show("変換キーを削除しますか？　削除すると同じ変換キーは二度と作れません。", "削除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
            if (res == DialogResult.Yes)
            {
                listBox1.Items.Remove(listBox1.SelectedItem);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1) return;
            textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
        }
        
        public string getEncryptionString()
        {
            return sd_.selectedEnc;
        }

        public bool getEncryptId()
        {
            return sd_.encryptId;
        }

        public bool getAmbiguousBirthDate()
        {
            return sd_.ambiguousBirthDate;
        }

        public bool getAmbiguousPostalCode()
        {
            return sd_.ambiguousPostalCode;
        }

        public int getBirthDateModifyType()
        {
            return sd_.birthDateModifyType;
        }

        public int getPostalCodeModifyType()
        {
            return sd_.postalCodeModifyType;
        }
    }

    [DataContract]
    [KnownType(typeof(FileType))]
    public class SettingData : IExtensibleDataObject
    {
        [DataMember]
        public List<string> encryptStrings { get; set; }

        [DataMember]
        public string selectedEnc { get; set; }

        [DataMember]
        public Dictionary<string, FileType> types { get; set; }

        [DataMember]
        public bool encryptId { get; set; }

        [DataMember]
        public bool ambiguousBirthDate { get; set; }

        [DataMember]
        public bool ambiguousPostalCode { get; set; }

        [DataMember]
        public int birthDateModifyType { get; set; }

        [DataMember]
        public int postalCodeModifyType { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
        private static string file = "settings.xml";

        public SettingData()
        {
            encryptStrings = new List<string>();
            selectedEnc = "";
            types = new Dictionary<string, FileType>();
            types.Add("FF1", new FileType("FF1", @"FF1_[0-9]{9}_[0-9]{4}\.txt", 2, 9, 6, "A000010", 11, 6, "A000010", 3));
            types.Add("FF4", new FileType("FF4", @"FF4_[0-9]{9}_[0-9]{4}\.txt", 2, 0, 0, "", 0, 0, "", 3));
            types.Add("Dn", new FileType("Dn", @"Dn_[0-9]{9}_[0-9]{4}\.txt", 2, 0, 0, "", 0, 0, "", 4));
            types.Add("EFn", new FileType("EFn", @"EFn_[0-9]{9}_[0-9]{4}\.txt", 2, 0, 0, "", 0, 0, "", 4));
            types.Add("EFg", new FileType("EFg", @"EFg_[0-9]{9}_[0-9]{4}\.txt", 2, 0, 0, "", 0, 0, "", 4));
            types.Add("Hn", new FileType("Hn", @"Hn_[0-9]{9}_[0-9]{4}\.txt", 3, 0, 0, "", 0, 0, "", 5));
            types.Add("FF1(H25)", new FileType("FF1(H25)", "", 4, 6, 0, "", 7, 0, "", 10));
            encryptId = true;
            ambiguousBirthDate = false;
            ambiguousPostalCode = false;
            birthDateModifyType = BirthDateTo0101.TYPENUM;
            postalCodeModifyType = PostalCodeTo0000000.TYPENUM;
        }

        private static string fullPath()
        {
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            FileInfo appFileInfo = new FileInfo(appPath);
            return appFileInfo.DirectoryName + @"\" + file;
        }

        public void serialize()
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(SettingData));
            XmlWriterSettings xwset = new XmlWriterSettings();
            xwset.Encoding = new System.Text.UTF8Encoding();
            using (XmlWriter xw = XmlWriter.Create(fullPath(), xwset))
            {
                dcs.WriteObject(xw, this);
            }
        }

        public static SettingData load()
        {
            FileInfo setFileInfo = new FileInfo(fullPath());
            if (!setFileInfo.Exists)
            {
                return new SettingData();
            }

            DataContractSerializer dcs = new DataContractSerializer(typeof(SettingData));
            using (XmlReader xr = XmlReader.Create(fullPath()))
            {
                return (SettingData)dcs.ReadObject(xr);
            }
        }

    }

    [DataContract]
    public class FileType : IExtensibleDataObject
    {
        [DataMember]
        public string typeName { get; set; }

        [DataMember]
        public string namingRule { get; set; }

        [DataMember]
        public uint idColumn { get; set; }

        [DataMember]
        public uint birthDateColumn { get; set; }

        [DataMember]
        public uint birthDateConditionColumn { get; set; }

        [DataMember]
        public string birthDateConditionMatch { get; set; }

        [DataMember]
        public uint postalCodeColumn { get; set; }

        [DataMember]
        public uint postalCodeConditionColumn { get; set; }

        [DataMember]
        public string postalCodeConditionMatch { get; set; }

        [DataMember]
        public uint admissionDateColumn { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }

        public FileType(string tn, string nr, uint ic, uint bdc, uint bdcc, string bdcm, uint pcc, uint pccc, string pccm, uint addc)
        {
            this.typeName = tn;
            this.namingRule = nr;
            this.idColumn = ic;
            this.birthDateColumn = bdc;
            this.birthDateConditionColumn = bdcc;
            this.birthDateConditionMatch = bdcm;
            this.postalCodeColumn = pcc;
            this.postalCodeConditionColumn = pccc;
            this.postalCodeConditionMatch = pccm;
            this.admissionDateColumn = addc;
        }
    }
}
