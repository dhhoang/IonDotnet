using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using IonDotnet;
using IonDotnet.Internals.Text;
using IonDotnet.Json;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace IonDotnet.Tests.Json
{
    [TestClass]
    public class JTokenFromReaderTest
    {
        [TestMethod]
        public void TrivialStruct()
        {
            //empty struct {}
            var trivial = DirStructure.ReadDataFile("text/trivial.ion");
            var text = Encoding.UTF8.GetString(trivial);

            var reader = new UserTextReader(text);

            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;
            Assert.AreEqual(0, obj.Count);
        }

        /// <summary>
        /// Test for single-value bool 
        /// </summary>
        [TestMethod]
        [DataRow("true", true)]
        [DataRow("false", false)]
        public void SingleBool(string text, bool value)
        {
            var reader = new UserTextReader(text);
            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JValue));
            Assert.AreEqual(new JValue(value), token);
        }


        [TestMethod]
        [DataRow("1234567890", 1234567890)]
        [DataRow("4611686018427387903", long.MaxValue / 2)]
        public void SingleNumber(string text, long value)
        {
            var reader = new UserTextReader(text);
            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JValue));
            Assert.AreEqual(new JValue(value), token);
        }

        [TestMethod]
        public void OneBoolInStruct()
        {
            //simple datagram: {yolo:true}
            var oneBool = DirStructure.ReadDataFile("text/onebool.ion");
            var reader = new UserTextReader(new MemoryStream(oneBool));

            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;
            Assert.AreEqual(1, obj.Count);
            Assert.AreEqual("yolo", obj.Properties().Single().Name);
            Assert.AreEqual(new JValue(true), obj["yolo"]);
        }

        [TestMethod]
        public void FlatScalar()
        {
            //a flat struct of scalar values:
            //boolean:true
            //str:"yes"
            //integer:123456
            //longInt:int.Max*2
            //bigInt:long.Max*10
            //double:2213.1267567f
            var flatScalar = DirStructure.ReadDataFile("text/flat_scalar.ion");

            var reader = new UserTextReader(new MemoryStream(flatScalar));
            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;
            Assert.AreEqual(6, obj.Count);



            Assert.AreEqual(new JValue(true), obj["boolean"]);
            Assert.AreEqual(new JValue("yes"), obj["str"]);
            Assert.AreEqual(new JValue(123456), obj["integer"]);
            Assert.AreEqual(new JValue(int.MaxValue * 2L), obj["longInt"]);
            Assert.AreEqual(new JValue(new BigInteger(long.MaxValue) * 10), obj["bigInt"]);
            Assert.AreEqual(new JValue(2213.1267567), obj["double"]);
        }

        [TestMethod]
        public void FlatIntList()
        {
            //a flat list of ints [123,456,789]
            var flatListInt = DirStructure.ReadDataFile("text/flatlist_int.ion");

            var reader = new UserTextReader(new MemoryStream(flatListInt));
            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JArray));
            var obj = (JArray)token;
            Assert.AreEqual(3, obj.Count);
            Assert.AreEqual(new JValue(123), obj[0]);
            Assert.AreEqual(new JValue(456), obj[1]);
            Assert.AreEqual(new JValue(789), obj[2]);
        }

        //        [TestMethod]
        //        public void ReadAnnotations_SingleField()
        //        {
        //            // a singlefield structure with annotations
        //            // {withannot:years::months::days::hours::minutes::seconds::18}
        //            var annotSingleField = DirStructure.ReadDataFile("text/annot_singlefield.ion");
        //            var converter = new SaveAnnotationsReaderRoutine();
        //            var reader = new UserTextReader(new MemoryStream(annotSingleField), converter);
        //            ReaderTestCommon.ReadAnnotations_SingleField(reader, converter);
        //        }

        [TestMethod]
        public void SingleSymbol()
        {
            //struct with single symbol
            //{single_symbol:'something'}
            var data = DirStructure.ReadDataFile("text/single_symbol.ion");

            var reader = new UserTextReader(new MemoryStream(data));
            var token = reader.ToJToken();

            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;

            var val = (JValue)obj["single_symbol"];

            Assert.IsTrue(val.IsIonSymbol());
            Assert.AreEqual(new JValue("something"), val);

        }

        [TestMethod]
        public void SingleIntList()
        {
            var data = DirStructure.ReadDataFile("text/single_int_list.ion");
            var reader = new UserTextReader(new MemoryStream(data));

            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JArray));
            var obj = (JArray)token;
            Assert.AreEqual(5, obj.Count);

            Assert.AreEqual(new JValue(1234), obj[0]);
            Assert.AreEqual(new JValue(5678), obj[1]);
            Assert.AreEqual(new JValue(6421), obj[2]);
            Assert.AreEqual(new JValue(-2147483648), obj[3]);
            Assert.AreEqual(new JValue(2147483647), obj[4]);
        }

        /// <summary>
        /// Test for a typical json-style message
        /// </summary>
        [TestMethod]
        public void Combined1()
        {
            var data = DirStructure.ReadDataFile("text/combined1.ion");
            var reader = new UserTextReader(new MemoryStream(data));

            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;

            Assert.AreEqual(new JValue("file"), obj["menu"]["id"]);
            Assert.AreEqual(new JValue("Open"), obj["menu"]["popup"][0]);
            Assert.AreEqual(new JValue("Load"), obj["menu"]["popup"][1]);
            Assert.AreEqual(new JValue("Close"), obj["menu"]["popup"][2]);
            Assert.AreEqual(new JValue("enddeep"), obj["menu"]["deep1"]["deep2"]["deep3"]["deep4val"]);
            Assert.AreEqual(new JValue(1234), obj["menu"]["positions"][0]);
            Assert.AreEqual(new JValue(5678), obj["menu"]["positions"][1]);
            Assert.AreEqual(new JValue(90), obj["menu"]["positions"][2]);
        }

        [TestMethod]
        public void Struct_OneBlob()
        {
            var data = DirStructure.ReadDataFile("text/struct_oneblob.ion");
            var reader = new UserTextReader(new MemoryStream(data));

            var expected = Convert.FromBase64String("AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ==");

            var token = reader.ToJToken();
            Assert.IsInstanceOfType(token, typeof(JObject));
            var obj = (JObject)token;
            var val = (JValue)obj["blobbbb"];

            Assert.AreEqual(JTokenType.Bytes, val.Type);
            Assert.AreEqual(Convert.ToBase64String(expected), Convert.ToBase64String(val.Value<byte[]>()));
        }
    }
}
