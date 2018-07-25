﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IonDotnet.Internals.Binary
{
    internal class RawBinaryWriter : IPrivateWriter
    {
        private enum ContainerType
        {
            Sequence,
            Struct,
            Annotation,
            Datagram
        }

        private const int IntZeroByte = 0x20;

        //high-bits of different value types
        private const byte PosIntTypeByte = 0x20;
        private const byte NegIntTypeByte = 0x30;
        private const byte TidListByte = 0xB0;
        private const byte TidSexpByte = 0xC0;
        private const byte TidStructByte = 0xD0;
        private const byte TidTypeDeclByte = 0xE0;
        private const byte TidStringByte = 0x80;
        private const byte ClobType = 0x90;
        private const byte BlobByteType = 0xA0;

        private const byte NullNull = 0x0F;

        private const byte BoolFalseByte = 0x10;
        private const byte BoolTrueByte = 0x11;

        private const int DefaultContainerStackSize = 6;


        private readonly IWriterBuffer _lengthBuffer;
        private readonly IWriterBuffer _dataBuffer;
        private readonly List<SymbolToken> _annotations = new List<SymbolToken>();

        private SymbolToken _currentFieldSymbolToken;
        private readonly ContainerStack _containerStack;
        private readonly List<Memory<byte>> _lengthSegments;

        internal RawBinaryWriter(IWriterBuffer lengthBuffer, IWriterBuffer dataBuffer, List<Memory<byte>> lengthSegments)
        {
            _lengthBuffer = lengthBuffer;
            _dataBuffer = dataBuffer;
            _lengthSegments = lengthSegments;
            _containerStack = new ContainerStack(DefaultContainerStackSize);

            //top-level writing also requires a tracker
            var pushedContainer = _containerStack.PushContainer(ContainerType.Datagram);
            _dataBuffer.StartStreak(pushedContainer.Sequence);
        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void UpdateCurrentContainerLength(long increase)
//        {
//            if (_containerStack.Count == 0) return;
//            //pop and push, should be quick
//            var ( sequence, type, length) = _containerStack.Pop();
//            _containerStack.Push((sequence, type, length + increase));
//        }

        /// <summary>
        /// Prepare the field name and annotations (if any)
        /// </summary>
        /// <remarks>This method should implemented in a way that it can be called multiple times and still remains the correct state</remarks>
        private void PrepareValue()
        {
            if (IsInStruct && _currentFieldSymbolToken == default)
            {
                throw new InvalidOperationException("In a struct but field name is not set");
            }

            if (_currentFieldSymbolToken != default)
            {
                //write field name id
                WriteVarUint(_currentFieldSymbolToken.Sid);
                _currentFieldSymbolToken = default;
            }

            if (_annotations.Count > 0)
            {
                //Since annotations 'wraps' the actual value, we basically won't know the length 
                //(the upcoming value might be another container) 
                //so we treat this as another container of type 'annotation'

                //add all written segments to the sequence
                _dataBuffer.Wrapup();

                //set a new container
                var newContainer = _containerStack.PushContainer(ContainerType.Annotation);
                _dataBuffer.StartStreak(newContainer.Sequence);

                var annotLength = _dataBuffer.WriteAnnotationsWithLength(_annotations);
                _containerStack.UpdateCurrentContainerLength(annotLength);

                _annotations.Clear();
            }
        }

        /// <summary>
        /// This is called after the value is written, and will check if the written value is wrapped within annotations
        /// </summary>
        private void FinishValue()
        {
            if (_containerStack.Count > 0)
            {
                var containerInfo = _containerStack.Peek();
                if (containerInfo.Type == ContainerType.Annotation)
                {
                    PopContainer();
                }
            }
        }

        /// <summary>
        /// Pop a container from the container stack and link the previous container sequence with the length
        /// and sequence of the popped container
        /// </summary>
        private void PopContainer()
        {
            var popped = _containerStack.Pop();
            if (_containerStack.Count == 0) return;


            var wrappedList = _dataBuffer.Wrapup();
            Debug.Assert(ReferenceEquals(wrappedList, popped.Sequence));

            var outer = _containerStack.Peek();

            //write the tid|len byte and (maybe) the length into the length buffer
            _lengthBuffer.StartStreak(_lengthSegments);
            byte tidByte;
            switch (popped.Type)
            {
                case ContainerType.Sequence:
                    tidByte = TidListByte;
                    break;
                case ContainerType.Struct:
                    tidByte = TidStructByte;
                    break;
                case ContainerType.Annotation:
                    tidByte = TidTypeDeclByte;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var wholeContainerLength = popped.Length;
            if (wholeContainerLength <= 0xD)
            {
                //fit in the tid byte
                tidByte |= (byte) wholeContainerLength;
                _containerStack.UpdateCurrentContainerLength(1 + wholeContainerLength);
                _lengthBuffer.WriteByte(tidByte);
            }
            else
            {
                tidByte |= IonConstants.LnIsVarLen;
                _lengthBuffer.WriteByte(tidByte);
                var lengthBytes = _lengthBuffer.WriteVarUint(popped.Length);
                _containerStack.UpdateCurrentContainerLength(1 + lengthBytes + wholeContainerLength);
            }

            _lengthBuffer.Wrapup();
            outer.Sequence.AddRange(_lengthSegments);

            //clear the length segments, no worry
            _lengthSegments.Clear();

            outer.Sequence.AddRange(wrappedList);
            _dataBuffer.StartStreak(outer.Sequence);
        }

        private void WriteVarUint(long value)
        {
            Debug.Assert(value >= 0);
            var written = _dataBuffer.WriteVarUint(value);
            _containerStack.UpdateCurrentContainerLength(written);
        }

        //this is not supposed to be called ever
        public ISymbolTable SymbolTable => SharedSymbolTable.GetSystem(1);

        //Flush() and Finish() will never be called at this level, so just in case 
        //they're implemented explicitly

        void IIonWriter.Flush(Stream outputStream)
        {
        }

        void IIonWriter.Finish(Stream outputStream)
        {
        }

        /// <summary>
        /// Flush the content to this 
        /// </summary>
        /// <param name="outputStream">Write all contents here</param>
        internal void FlushAndFinish(Stream outputStream)
        {
            Debug.Assert(_containerStack.Count == 1);
            Debug.Assert(outputStream?.CanWrite == true);

            var currentSequence = _containerStack.Peek().Sequence;
            //wrapup to append all data to the sequence
            _dataBuffer.Wrapup();
            foreach (var segment in currentSequence)
            {
                outputStream.Write(segment.Span);
            }

            outputStream.Flush();

            //reset the states
            _containerStack.Clear();

            //top-level writing also requires a tracker
            var pushed = _containerStack.PushContainer(ContainerType.Datagram);
            _dataBuffer.StartStreak(pushed.Sequence);
            _dataBuffer.Reset();
            //double calls to Reset() should be fine
            _lengthBuffer.Reset();
            _lengthSegments.Clear();

            //TODO implement writing again after finish
        }

        public void SetFieldName(string name) => throw new NotSupportedException("Cannot set a field name here");

        public void SetFieldNameSymbol(SymbolToken name)
        {
            if (!IsInStruct) throw new IonException($"Has to be in a struct to set a field name");
            _currentFieldSymbolToken = name;
        }

        public void StepIn(IonType type)
        {
            if (!type.IsContainer()) throw new IonException($"Cannot step into {type}");

            PrepareValue();
            //wrapup the current writes

            if (_containerStack.Count > 0)
            {
                var writeList = _dataBuffer.Wrapup();
                Debug.Assert(ReferenceEquals(writeList, _containerStack.Peek().Sequence));
            }

            var pushedContainer = _containerStack.PushContainer(type == IonType.Struct ? ContainerType.Struct : ContainerType.Sequence);
            _dataBuffer.StartStreak(pushedContainer.Sequence);
        }

        public void StepOut()
        {
            if (_currentFieldSymbolToken != default) throw new IonException("Cannot step out with field name set");
            if (_annotations.Count > 0) throw new IonException("Cannot step out with annotations set");

            //TODO check if this container is actually list or struct
            var currentContainerType = _containerStack.Peek().Type;

            if (currentContainerType != ContainerType.Sequence && currentContainerType != ContainerType.Struct)
                throw new IonException($"Cannot step out of {currentContainerType}");

            PopContainer();
            //clear annotations
            FinishValue();
        }

        public bool IsInStruct => _containerStack.Count > 0 && _containerStack.Peek().Type == ContainerType.Struct;

        public void WriteValue(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteValues(IIonReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteNull()
        {
            PrepareValue();
            _containerStack.UpdateCurrentContainerLength(1);
            _dataBuffer.WriteByte(NullNull);
        }

        public void WriteNull(IonType type)
        {
            var nullByte = IonConstants.GetNullByte(type);
            PrepareValue();
            _containerStack.UpdateCurrentContainerLength(1);
            _dataBuffer.WriteByte(nullByte);
            FinishValue();
        }

        public void WriteBool(bool value)
        {
            PrepareValue();
            _containerStack.UpdateCurrentContainerLength(1);
            _dataBuffer.WriteByte(value ? BoolTrueByte : BoolFalseByte);
        }

        public void WriteInt(long value)
        {
            PrepareValue();
            if (value == 0)
            {
                _containerStack.UpdateCurrentContainerLength(1);
                _dataBuffer.WriteByte(IntZeroByte);
            }
            else if (value < 0)
            {
                if (value == long.MinValue)
                {
                    // XXX special case for min_value which will not play nice with signed
                    // arithmetic and fit into the positive space
                    // XXX we keep 2's complement of Long.MIN_VALUE because it encodes to unsigned 2
                    // ** 63 (0x8000000000000000L)
                    // XXX WriteBuffer.writeUInt64() never looks at sign
                    _dataBuffer.WriteByte(NegIntTypeByte | 0x8);
                    _dataBuffer.WriteUint64(value);
                    _containerStack.UpdateCurrentContainerLength(9);
                }
                else
                {
                    WriteTypedUInt(NegIntTypeByte, -value);
                }
            }
            else
            {
                WriteTypedUInt(PosIntTypeByte, value);
            }

            FinishValue();
        }

        private void WriteTypedUInt(byte type, long value)
        {
            if (value <= 0xFFL)
            {
                _containerStack.UpdateCurrentContainerLength(2);
                _dataBuffer.WriteUint8(type | 0x01);
                _dataBuffer.WriteUint8(value);
            }
            else if (value <= 0xFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(3);
                _dataBuffer.WriteUint8(type | 0x02);
                _dataBuffer.WriteUint16(value);
            }
            else if (value <= 0xFFFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(4);
                _dataBuffer.WriteUint8(type | 0x03);
                _dataBuffer.WriteUint24(value);
            }
            else if (value <= 0xFFFFFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(5);
                _dataBuffer.WriteUint8(type | 0x04);
                _dataBuffer.WriteUint32(value);
            }
            else if (value <= 0xFFFFFFFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(6);
                _dataBuffer.WriteUint8(type | 0x05);
                _dataBuffer.WriteUint40(value);
            }
            else if (value <= 0xFFFFFFFFFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(7);
                _dataBuffer.WriteUint8(type | 0x06);
                _dataBuffer.WriteUint48(value);
            }
            else if (value <= 0xFFFFFFFFFFFFFFL)
            {
                _containerStack.UpdateCurrentContainerLength(8);
                _dataBuffer.WriteUint8(type | 0x07);
                _dataBuffer.WriteUint56(value);
            }
            else
            {
                _containerStack.UpdateCurrentContainerLength(9);
                _dataBuffer.WriteUint8(type | 0x08);
                _dataBuffer.WriteUint64(value);
            }
        }

        public void WriteInt(BigInteger value)
        {
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                WriteInt((long) value);
                return;
            }

            PrepareValue();

            var type = PosIntTypeByte;
            if (value < 0)
            {
                type = NegIntTypeByte;
                value = BigInteger.Negate(value);
            }

            //TODO is this different than java, is there a no-alloc way?
            var buffer = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            WriteTypedBytes(type, buffer);

            FinishValue();
        }

        /// <summary>
        /// Write raw bytes with a type.
        /// </summary>
        /// <remarks>This does not do <see cref="PrepareValue"/></remarks> or <see cref="FinishValue"/>
        private void WriteTypedBytes(byte type, ReadOnlySpan<byte> data)
        {
            var totalLength = 1;
            if (data.Length < 0xD)
            {
                _dataBuffer.WriteUint8(type | (byte) data.Length);
            }
            else
            {
                _dataBuffer.WriteUint8(type | IonConstants.LnIsVarLen);
                totalLength += _dataBuffer.WriteVarUint(data.Length);
            }

            _containerStack.UpdateCurrentContainerLength(totalLength);
            _dataBuffer.WriteBytes(data);
        }

        public void WriteFloat(double value)
        {
            throw new NotImplementedException();
        }

        public void WriteDecimal(decimal value)
        {
            throw new NotImplementedException();
        }

        public void WriteTimestamp(DateTime value)
        {
            throw new NotImplementedException();
        }

        public void WriteSymbol(SymbolToken symbolToken)
        {
            throw new NotImplementedException();
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNull(IonType.String);
                return;
            }

            PrepareValue();
            //TODO what's the performance implication of this?
            var stringByteSize = Encoding.UTF8.GetByteCount(value);
            //since we know the length of the string upfront, we can just write the length right here
            var tidByte = TidStringByte;
            var totalSize = stringByteSize;
            if (stringByteSize <= 0x0D)
            {
                tidByte |= (byte) stringByteSize;
                _dataBuffer.WriteByte(tidByte);
                totalSize += 1;
            }
            else
            {
                tidByte |= IonConstants.LnIsVarLen;
                _dataBuffer.WriteByte(tidByte);
                totalSize += 1 + _dataBuffer.WriteVarUint(stringByteSize);
            }

            _dataBuffer.WriteUtf8(value.AsSpan(), stringByteSize);
            _containerStack.UpdateCurrentContainerLength(totalSize);

            FinishValue();
        }

        public void WriteBlob(ReadOnlySpan<byte> value)
        {
            if (value == null)
            {
                WriteNull(IonType.Blob);
                return;
            }

            PrepareValue();

            WriteTypedBytes(BlobByteType, value);

            FinishValue();
        }

        public void WriteClob(ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        public void SetTypeAnnotations(IEnumerable<string> annotations)
            => throw new NotSupportedException("raw writer does not support setting annotations as text");

        public void SetTypeAnnotationSymbols(IEnumerable<SymbolToken> annotations)
        {
            _annotations.Clear();

            foreach (var annotation in annotations)
            {
                _annotations.Add(annotation);
            }
        }

        internal void AddTypeAnnotationSymbol(SymbolToken annotation) => _annotations.Add(annotation);

        internal void ClearAnnotations() => _annotations.Clear();

        public void AddTypeAnnotation(string annotation) => throw new NotSupportedException("raw writer does not support adding annotations");

        public void Dispose()
        {
            // this class is supposed to be used a tool for another writer wrapper, which will take care of freeing the resources
            // so nothing to do here
        }

        public ICatalog Catalog => throw new NotSupportedException();

        public bool IsFieldNameSet() => _currentFieldSymbolToken != default;

        public int GetDepth() => _containerStack.Count - 1;

        public void WriteIonVersionMarker()
        {
            _dataBuffer.WriteUint32(0xE0_01_00_EA);
        }

        public bool IsStreamCopyOptimized()
        {
            throw new NotImplementedException();
        }

        internal IWriterBuffer GetLengthBuffer() => _lengthBuffer;
        internal IWriterBuffer GetDataBuffer() => _dataBuffer;

        private class ContainerInfo
        {
            public List<Memory<byte>> Sequence;
            public ContainerType Type;
            public long Length;
        }

        private class ContainerStack
        {
            private ContainerInfo[] _array;

            public ContainerStack(int initialCapacity)
            {
                _array = new ContainerInfo[initialCapacity];
            }

            public ContainerInfo PushContainer(ContainerType containerType)
            {
                EnsureCapacity(Count);
                if (_array[Count] == null)
                {
                    _array[Count] = new ContainerInfo {Sequence = new List<Memory<byte>>(4)};
                }
                else
                {
                    _array[Count].Sequence.Clear();
                }

                _array[Count].Length = 0;
                _array[Count].Type = containerType;
                return _array[Count++];
            }

            public void UpdateCurrentContainerLength(long increase)
            {
                _array[Count - 1].Length += increase;
            }

            public ContainerInfo Peek()
            {
                if (Count == 0) throw new IndexOutOfRangeException();
                return _array[Count - 1];
            }

            public ContainerInfo Pop()
            {
                if (Count == 0) throw new IndexOutOfRangeException();
                var ret = _array[--Count];
                return ret;
            }

            public void Clear()
            {
                //don't dispose of the lists
                Count = 0;
            }

            public int Count { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureCapacity(int forIndex)
            {
                if (forIndex < _array.Length) return;
                //resize
                var newArray = new ContainerInfo[_array.Length * 2];
                Buffer.BlockCopy(_array, 0, newArray, 0, _array.Length);
                _array = newArray;
            }
        }
    }
}