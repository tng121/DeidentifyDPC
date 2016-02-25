using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

/*
 * 課題管理
 * 
 * 生年月日匿名化：１月１日にする、入院日に合わせる、年の１０の位だけ残す　作ったけどテストしてないのと入院日作ってない
 * 郵便番号匿名化：下４桁を0にする(9999999を除く)、すべて0000000にする 作ったけとテストしてない
 * 並べ替え：匿名化後のID順に並べ替える
 * 
 */

namespace DeidentifyDPC
{
    public partial class Form1 : Form
    {
        private Dictionary<string, FileType> types_ = new Dictionary<string, FileType>();
        private Dictionary<string, string> idDict_ = new Dictionary<string, string>();
        private IdEncryption encrypt;
        private bool encryptId;
        private bool ambiguousBirthDate;
        private bool ambiguousPostalCode;
        private BirthDateModifier bdmod;
        private PostalCodeModifier pcmod;
        public Form1()
        {
            InitializeComponent();
            SettingData sd = SettingData.load();
            types_ = sd.types;
            encrypt = IdEncryption.StringConstructor(sd.selectedEnc);
            encryptId = sd.encryptId;
            ambiguousBirthDate = sd.ambiguousBirthDate;
            ambiguousPostalCode = sd.ambiguousPostalCode;
            bdmod = BirthDateModifier.TypedConstructor(sd.birthDateModifyType);
            pcmod = PostalCodeModifier.TypedConstructor(sd.postalCodeModifyType);
            if (encrypt.isBlank())
            {
                button5_Click(null, null);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string path in ofd.FileNames)
                {
                    addFileList(fileTypeByRule(path), path);
                }
            }
            displayProcessableFiles();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            displayProcessableFiles();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (encrypt.isBlank())
            {
                MessageBox.Show("変換キーが設定されていません。");
                button5_Click(sender, e);
                return;
            }
            backgroundWorker1.RunWorkerAsync();
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");
            int processed = 0;

            foreach (string lbi in listBox1.Items)
            {
                FileType type = getTypeFromList(lbi);
                if (type == null) continue;
                string path = getPathFromList(lbi);

                string deidPath = deidentifiedPath(path);
                using (StreamWriter sw = new StreamWriter(deidPath, false, sjis))
                {
                    using (StreamReader sr = new StreamReader(path, sjis))
                    {
                        string line = null;
                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(deidentify(line, type));
                        }
                    }
                }

                using (StreamWriter swid = new StreamWriter(idMapPath(path), false, sjis))
                {
                    foreach (KeyValuePair<string, string> kvp in idDict_.ToList())
                    {
                        swid.WriteLine(kvp.Key + "\t" + kvp.Value);
                    }
                }
                idDict_.Clear();
                processed++;

                int percent = (int)((double)processed / countProcessableFiles() * 100);

                backgroundWorker1.ReportProgress(percent, processed + " / " + countProcessableFiles());
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            textBox1.Text = e.UserState.ToString() + " ファイル匿名化処理済";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("匿名化が終了しました。");
            displayProcessableFiles();
            progressBar1.Value = 0;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TypeSet ts = new TypeSet();
            ts.setTypes(this.types_.Keys.ToList());

            DialogResult tsRes = ts.ShowDialog();
            string ftname = ts.selectedFileTypeName();
            if (tsRes == DialogResult.Cancel || ftname == null)
            {
                ts.Dispose();
                return;
            }

            FileType selft = this.types_[ftname];
            ts.Dispose();

            for (int i = listBox1.Items.Count - 1; i >= 0; i-- )
            {
                if(listBox1.GetSelected(i)) setTypeToList(selft, i);
            }

            listBox1.ClearSelected();
            displayProcessableFiles();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SettingDialog sd = new SettingDialog();

            DialogResult sdRes = sd.ShowDialog();
            if (sdRes == DialogResult.Cancel)
            {
                sd.Dispose();
                return;
            }

