using System;

namespace IonDotnet.Internals.Lite
{
    internal sealed class IonFloatLite: IonValueLite, IIonFloat
    {
        private double _value;
        private static readonly int HASH_SIGNATURE = IonType.Float.ToString().GetHashCode();
        
        void ValidateThisNotNull()
        {
            if (this.IsNull)
            {
                throw new System.ArgumentException("");
            }
        }

        void CheckForLock()
        {
            if (this.ReadOnly)
            {
                throw new System.InvalidOperationException("");
            }
        }
       
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
                this.ValidateThisNotNull();
                return (float)_value;
            }
            // verify that this value is not readonly
            set
            {
                this.CheckForLock();
                _value = (double) value;
            }
            
        }

        public double DoubleValue
        {
            get
            {
                this.ValidateThisNotNull();
                return _value;
            }
            set
            {
                this.CheckForLock();
                _value = value;
            }
        }
        public bool IsNumeric {
            get { return !(this.IsNullValue() || Double.IsNaN(_value) || Double.IsInfinity(_value)); }
            
        }
    }
}
