using System;
using System.Text;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Bufferize;

namespace Tests
{
    [TestFixture]
    public class BufferBuilderTests
    {
        private BufferBuilder _bufferBuilder;

        private BufferBuilderHandlerMock _handler;

        [SetUp]
        public void Init()
        {
            _handler = new BufferBuilderHandlerMock();
            _bufferBuilder = new BufferBuilder(_handler, 12, "\n");
        }

        [Test]
        public void Add()
        {
            Assert.AreEqual(12, _bufferBuilder.Capacity);
            _bufferBuilder.Add(CreateSerializedMetric('1', 3));
            _bufferBuilder.Add(CreateSerializedMetric('2', 3));
            _bufferBuilder.Add(CreateSerializedMetric('3', 3));
            Assert.Null(_handler.Buffer);
            _bufferBuilder.Add(CreateSerializedMetric('4', 3));
            Assert.AreEqual(4, _bufferBuilder.Length);
            Assert.AreEqual("111\n222\n333\n", Encoding.UTF8.GetString(_handler.Buffer));
        }

        [Test]
        public void HandleBufferAndReset()
        {
            Assert.Less(4, _bufferBuilder.Capacity);
            _bufferBuilder.Add(CreateSerializedMetric('1', 2));
            _bufferBuilder.HandleBufferAndReset();
            Assert.AreEqual("11\n", Encoding.UTF8.GetString(_handler.Buffer));

            _bufferBuilder.Add(CreateSerializedMetric('3', 4));
            _bufferBuilder.HandleBufferAndReset();
            Assert.AreEqual("3333\n", Encoding.UTF8.GetString(_handler.Buffer));
        }

        [Test]
        public void AddReturnedValue()
        {
            _bufferBuilder.Add(CreateSerializedMetric('1', _bufferBuilder.Capacity - 1)); // -1 for separator
            Assert.AreEqual(_bufferBuilder.Capacity, _bufferBuilder.Length);
        }

        private static SerializedMetric CreateSerializedMetric(char c, int count)
        {
            var serializedMetric = new SerializedMetric();
            serializedMetric.Builder.Append(new string(c, count));

            return serializedMetric;
        }
    }
}