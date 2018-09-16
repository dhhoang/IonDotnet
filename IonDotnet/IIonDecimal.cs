namespace IonDotnet
{
    // An Ion Decimal Value
    public interface IIonDecimal : IIonValue<IIonDecimal>
    {
        decimal DecimalValue { get; set; }  
        float FloatValue { get; set; }
        double DoubleValue { get; set; }
    }
}
