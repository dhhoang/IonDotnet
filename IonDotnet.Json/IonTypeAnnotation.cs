using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace IonDotnet.Json
{
    internal sealed class IonTypeAnnotation
    {
        internal IonTypeAnnotation(IonType type)
        {
            IonType = type;
        }

        public IonType IonType { get; }
    }
}
