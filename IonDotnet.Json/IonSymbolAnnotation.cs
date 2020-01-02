using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace IonDotnet.Json
{
    internal sealed class IonSymbolAnnotation
    {
        private IonSymbolAnnotation() { }
        public static readonly IonSymbolAnnotation Default = new IonSymbolAnnotation();
    }
}
