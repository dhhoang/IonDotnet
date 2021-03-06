using IonDotnet.Internals;

namespace IonDotnet.Tree
{
    /// <inheritdoc />
    /// <summary>
    /// Represent a null.null value.
    /// </summary>
    public sealed class IonNull : IonValue
    {
        public IonNull() : base(true)
        {
        }

        internal override void WriteBodyTo(IPrivateWriter writer) => writer.WriteNull();

        public override IonType Type => IonType.Null;
    }
}
