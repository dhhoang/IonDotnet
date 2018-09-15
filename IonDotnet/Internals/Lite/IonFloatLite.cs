namespace IonDotnet.Internals.Lite
{
    internal sealed class IonFloatLite: IonValueLite, IIonFloat
    {
        private float _float_value;
        private static readonly int HASH_SIGNATURE = IonType.Float.ToString().GetHashCode();

        
        public IonFloatLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {
        }

        public IonFloatLite(IonFloatLite existing, IContext context) : base(existing, context)
        {
            _float_value = existing._float_value;
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
            writer.WriteFloat(_float_value);
        }

        public IIonFloat Clone()
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type { get; }
        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public bool IsNumeric { get; }
    }
}