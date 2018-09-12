namespace IonDotnet.Internals.Lite
{
    internal sealed class IonDecimalLite: IonValueLite, IIonDecimal
    {

        private decimal _decimal_value;
        private static readonly int HASH_SIGNATURE = IonType.Decimal.ToString().GetHashCode();
        
        public IonDecimalLite(ContainerlessContext containerlessContext, bool isNull) : base(containerlessContext, isNull)
        {    
        }

        public IonDecimalLite(IonDecimalLite existing, IContext context) : base(existing, context)
        {
            _decimal_value = existing._decimal_value;
        }
        
        protected override int GetHashCode(ISymbolTableProvider symbolTableProvider)
        {
            throw new System.NotImplementedException();          
        }

        public override IonValueLite Clone(IContext parentContext)
        {
            return new IonDecimalLite(this, parentContext);
        }

        protected override void WriteBodyTo(IIonWriter writer, ISymbolTableProvider symbolTableProvider)
        {
            writer.WriteDecimal(_decimal_value);
        }

        public IIonDecimal Clone()
        {
            throw new System.NotImplementedException();
        }

        public override IonType Type { get; }
        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public decimal DecimalValue { get; set; }
    }
}