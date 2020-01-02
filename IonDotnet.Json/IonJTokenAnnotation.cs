using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace IonDotnet.Json
{
    internal sealed class IonJTokenAnnotation
    {
        internal IonJTokenAnnotation(string annotation)
        {
            Annotation = annotation;
        }

        public string Annotation { get; }
    }
}
