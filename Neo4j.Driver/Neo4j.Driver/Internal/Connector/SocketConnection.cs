﻿// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Connector
{
    internal class SocketConnection : IConnection
    {
        private readonly ISocketClient _client;
        private readonly IMessageResponseHandler _responseHandler;

        private readonly Queue<IRequestMessage> _messages = new Queue<IRequestMessage>();
        internal IReadOnlyList<IRequestMessage> Messages => _messages.ToList();

        private volatile bool _interrupted;
        private readonly object _syncLock = new object();

        public SocketConnection(ISocketClient socketClient, IAuthToken authToken, ILogger logger,
            IMessageResponseHandler messageResponseHandler = null)
        {
            Throw.ArgumentNullException.IfNull(socketClient, nameof(socketClient));
            _responseHandler = messageResponseHandler ?? new MessageResponseHandler(logger);

            _client = socketClient;
            Task.Run(() => _client.Start()).Wait();

            // add init requestMessage by default
            Init(authToken);
        }

        private void Init(IAuthToken authToken)
        {
            var initCollector = new InitCollector();
            Enqueue(new InitMessage("neo4j-dotnet/1.1", authToken.AsDictionary()), initCollector);
            Sync();
            Server = initCollector.Server;
        }

        public SocketConnection(Uri uri, IAuthToken authToken, EncryptionManager encryptionManager, ILogger logger)
            : this(new SocketClient(uri, encryptionManager, logger), authToken, logger)
        {
        }

        public void Sync()
        {
            Send();
            Receive();
        }

        public void Send()
        {
            lock (_syncLock)
            {
                EnsureNotInterrupted();
                if (_messages.Count == 0)
                {
                    // nothing to send
                    return;
                }
                // blocking to send
                _client.Send(_messages);
                _messages.Clear();
            }
        }

        private void Receive()
        {
            if (_responseHandler.UnhandledMessageSize == 0)
            {
                // nothing to receive
                return;
            }

            // blocking to receive
            _client.Receive(_responseHandler);
            AssertNoServerFailure();
        }

        public void ReceiveOne()
        {
            _client.ReceiveOne(_responseHandler);
            AssertNoServerFailure();
        }

        public void Run(string statement, IDictionary<string, object> paramters = null, IMessageResponseCollector resultBuilder = null, bool pullAll = false)
        {
            if (pullAll)
            {
                Enqueue(new RunMessage(statement, paramters), resultBuilder, new PullAllMessage());
            }
            else
            {
                Enqueue(new RunMessage(statement, paramters), resultBuilder, new DiscardAllMessage());
            }
        }

        public void Reset()
        {
            Enqueue(new ResetMessage());
        }

        public void ResetAsync()
        {
            lock (_syncLock)
            {
                if (!_interrupted)
                {
                    Enqueue(new ResetMessage(), new ResetCollector(() => { _interrupted = false; }));
                    Send();
                    _interrupted = true;
                }
            }
        }

        public bool IsOpen => _client.IsOpen;
        public bool HasUnrecoverableError { private set; get; }
        public bool IsHealthy => IsOpen && !HasUnrecoverableError;
        public string Server { private set; get; }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            Task.Run(() => _client.Stop()).Wait();
        }

        private void AssertNoServerFailure()
        {
            if (_responseHandler.HasError)
            {
                if (IsRecoverableError(_responseHandler.Error))
                {
                    if (!_interrupted)
                    {
                        Enqueue(new AckFailureMessage());
                    }
                }
                else
                {
                    HasUnrecoverableError = true;
                }
                var error = _responseHandler.Error;
                _responseHandler.Error = null;
                _interrupted = false;
                throw error;
            }
        }

        private bool IsRecoverableError(Neo4jException error)
        {
            return error is ClientException || error is TransientException;
        }

        private void Enqueue(IRequestMessage requestMessage, IMessageResponseCollector resultBuilder = null, IRequestMessage requestStreamingMessage = null)
        {
            lock (_syncLock)
            {
                EnsureNotInterrupted();
                _messages.Enqueue(requestMessage);
                _responseHandler.EnqueueMessage(requestMessage, resultBuilder);

                if (requestStreamingMessage != null)
                {
                    _messages.Enqueue(requestStreamingMessage);
                    _responseHandler.EnqueueMessage(requestStreamingMessage, resultBuilder);
                }
            }
        }

        private void EnsureNotInterrupted()
        {
            if (_interrupted)
            {
                try
                {
                    while (_responseHandler.UnhandledMessageSize > 0)
                    {
                        ReceiveOne();
                    }
                }
                catch (Neo4jException e)
                {
                    throw new ClientException(
                        "An error has occurred due to the cancellation of executing a previous statement. " +
                        "You received this error probably because you did not consume the result immediately after " +
                        "running the statement which get reset in this session.", e);
                }
            }
        }
    }
}