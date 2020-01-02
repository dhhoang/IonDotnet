using System;
using System.Collections.Generic;
using System.Text;
using IonDotnet;
using IonDotnet.Json;

namespace Newtonsoft.Json.Linq
{
    public static class JTokenExtensions
    {
        #region Writing

        public static void WriteAsIon(this JToken token, IIonWriter writer)
        {
            IonDotnet.Json.Convert.WriteJToken(token, writer);
        }

        #endregion


        #region Symbols

        /// <summary>
        /// Makes the value an Ion symbol. Should be used with string values only.
        /// </summary>
        /// <param name="value"></param>
        public static void ConvertToIonSymbol(this JValue value)
        {
            if (!IsIonSymbol(value))
                value.AddAnnotation(IonSymbolAnnotation.Default);
        }
        public static void ConvertFromIonSymbol(this JValue value)
        {
            value.RemoveAnnotations(typeof(IonSymbolAnnotation));
        }
        public static bool IsIonSymbol(this JValue value)
        {
            return value.Annotation(typeof(IonSymbolAnnotation)) != null;
        }

        #endregion


        #region Typed nulls

        /// <summary>
        /// Returns the null-type for the specified JValue if it is null,
        /// or IonType.None if the value is not null.
        /// </summary>
        /// <returns>The IonType null type.</returns>
        public static IonType GetIonNullType(this JValue value)
        {
            if (value.Value == null)
            {
                var ann = value.Annotation(typeof(IonTypeAnnotation)) as IonTypeAnnotation;
                return ann?.IonType ?? IonType.Null;
            }
            return IonType.None;
        }
        #endregion

        #region Annotations

        public static void SetIonAnnotation(this JToken token, string annotation)
        {
            RemoveIonAnnotations(token);
            AddIonAnnotation(token, annotation);
        }

        public static void AddIonAnnotation(this JToken token, string annotation)
        {
            token.AddAnnotation(new IonJTokenAnnotation(annotation));
        }

        public static void RemoveIonAnnotations(this JToken token)
        {
            token.RemoveAnnotations(typeof(IonJTokenAnnotation));
        }

        public static string[] GetIonAnnotations(this JToken token)
        {
            var l = new List<string>();
            foreach (IonJTokenAnnotation annotation in token.Annotations(typeof(IonJTokenAnnotation)))
                l.Add(annotation.Annotation);
            return l.ToArray();
        }

        #endregion

    }
}
