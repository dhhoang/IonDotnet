using System;
using IonDotnet.Systems;

namespace IonDotnet
{
    public interface IIonValue<out T> : IIonValue where T : IIonValue
    {
        /// <summary>
        /// Creates a copy of this value and all of its children.
        /// The cloned value may use the same shared symbol tables, but it will have an independent local
        /// symbol table if necessary. The cloned value will be modifiable regardless of whether this instance <see cref="IIonValue.ReadOnly"/>.
        ///
        /// The cloned value will be created in the context of the same
        /// <see cref="IValueFactory"/> as this instance; if you want a copy using a
        /// different factory, then use <see cref="IValueFactory.clone()"/> instead
        /// </summary>
        /// <returns>A copy of this value</returns>
        /// <exception cref="UnknownSymbolException" />
        T Clone();
    }

    // Base type for Ion Data Note
    public interface IIonValue
    {
        /// <summary>
        /// Gets an enumeration value identifying the core Ion data type of this object
        /// </summary>
        IonType Type { get; }

        /// <summary>
        /// Determines whether this object is a Null value
        /// </summary>
        /// <remarks>
        /// There are different Null values such as 'null' or 'null.string' or 'null.bool'
        /// </remarks>
        bool IsNull { get; }

        /// <summary>
        /// Determine whether this value is read-only
        /// </summary>
        /// <remarks>
        /// A read-only IonValue is thread-safe
        /// </remarks>
        bool ReadOnly { get; }

        /// <summary>
        /// The symbol table used to encode this value. Either a
        /// local or system symbol table (or null).
        /// </summary>
        ISymbolTable SymbolTable { get; }

        /// <summary>
        /// Field name attached to this value
        /// or null if this is not part of an <see cref="IIonStruct"/>.
        /// <exception cref="UnknownSymbolException">if the field name has unknown text.</exception>
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// The <see cref="T:IonDotnet.SymbolToken" /> attached to this value as an interned symbol (text + ID)
        /// </summary>
        SymbolToken FieldNameSymbol { get; }

        /// <summary>
        /// The container of this value or null if this is not part of one
        /// </summary>
        IIonContainer Container { get; }

        /// <summary>
        /// Removes this value from its container, if any.
        /// </summary>
        /// <returns>True if this value was in a container before remove</returns>
        bool RemoveFromContainer();

        /// <summary>
        /// The top level value above this value.
        /// If this value has no container, or if it's immediate container is a datagram, then this value is returned.
        /// </summary>
        /// <returns>the top level value above this value, never null, and never an <see cref="IIonDatagram"/></returns>
        /// <exception cref="NotSupportedException">if this is an <see cref="IIonDatagram"/></exception>
        IIonValue TopLevelValue { get; }
        
        /// <summary>
        /// Gets this value's user type annotations as text.
        /// </summary>
        /// <returns>the (ordered) annotations on the current value, or an empty array (not null) if there are none.</returns>
        /// <exception cref="UnknownSymbolException">if any annotation has unknown text</exception>
        string[] GetTypeAnnotations();
        
        /// <summary>
        /// Gets this value's user type annotations as interned symbols (text + ID).<see cref="T:IonDotnet.SymbolToken" />
        /// </summary>
        /// <returns>the (ordered) annotations on the current value, or an empty array (not null) if there are none.</returns>
        ArraySegment<SymbolToken> GetTypeAnnotationSymbols();
        
        /// <summary>
        /// Determines whether or not the value is annotated with a particular user type annotation.
        /// </summary>
        /// <param name="annotation">annotation as a string value</param>
        /// <returns>true if this value has the annotation</returns>
        bool HasTypeAnnotation(string annotation);
        
        /// <summary>
        /// Replaces all type annotations with the given text.
        /// </summary>
        /// <param name="annotations">
        /// Annotations the new annotations.  If null or empty array, then 
        /// all annotations are removed.  Any duplicates are preserved.
        /// </param>
        void SetTypeAnnotations(params string[] annotations);
        
        /// <summary>
        /// Replaces all type annotations with the given symbol tokens.
        /// The contents of the <param name="annotations"></param> array are copied into this
        /// writer, so the caller does not need to preserve the array.
        /// This is an "expert method": correct use requires deep understanding
        /// of the Ion binary format. You almost certainly don't want to use it.
        /// </summary>
        /// <param name="annotations">the new annotations</param>
        /// If null or empty array, then all annotations are removed.
        /// Any duplicates are preserved.
        void SetTypeAnnotationSymbols(params SymbolToken[] annotations);
        
        /// <summary>
        /// Adds a user type annotation to the annotations attached to
        /// this value. If the annotation exists the list does not change.
        /// </summary>
        void ClearTypeAnnotations();
        
        /// <summary>
        /// Adds a user type annotation to the annotations attached to
        /// this value. If the annotation exists the list does not change.
        /// </summary>
        /// <param name="annotation"></param>
        void AddTypeAnnotation(string annotation);
        
        /// <summary>
        /// Removes a user type annotation from the list of annotations attached to this value.
        /// </summary>
        /// <param name="annotation"></param>
        void RemoveTypeAnnotation(string annotation);
        
        /// <summary>
        /// Copies this value to the given <see cref="IIonWriter"/>.
        /// This method writes annotations and field names (if in a struct),
        /// and performs a deep write, including the contents of any containers encountered.
        /// </summary>
        /// <param name="writer"></param>
        void WriteTo(IIonWriter writer);
        
        /// <summary>
        /// Entry point for visitor pattern.  Implementations of this method by
        /// concrete classes will simply call the appropriate visit
        /// method on the <param name="visitor"></param>. For example, instances of
        /// <see cref="IIonBool"/> will invoke <see cref="IValueVisitor.visit(IonBool)"/>.
        /// </summary>
        /// <param name="visitor">will have one of its visit methods called.</param>
        /// <exception cref="NullReferenceException">if <param name="visitor"></param> is null.</exception>
        void Accept(IValueVisitor visitor);
        
        /// <summary>
        /// Marks this instance and its children to be immutable.
        /// In addition, read-only values are safe for simultaneous use
        /// from multiple threads.  This may require materializing the Java forms of the values.
        /// After this method completes, any attempt to change the state of this
        /// instance, or of any contained value, will trigger a <exception cref="InvalidOperationException"></exception>.
        /// </summary>
        void MakeReadOnly();
        
        /// <summary>
        /// System that constructed this value
        /// </summary>
        /// <return> not null</return>
        IIonSystem System { get; }

        /// <summary>
        /// Returns a pretty-printed Ion text representation of this value, using
        /// the settings of <see cref="IonTextWriterBuilder.pretty()"/> {@link IonTextWriterBuilder#pretty()}.
        /// </summary>
        /// <returns></returns>
        string ToPrettyString();
    }
}
