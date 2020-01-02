using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IonDotnet
{
    public static class IonReaderExtensions
    {
        public static JToken ToJToken(this IIonReader reader)
        {
            return Json.Convert.ToJToken(reader.MoveNext(), reader);
        }
    }
}
