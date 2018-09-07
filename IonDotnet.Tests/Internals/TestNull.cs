using System.Text;
using IonDotnet.Internals.Lite;
using IonDotnet.Internals.Text;
using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Internals
{
    [TestClass]
    public class TestNull
    {
        [TestMethod]
        public void TestAllNulls()
        {
            byte[] values = DirStructure.IonTestFileAsBytes("good/allNulls.ion");
            string str = Encoding.UTF8.GetString(values);
            
            var reader = new UserTextReader(str);
            reader.MoveNext();
            Assert.AreEqual(IonType.List,reader.CurrentType);
            reader.StepIn();

            while (reader.MoveNext() != IonType.None)
            {
                Assert.IsTrue(reader.CurrentIsNull);
            }
        }
        
        [TestMethod]
        public void TestNonNulls()
        {
            byte[] values = DirStructure.IonTestFileAsBytes("good/nonNulls.ion");
            string str = Encoding.UTF8.GetString(values);
            
            var reader = new UserTextReader(str);
            reader.MoveNext();
            while (reader.MoveNext() != IonType.None)
            {
                Assert.IsFalse(reader.CurrentIsNull);
            }
            
        }
    }
}