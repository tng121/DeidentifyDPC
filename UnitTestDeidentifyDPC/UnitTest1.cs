using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeidentifyDPC;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestDeidentifyDPC
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Transformation tr;

            tr = new LeftRotateTransformation(3);
            Assert.AreEqual((ulong)4567890123, tr.transform(1234567890));

            tr = new LinearTransformation(11, 3);
            Assert.AreEqual(3580246793, tr.transform(1234567890));

            Assert.IsTrue(ModularExponentiateTransformation.primeFactors(60).SequenceEqual(new List<ulong> { 2, 2, 3, 5 }));

            Assert.IsTrue(ModularExponentiateTransformation.primeFactors(1).SequenceEqual(new List<ulong> ()));

            Assert.IsTrue(ModularExponentiateTransformation.primeFactors(15966421).SequenceEqual(new List<ulong> { 15966421 }));

            Assert.IsNotNull(new ModularExponentiateTransformation(2, 2147483543, 269, 15966421, 5, 282013133, 17, 17, 17));

            Assert.IsNotNull(new LinearTransformation());
            Assert.IsNotNull(new LeftRotateTransformation());

            tr = Transformation.StringConstrucor("{lr:3}");
            Assert.AreEqual((ulong)4567890123, tr.transform(1234567890));

            tr = Transformation.StringConstrucor("{ln:b,3}");
            Assert.AreEqual(3580246793, tr.transform(1234567890));

            tr = Transformation.StringConstrucor("{me:2,1___-n,4d,YW3l,5,gPOTd,h,h,h}");
            Assert.IsNotNull(tr);
            Console.WriteLine("2147483543.To64 => " + Transformation.To64(2147483543));
            Console.WriteLine("269.To64 => " + Transformation.To64(269));
            Console.WriteLine("15966421.To64 => " + Transformation.To64(15966421));
            Console.WriteLine("282013133.To64 => " + Transformation.To64(282013133));
            Console.WriteLine("17.To64 => " + Transformation.To64(17));

            tr = new ModularExponentiateTransformation();
            Console.WriteLine(tr.ToString());
            Console.WriteLine("me(1234567890) => " + tr.transform(1234567890).ToString());
            Assert.IsNotNull(tr);

            IdEncryption enc = IdEncryption.StringConstructor("{ln:b,3}{lr:3}");
            Assert.AreEqual((ulong)246793358, enc.encrypt(1234567890));

            Assert.AreEqual("op" , Transformation.To64(24*64+25));

            Assert.AreEqual((ulong)24 * 64 + 26, Transformation.From64("oq"));

            bool thrown = false;
            try
            {
                Transformation.From64("あああ");
            }
            catch (ArgumentException)
            {
                thrown = true;
            }
            Assert.AreEqual(true, thrown);

            enc = new IdEncryption(new List<Transformation>() { new LinearTransformation(79565413, 1974568793), new LeftRotateTransformation(5) });

            Console.WriteLine("enc2: " + enc.ToString());
            Assert.IsNotNull(enc);

            BirthDateModifier bdmod = new BirthDateTo0101();
            Assert.AreEqual("20150101", bdmod.modify("20151201",null));
            Assert.AreEqual("20160101", bdmod.modify("20167890",null));

            bdmod = BirthDateModifier.TypedConstructor(0); //AdmDate
            Assert.AreEqual("19801201", bdmod.modify("19800503", "20151201"));
            Assert.AreEqual("19820503", bdmod.modify("19811201", "20150503"));
            Assert.AreEqual(getAge(19800503, 20151201), getAge(19801201, 20151201));
            Assert.AreEqual(getAge(19811201, 20150503), getAge(19820503, 20150503));
            Assert.AreEqual("19811201", bdmod.modify("19811201", "20151201"));

            PostalCodeModifier pcmod = new PostalCodeTo0000000();
            Assert.AreEqual("0000000", pcmod.modify("1234567"));
            Assert.AreEqual("0000000", pcmod.modify("9999999"));

        }


        private int getAge(int birthdate, int today)
        {
            return (today - birthdate) / 10000;
        }

    }
}
