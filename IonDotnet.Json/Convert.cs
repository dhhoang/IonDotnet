using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Newtonsoft.Json.Linq;

namespace IonDotnet.Json
{
    internal static class Convert
    {
        #region Convert to JSON
        public static JToken ToJToken(IonType type, IIonReader reader)
        {
            if (reader.CurrentIsNull)
            {
                var val = JValue.CreateNull();
                if (type != IonType.Null)
                {
                    val.AddAnnotation(new IonTypeAnnotation(type));
                }
                return val;
            }
            switch (type)
            {
                case IonType.Struct:
                    return StructToJObject(reader);
                case IonType.List:
                    return ListToJArray(reader);
                case IonType.Bool:
                    return new JValue(reader.BoolValue());
                case IonType.Int:
                    switch (reader.GetIntegerSize())
                    {
                        case IntegerSize.Int:
                            return new JValue(reader.IntValue());
                        case IntegerSize.Long:
                            return new JValue(reader.LongValue());
                        case IntegerSize.BigInteger:
                            return new JValue(reader.BigIntegerValue());
                        default:
                            throw new InvalidDataException("Cannot map integer of size " + reader.GetIntegerSize() + " to JSON");
                    }
                case IonType.Float:
                    return new JValue(reader.DoubleValue());
                case IonType.Blob:
                case IonType.Clob:
                    return new JValue(reader.NewByteArray());
                case IonType.Decimal:
                    return new JValue(reader.DecimalValue());
                case IonType.String:
                    return new JValue(reader.StringValue());
                case IonType.Timestamp:
                    return new JValue(reader.TimestampValue().DateTimeValue);
                case IonType.Symbol:
                    var val = new JValue(reader.StringValue());
                    val.ConvertToIonSymbol();
                    return val;
            }
            throw new InvalidDataException("Cannot map type " + type + " to JSON");
        }

        private static JObject StructToJObject(IIonReader reader)
        {
            var obj = new JObject();
            reader.StepIn();
            for (var type = reader.MoveNext(); type != IonType.None; type = reader.MoveNext())
            {
                obj[reader.CurrentFieldName] = ToJToken(type, reader);
            }
            reader.StepOut();
            return obj;
        }

        private static JArray ListToJArray(IIonReader reader)
        {
            var obj = new JArray();
            reader.StepIn();
            for (var type = reader.MoveNext(); type != IonType.None; type = reader.MoveNext())
            {
                obj.Add(ToJToken(type, reader));
            }
            reader.StepOut();
            return obj;
        }
        #endregion

        #region Convert from JSON

        public static void WriteJToken(JToken token, IIonWriter writer)
        {
            if (token is JValue val)
            {
                WriteJValue(val, writer);
            }
            else if (token is JObject obj)
            {

            }
            else if (token is JArray arr)
            {

            }
            else
            {
                throw new ArgumentException("Unsupported JToken type " + token.Type);
            }
        }

        private static void WriteJValue(JValue val, IIonWriter writer)
        {
            switch (val.Type)
            {
                case JTokenType.Null:
                    var nullType = val.GetIonNullType();
                    if (nullType != IonType.Null && nullType != IonType.None)
                    {
                        writer.WriteNull(nullType);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                    break;
                case JTokenType.String:
                    writer.WriteString(val.Value<string>());
                    break;
                case JTokenType.Integer:
                    if (val.Value is BigInteger bi)
                    {
                        writer.WriteInt(bi);
                    }
                    else if (val.Value is long l)
                    {
                        writer.WriteInt(l);
                    }
                    else
                    {
                        writer.WriteInt(val.Value<int>());
                    }
                    break;
                case JTokenType.Boolean:
                    writer.WriteBool(val.Value<bool>());
                    break;
                case JTokenType.Bytes:
                    writer.WriteBlob(val.Value<byte[]>());
                    break;
                case JTokenType.Float:
                    if (val.Value is decimal d)
                    {
                        writer.WriteDecimal(d);
                    }
                    else
                    {
                        writer.WriteFloat(val.Value<double>());
                    }
                    break;
                case JTokenType.Date:
                    if (val.Value is DateTimeOffset dto)
                    {
                        writer.WriteTimestamp(new Timestamp(dto));
                    }
                    else
                    {
                        writer.WriteTimestamp(new Timestamp(val.Value<DateTime>()));
                    }
                    break;
                case JTokenType.Guid:
                    writer.WriteString(val.Value<Guid>().ToString());
                    break;
                case JTokenType.Uri:
                    writer.WriteString(val.Value<Uri>().ToString());
                    break;
                case JTokenType.TimeSpan:
                    writer.WriteString(val.Value<TimeSpan>().ToString("g", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new ArgumentException("Unsupported JValue type " + val.Type);
            }
        }

        #endregion
    }
}
