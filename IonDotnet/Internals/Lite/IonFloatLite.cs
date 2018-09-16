using System;

namespace IonDotnet.Internals.Lite
{
    internal sealed class IonFloatLite: IonValueLite, IIonFloat
    {
        private double _value;
        private static readonly int HASH_SIGNATURE = IonType.Float.ToString().GetHashCode();
       
        public IonFloatLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonFloatLite(IonFloatLite existing, IContext context) : base(existing, context)
        {
            _value = existing._value;
        }

        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            throw new System.NotImplementedException();
        }

        public override IonValueLite Clone(IContext parentContext)
        {
            return new IonFloatLite(this, parentContext);
        }

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            writer.WriteFloat(_value);
        }

        public IIonFloat Clone()
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type => IonType.Float;

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public float FloatValue
        {
            get
            {
                ValidateThisNotNull();
                return (float)_value;
            }
            set
            {
                CheckForLock();
                _value = (double) value;
            }
            
        }

        public double DoubleValue
        {
            get
            {
                ValidateThisNotNull();
                return _value;
            }
            set
            {
                CheckForLock();
                _value = value;
            }
        }
        public bool IsNumeric => !(IsNullValue() || Double.IsNaN(_value) || Double.IsInfinity(_value));
    }
}
