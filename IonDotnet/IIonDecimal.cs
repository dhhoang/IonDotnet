namespace IonDotnet
{
    // An Ion Decimal Value
    public interface IIonDecimal : IIonValue<IIonDecimal>
    {
        decimal DecimalValue { get; set; }  
    }
}