            string encryptString = sd.getEncryptionString();
            encrypt = IdEncryption.StringConstructor(encryptString);
            encryptId = sd.getEncryptId();
            ambiguousBirthDate = sd.getAmbiguousBirthDate();
            bdmod = BirthDateModifier.TypedConstructor(sd.getBirthDateModifyType());
            ambiguousPostalCode = sd.getAmbiguousPostalCode();
            pcmod = PostalCodeModifier.TypedConstructor(sd.getPostalCodeModifyType());
            sd.Dispose();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < listBox1.Items.Count; i++)
            {
                listBox1.SetSelected(i, !listBox1.GetSelected(i));
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            for(int i = listBox1.Items.Count - 1; i >= 0; i--)
            {
                if(listBox1.GetSelected(i)) listBox1.Items.RemoveAt(i);
            }
            displayProcessableFiles();
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string d in drags)
                {
                    if (!System.IO.File.Exists(d)) return;
                }
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in files)
            {
                addFileList(fileTypeByRule(path), path);
            }
            displayProcessableFiles();
        }

        private string deidentifiedPath(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fn = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dir + "\\" + fn + "_deid" + ext;
        }

        private string idMapPath(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fn = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return dir + "\\" + fn + "_map" + ext;
        }

        private FileType fileTypeByRule(string path)
        {
            return types_.Values.FirstOrDefault(ft => ft.namingRule == "" ? false : new Regex(ft.namingRule).IsMatch(Path.GetFileName(path)));
        }

        private void addFileList(FileType type, string path)
        {
            string tname = type == null ? "TYPE?" : type.typeName;
            listBox1.Items.Add("[" + tname + "] " + path);
        }

        private FileType getTypeFromList(string item)
        {
            string typeName = Regex.Match(item, @"\[(.+)\] .+").Groups[1].ToString();
            if (typeName == "TYPE?") return null;
            return types_[typeName];
        }

        private string getPathFromList(string item)
        {
            return Regex.Match(item, @"\[.+\] (.+)").Groups[1].ToString();
        }

        private void setTypeToList(FileType type, int selectedIndex)
        {
            string path = getPathFromList(listBox1.Items[selectedIndex].ToString());
            string tname = type == null ? "TYPE?" : type.typeName;
            listBox1.Items[selectedIndex] = "[" + tname + "] " + path;
        }

        private int countProcessableFiles()
        {
            int c = 0;
            foreach (object li in listBox1.Items)
            {
                FileType ft = getTypeFromList((string)li);
                if (ft != null) c++;
            }
            return c;
        }

        private void displayProcessableFiles()
        {
            textBox1.Text = countProcessableFiles() + " ファイル匿名化処理可";
        }

        private string deidentify(string line, FileType type)
        {
            string[] vals = line.Split("\t".ToCharArray());
            if (encryptId)
            {
                if (0 < type.idColumn && type.idColumn <= vals.Length)
                {
                    vals[type.idColumn - 1] = modifyId(vals[type.idColumn - 1]);
                }
            }
            if (ambiguousBirthDate && 0 < type.birthDateColumn && type.birthDateColumn <= vals.Length)
            {
                if (0 < type.birthDateConditionColumn && type.birthDateConditionColumn <= vals.Length ? vals[type.birthDateConditionColumn - 1] == type.birthDateConditionMatch : true)
                {
                    string stv = vals[type.birthDateColumn - 1];
                    ulong lv;
                    if (UInt64.TryParse(stv, out lv)) //10進変換に失敗した場合は何もしない
                    {
                        string add = 0 < type.admissionDateColumn && type.admissionDateColumn <= vals.Length ? vals[type.admissionDateColumn - 1] : null;
                        vals[type.birthDateColumn - 1] = bdmod.modify(stv, add);
                    }
                }
            }
            if (ambiguousPostalCode && 0 < type.postalCodeColumn && type.postalCodeColumn <= vals.Length)
            {
                if (0 < type.postalCodeConditionColumn && type.postalCodeConditionColumn <= vals.Length ? vals[type.postalCodeConditionColumn - 1] == type.postalCodeConditionMatch : true)
                {
                    string stv = vals[type.postalCodeColumn - 1];
                    ulong lv;
                    if (UInt64.TryParse(stv, out lv)) //10進変換に失敗した場合は何もしない
                    {
                        vals[type.postalCodeColumn - 1] = pcmod.modify(stv);
                    }

                }
            }
            return String.Join("\t", vals);
        }

        private string modifyId(string id)
        {
            if (idDict_.ContainsKey(id)) return idDict_[id];

            ulong idv;
            if (!UInt64.TryParse(id, out idv)) return id; //10進変換に失敗した場合は何もしない

            string deid = encrypt.encrypt(idv).ToString("D10");
            idDict_.Add(id, deid);
            return deid;
        }

    }

    public class IdEncryption
    {
        private List<Transformation> trans_ = new List<Transformation>();

        public IdEncryption(List<Transformation> trans)
        {
            trans_ = trans_.Concat(trans).ToList();
        }

        public ulong encrypt(ulong original)
        {
            ulong result = original;
            trans_.ForEach(tr => result = tr.transform(result));
            return result;
        }

        public bool isBlank()
        {
            return trans_.Count == 0;
        }

        public static IdEncryption StringConstructor(string s)
        {
            List<Transformation> lt = s.Split('}').Where(e => e.Length > 0).Select(e => Transformation.StringConstrucor(e + "}")).Where(t => t != null).ToList();
            IdEncryption enc = new IdEncryption(lt);
            return enc;
        }

        public override string ToString()
        {
            return string.Join("", trans_);
        }
    }

    //10桁整数から10桁整数への変換で衝突が一切発生しないものを考えたい。
    //一般的なハッシュ関数(SHA-256等)を使うことも考えたが、上記条件を満たすようには見えなかったので、
    //とりあえずは原始的だが確実な方法をとった。
    //暗号学的な耐性には乏しいが、結果だけから操作を推定して復元するのは比較的難しいと思われる。
    public abstract class Transformation
    {
        public abstract ulong transform(ulong original);

        public abstract bool isValid();

        //64進数だと6桁。85進数にすると5桁ですむが…
        private static string b64s = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_";

        private static IEnumerable<Type> registeredSub_ = new List<Type>() { typeof(LinearTransformation), typeof(LeftRotateTransformation), typeof(ModularExponentiateTransformation) };
        
        public static long LongRandom(long min, long max, Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }

        public static Transformation StringConstrucor(string s)
        {
            Match m = Regex.Match(s, @"\{(\w+)\:(.+)\}");
            string type = m.Groups[1].ToString();
            //IEnumerable<ulong> args = m.Groups[2].ToString().Split(',').Select(str => UInt64.Parse(str));
            IEnumerable<ulong> args = m.Groups[2].ToString().Split(',').Select(str => From64(str));
            Type subType = registeredSub_.SingleOrDefault(st => st.GetField("trcode").GetValue(null).ToString() == type);
            if (subType == null) return null;
            System.Reflection.ConstructorInfo cnst = subType.GetConstructors().SingleOrDefault(c => c.GetParameters().Count() == args.Count());
            if (cnst == null) return null;
            Transformation tr = (Transformation)cnst.Invoke(args.Cast<object>().ToArray());
            return tr;
        }

        public static string To64(ulong l)
        {
            return l == 0 ? "" : To64(l / 64) + b64s.Substring((int)(l % 64), 1);
        }

        private static ulong From64Char(char[] ca, int readi)
        {
            if (ca.Length == readi)
            {
                return 0;
            }
            int i = b64s.IndexOf(ca[ca.Length - readi - 1]);
            if (i == -1)
            {
                throw new ArgumentException();
            }
            return (ulong)b64s.IndexOf(ca[ca.Length - readi - 1]) + From64Char(ca, readi + 1) * 64;
        }

        public static ulong From64(string s)
        {
            return From64Char(s.ToCharArray(), 0);
        }
    }

    public class LinearTransformation : Transformation
    {
        private ulong multiple_;
        private ulong slide_;
        public readonly static string trcode = "ln";

        public LinearTransformation()
        {
            ulong m, s;
            Random rand = new Random();
            do
            {
                m = (ulong)LongRandom(10000000, 100000000, rand);
                s = (ulong)LongRandom(100000000, 10000000000, rand);
            } while (!isValid(m, s));
            multiple_ = m;
            slide_ = s;
            if (!isValid()) throw new ArgumentException();
        }

        public LinearTransformation(ulong multiple, ulong slide)
        {
            multiple_ = multiple;
            slide_ = slide;
            if (!isValid()) throw new ArgumentException();
        }

        public static bool isValid(ulong m, ulong s)
        {
            if (m > UInt64.MaxValue / 9999999999) return false;
            if (m % 2 == 0 || m % 5 == 0) return false;
            if (s > 9999999999) return false;
            return true;
        }

        public override bool isValid()
        {
            return isValid(multiple_, slide_);
        }

        public override ulong transform(ulong original)
        {
            ulong m = original * multiple_ % 10000000000;
            return (m + slide_) % 10000000000;
        }
        
        public override string ToString()
        {
            //return "{" + trcode + ":" + multiple_.ToString() + "," + slide_.ToString() + "}";
            return "{" + trcode + ":" + Transformation.To64(multiple_) + "," + Transformation.To64(slide_) + "}";
        }
    }

    public class LeftRotateTransformation : Transformation
    {
        private ulong rotate_;
        public readonly static string trcode = "lr";

        public LeftRotateTransformation()
        {
            ulong r;
            do
            {
                r = (ulong)(new Random()).Next(1, 10);
            } while (!isValid(r));
            rotate_ = r;
            if (!isValid()) throw new ArgumentException();
        }

        public LeftRotateTransformation(ulong rotate)
        {
            rotate_ = rotate;
            if (!isValid()) throw new ArgumentException();
        }

        public static bool isValid(ulong rotate)
        {
            if (rotate >= 10) return false;
            return true;
        }

        public override bool isValid()
        {
            return isValid(rotate_);
        }

        public override ulong transform(ulong original)
        {
            return original % (ulong)Math.Pow(10, 10 - rotate_) * (ulong)Math.Pow(10, rotate_) + original / (ulong)Math.Pow(10, 10 - rotate_);
        }

        public override string ToString()
        {
            //return "{" + trcode + ":" + rotate_.ToString() + "}";
            return "{" + trcode + ":" + Transformation.To64(rotate_) + "}";
        }

    }

    //冪剰余変換
    //RSA暗号の仕組みから、任意のn、任意の素数のP,Q（ただしP≠Q）があり、
    //A×B＝n×(P-1)×(Q-1)＋1
    //を満たすようなA,Bがあれば、元の数のA×B乗をP×Qで割った余り（べき剰余）は元の数に戻る。
    //つまり、このようなA(やB)（≠1）について、A乗の操作は衝突しない変換となるが、無変換の可能性もある。
    //理論的にははっきりしてはいないが、無変換となる操作はA×B乗のほか、経験的にはn×LCM(P-1,Q-1)＋1乗でもなるような気がする。
    //(LCM＝最小公倍数)
    //このようなA,Bになれない条件を考えると、nがどのような数であってもn×(P-1)×(Q-1)で割り切れない、つまり(P-1)×(Q-1)と2以上の約数を持つ数である。
    //よって、A,Bは(P-1)×(Q-1)と互いに素な数から選べばよい。ここでは簡略化のため、50以下であるAを用いた。
    //ただし、10の10乗は素数2つの積で表せないため、R,S,T,Uを素数として
    //10の10乗＝R×S＋T×U
    //を満たすR,S,T,Uを見つけ、0～R×S-1の範囲とR×S～10の10乗の範囲に分けて変換すると10桁整数から10桁整数の変換として適切である。
    //もっとも、ulongでは4294967295の2乗が限界なので、この大きさ以内の3つ以上の範囲に分割する必要がある。
    //なお、範囲の分割によって、よく観察すると一定範囲内で移動させていることが明らかになりやすいため、
    //この変換後に別の変換方式でさらに攪拌する必要がある。
    public class ModularExponentiateTransformation : Transformation
    {
        private ulong range1div1, range1div2, range2div1, range2div2, range3div1, range3div2, exp1, exp2, exp3, range1, range2, range3;
        private static ulong max_mod_exp_base = (ulong)Math.Sqrt(UInt64.MaxValue);
        public readonly static string trcode = "me";

        public ModularExponentiateTransformation()
        {
            ulong min_mod_exp_base = 10000000000 - 2 * max_mod_exp_base;

            Random rnd = new Random();
            ulong[] rda, rda3;
            int failc;
            rda3 = new ulong[0];
            do{
                failc = 0;
                do
                {
                    range1 = (ulong)Transformation.LongRandom((long)min_mod_exp_base, (long)max_mod_exp_base, rnd);
                    rda = primeFactors(range1).ToArray();
                } while (rda.Length != 2);
                range1div1 = rda[0];
                range1div2 = rda[1];
                do
                {
                    failc++;
                    if (failc > 10000) break;
                    range2 = (ulong)Transformation.LongRandom((long)min_mod_exp_base, (long)max_mod_exp_base, rnd);
                    range3 = 10000000000 - range1 - range2;
                    if (range3 < min_mod_exp_base || max_mod_exp_base < range3) continue;
                    rda = primeFactors(range2).ToArray();
                    rda3 = primeFactors(range3).ToArray();
                } while (rda.Length != 2 || rda3.Length != 2);
            } while (failc > 10000);
            range2div1 = rda[0];
            range2div2 = rda[1];
            range3div1 = rda3[0];
            range3div2 = rda3[1];

            ulong rphi;
            do{
                exp1 = (ulong)rnd.Next(2, 50);
                rphi = (range1div1 - 1) * (range1div2 - 1);
            } while (GCD(rphi, exp1) > 1);
            do
            {
                exp2 = (ulong)rnd.Next(2, 50);
                rphi = (range2div1 - 1) * (range2div2 - 1);
            } while (GCD(rphi, exp2) > 1);
            do
            {
                exp3 = (ulong)rnd.Next(2, 50);
                rphi = (range3div1 - 1) * (range3div2 - 1);
            } while (GCD(rphi, exp3) > 1);
            range1 = range1div1 * range1div2;
            range2 = range2div1 * range2div2;
            range3 = range3div1 * range3div2;
            if (!isValid()) throw new ArgumentException(string.Join(",", new ulong[]{range1div1, range1div2, range2div1, range2div2, range3div1, range3div2, exp1, exp2, exp3}));
        }

        public ModularExponentiateTransformation(ulong rng1div1, ulong rng1div2, ulong rng2div1, ulong rng2div2, ulong rng3div1, ulong rng3div2, ulong ex1, ulong ex2, ulong ex3)
        {
            range1div1 = rng1div1;
            range1div2 = rng1div2;
            range2div1 = rng2div1;
            range2div2 = rng2div2;
            range3div1 = rng3div1;
            range3div2 = rng3div2;
            exp1 = ex1;
            exp2 = ex2;
            exp3 = ex3;
            range1 = range1div1 * range1div2;
            range2 = range2div1 * range2div2;
            range3 = range3div1 * range3div2;
            if (!isValid()) throw new ArgumentException();
        }

        public static bool isValid(ulong r1d1, ulong r1d2, ulong r2d1, ulong r2d2, ulong r3d1, ulong r3d2, ulong e1, ulong e2, ulong e3)
        {
            ulong r1 = r1d1 * r1d2;
            ulong r2 = r2d1 * r2d2;
            ulong r3 = r3d1 * r3d2;
            if (r1 + r2 + r3 != 10000000000) return false;
            if (new ulong[3] { r1, r2, r3 }.Any(r => r > max_mod_exp_base)) return false;
            if (new ulong[6] { r1d1, r1d2, r2d1, r2d2, r3d1, r3d2 }.Any(rd => !isPrime(rd))) return false;
            ulong[] rdm = new ulong[3] { (r1d1 - 1) * (r1d2 - 1), (r2d1 - 1) * (r2d2 - 1), (r3d1 - 1) * (r3d2 - 1) };
            if (rdm.Zip(new ulong[3]{e1, e2, e3}, (rd, e) => GCD(rd, e)).Any(g => g > 1)) return false;
            return true;
        }

        public override bool isValid()
        {
            return isValid(range1div1, range1div2, range2div1, range2div2, range3div1, range3div2, exp1, exp2, exp3);
        }

        public override ulong transform(ulong original)
        {
            if (original < range1)
            {
                return mod_exp(original, exp1, range1) + range2 + range3;
            }
            else if (original < range1 + range2)
            {
                return mod_exp(original - range1, exp2, range2) + range3;
            }
            return mod_exp(original - range1 - range2, exp3, range3);
        }

        public override string ToString()
        {
            ulong[] prms = new ulong[]{range1div1, range1div2, range2div1, range2div2, range3div1, range3div2, exp1, exp2, exp3};
            //return "{" + trcode + ":" + string.Join(",", prms) + "}";
            return "{" + trcode + ":" + string.Join(",", prms.Select(v => Transformation.To64(v))) + "}";
        } 

        public static ulong mod_exp(ulong b, ulong e, ulong m)
        {
            ulong result = 1;
            while (e > 0)
            {
                result = (result * b) % m;
                e--;
            }
            return result;
        }

        public static IEnumerable<ulong> primeFactors(ulong n)
        {
            ulong i = 2;
            ulong tmp = n;
            
            while (i * i <= n)
            {
                if(tmp % i == 0)
                {
                    tmp /= i;
                    yield return i;
                }
                else
                {
                    i = i == 2 ? 3 : i == 3 ? 5 : i % 6 == 5 ? i + 2 : i + 4;
                }
            }
            if(tmp != 1) yield return tmp;
        }

        public static bool isPrime(ulong n)
        {
            return primeFactors(n).Count() == 1;
        }

        public static ulong GCD(ulong m, ulong n)
        {
            return n == 0 ? m : GCD(n, m % n);
        }
    }

    public abstract class BirthDateModifier
    {
        private static List<Type> registeredSub_ = new List<Type>() { typeof(BirthDateToAdmDate), typeof(BirthDateTo0101), typeof(BirthDateTo00101) };

        public abstract string modify(string bd, string based);

        public static BirthDateModifier TypedConstructor(int type)
        {
            Type subType = registeredSub_.SingleOrDefault(t => getTypeNum(t) == type);
            if (subType == null) subType = registeredSub_[0];
            return getInstance(subType);
        }

        public static string[] descriptions()
        {
            return registeredSub_.Select(type => getTypeNum(type) + ":" + getDescription(type)).ToArray();
        }
        
        public static int getTypeNum(string numdes)
        {
            int found = numdes.IndexOf(':');
            if (found == -1)
            {
                return -1;
            }
            string typestr = numdes.Substring(0, found);
            int typenum;
            if (!Int32.TryParse(typestr, out typenum))
            {
                return -1;
            }
            return typenum;
        }

        //reflection methods
        private static BirthDateModifier getInstance(Type t)
        {
            System.Reflection.ConstructorInfo cnst = t.GetConstructor(new Type[0]);
            return (BirthDateModifier)cnst.Invoke(new object[0]);
        }

        private static int getTypeNum(Type t)
        {
            return (int)(t.GetField("TYPENUM").GetValue(null));
        }

        private static string getDescription(Type t)
        {
            return t.GetField("description").GetValue(null).ToString();
        }
    }

    public class BirthDateToAdmDate : BirthDateModifier
    {
        public readonly static int TYPENUM = 0;

        public readonly static string description = "入院月日を誕生日とする";

        public override string modify(string bd, string admd)
        {
            uint bdymd, admymd;
            if (UInt32.TryParse(bd, out bdymd))
            {
                if (UInt32.TryParse(admd, out admymd))
                {
                    if (bdymd % 10000 <= admymd % 10000)
                    {
                        bdymd = bdymd / 10000 * 10000 + (admymd % 10000);
                    }
                    else
                    {
                        bdymd = (bdymd / 10000 + 1) * 10000 + (admymd % 10000);
                    }
                    return bdymd.ToString();
                }
            }
            return bd;
        }

        public override string ToString()
        {
            return TYPENUM + ":" + description;
        }
    }

    public class BirthDateTo0101 : BirthDateModifier
    {
        public readonly static int TYPENUM = 1;

        public readonly static string description = "１月１日を誕生日とする";

        public override string modify(string bd, string based)
        {
            return bd.Substring(0, 4) + "0101";
        }

        public override string ToString()
        {
            return TYPENUM + ":" + description;
        }
    }

    public class BirthDateTo00101 : BirthDateModifier
    {
        public readonly static int TYPENUM = 2;

        public readonly static string description = "生年の下一桁を０年かつ１月１日を誕生日とする";

        public override string modify(string bd, string based)
        {
            return bd.Substring(0, 3) + "00101";
        }

        public override string ToString()
        {
            return TYPENUM + ":" + description;
        }
    }

    public abstract class PostalCodeModifier
    {
        private static List<Type> registeredSub_ = new List<Type>() { typeof(PostalCodeTo0000), typeof(PostalCodeTo0000000) };

        public abstract string modify(string pc);

        public static PostalCodeModifier TypedConstructor(int type)
        {
            Type subType = registeredSub_.SingleOrDefault(t => getTypeNum(t) == type);
            if (subType == null) subType = registeredSub_[0];
            return getInstance(subType);
        }

        public static string[] descriptions()
        {
            return registeredSub_.Select(type => getTypeNum(type) + ":" + getDescription(type)).ToArray();
        }

        public static int getTypeNum(string numdes)
        {
            int found = numdes.IndexOf(':');
            if (found == -1)
            {
                return -1;
            }
            string typestr = numdes.Substring(0, found);
            int typenum;
            if (!Int32.TryParse(typestr, out typenum))
            {
                return -1;
            }
            return typenum;
        }

        //reflection methods
        private static PostalCodeModifier getInstance(Type t)
        {
            System.Reflection.ConstructorInfo cnst = t.GetConstructor(new Type[0]);
            return (PostalCodeModifier)cnst.Invoke(new object[0]);
        }

        private static int getTypeNum(Type t)
        {
            return (int)(t.GetField("TYPENUM").GetValue(null));
        }

        private static string getDescription(Type t)
        {
            return t.GetField("description").GetValue(null).ToString();
        }
    }

    public class PostalCodeTo0000 : PostalCodeModifier
    {
        public readonly static int TYPENUM = 0;

        public readonly static string description = "下４桁を0にする(9999999除く)";

        public override string modify(string pc)
        {
            return pc == "9999999" ? pc : pc.Substring(0, 3) + "0000";
        }

        public override string ToString()
        {
            return TYPENUM + ":" + description;
        }
    }

    public class PostalCodeTo0000000 : PostalCodeModifier
    {
        public readonly static int TYPENUM = 1;

        public readonly static string description = "すべて不明とする";

        public override string modify(string pc)
        {
            return "0000000";
        }

        public override string ToString()
        {
            return TYPENUM + ":" + description;
        }
}
}
