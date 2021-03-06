// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    internal class SocketClientTestHarness : IDisposable
    {
        public SocketClient Client { get; }
        public Mock<Stream> MockWriteStream { get; }
        public Mock<ITcpSocketClient> MockTcpSocketClient { get; }
        string _received = string.Empty;

        public SocketClientTestHarness(Uri uri=null, EncryptionManager encryptionManager = null)
        {
            MockTcpSocketClient = new Mock<ITcpSocketClient>();
            MockWriteStream = TestHelper.TcpSocketClientSetup.CreateWriteStreamMock(MockTcpSocketClient);
            Client = new SocketClient(uri, encryptionManager?? new Mock<EncryptionManager>().Object, MockTcpSocketClient.Object);
               
        }

        public async Task ExpectException<T>(Func<Task> func, string errorMessage=null) where T : Exception
        {
            var exception = await Record.ExceptionAsync(() => func());
            Assert.NotNull(exception);
            Assert.IsType<T>(exception);
            if (errorMessage != null)
            {
                exception.Message.Should().Contain(errorMessage);
            }
        }

        public void SetupReadStream(string hexBytes)
        {
            SetupReadStream(hexBytes.ToByteArray());
        }

        public void ResetCalls()
        {
            MockTcpSocketClient.ResetCalls();
            MockWriteStream.ResetCalls();
        }

        public void SetupWriteStream()
        {
            MockWriteStream
                .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Callback<byte[], int, int>((buffer, start, size) => _received = $"{buffer.ToHexString(start, size)}");
        }

        public void VerifyWriteStreamUsages(int count)
        {
            MockTcpSocketClient.Verify(c => c.WriteStream, Times.Exactly(2));
        }

        public void VerifyWriteStreamContent(byte[] expectedBytes, int expectedLength)
        {
            MockWriteStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                $"Received {_received}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, expectedLength)}");
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Client.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SetupReadStream(byte[] bytes)
        {
            TestHelper.TcpSocketClientSetup.SetupClientReadStream(MockTcpSocketClient, bytes);
        }
    }
}