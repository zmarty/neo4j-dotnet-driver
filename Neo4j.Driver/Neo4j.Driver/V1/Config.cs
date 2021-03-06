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

using Neo4j.Driver.Internal;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Control the level of encryption to require.
    /// </summary>
    public enum EncryptionLevel
    {
        /// <summary>
        /// No encryption at all.
        /// </summary>
        None,
        /// <summary>
        ///  With this level, the driver will only connect to the server without encryption if local but with encryption otherwise.
        /// </summary>
        EncryptedNonLocal,
        /// <summary>
        /// Always encrypted.
        /// </summary>
        Encrypted
    }

    /// <summary>
    /// Use this class to config the <see cref="IDriver"/>.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// When the <see cref="MaxIdleSessionPoolSize"/> is set to <see cref="InfiniteMaxIdleSessionPoolSize" />, the idle session pool will pool all sessions created by the driver.
        /// </summary>
        public const int InfiniteMaxIdleSessionPoolSize = -1;
        static Config()
        {
            DefaultConfig = new Config();
        }


        /// <summary>
        /// Returns the default configuration for the <see cref="IDriver"/>.
        /// </summary>
        /// <remarks>
        /// The defaults are <br/>
        /// <list type="bullet">
        /// <item><see cref="EncryptionLevel"/> : <c><see cref="EncryptionLevel"/> EncryptedNonLocal</c> </item>
        /// <item><see cref="TrustStrategy"/> : <c><see cref="V1.TrustStrategy.TrustOnFirstUse"/>TrustOnFirstUse</c> </item>
        /// <item><see cref="Logger"/> : <c>DebugLogger</c> at <c><see cref="LogLevel"/> Info</c> </item>
        /// <item><see cref="MaxIdleSessionPoolSize"/> : <c>10</c> </item>
        /// </list>
        /// </remarks>
        public static Config DefaultConfig { get; }

        /// <summary>
        /// Create an instance of <see cref="IConfigBuilder"/> to build a <see cref="Config"/>.
        /// </summary>
        public static IConfigBuilder Builder => new ConfigBuilder(new Config());

        /// <summary>
        /// Gets or sets the use of encryption for all the connections created by the <see cref="IDriver"/>.
        /// </summary>
        public EncryptionLevel EncryptionLevel { get; set; } = EncryptionLevel.EncryptedNonLocal;

        /// <summary>
        /// Gets or sets how to determine the authenticity of an encryption certificate provided by the Neo4j instance we are connecting to.
        /// </summary>
        public TrustStrategy TrustStrategy { get; set; } = TrustStrategy.TrustOnFirstUse();

        /// <summary>
        /// Gets or sets the <see cref="ILogger"/> instance to be used by the <see cref="ISession"/>s.
        /// </summary>
        public ILogger Logger { get; set; } = new DebugLogger {Level = LogLevel.Info};

        /// <summary>
        /// Gets or sets the max idle session pool size.
        /// </summary>
        /// <remarks> 
        /// The max idle session pool size represents the maximum number of sessions buffered by the driver. 
        /// A buffered <see cref="ISession"/> is a session that has already been connected to the database instance and doesn't need to re-initialize.
        /// </remarks>
        public int MaxIdleSessionPoolSize { get; set; } = 10;

        private class ConfigBuilder : IConfigBuilder
        {
            private readonly Config _config;

            internal ConfigBuilder(Config config)
            {
                _config = config;
            }

            public IConfigBuilder WithEncryptionLevel(EncryptionLevel level)
            {
                _config.EncryptionLevel = level;
                return this;
            }

            public IConfigBuilder WithTrustStrategy(TrustStrategy strategy)
            {
                _config.TrustStrategy = strategy;
                return this;
            }

            public IConfigBuilder WithLogger(ILogger logger)
            {
                _config.Logger = logger;
                return this;
            }

            public IConfigBuilder WithMaxIdleSessionPoolSize(int size)
            {
                _config.MaxIdleSessionPoolSize = size;
                return this;
            }

            public Config ToConfig()
            {
                return _config;
            }
        }
    }
    /// <summary>
    /// Provides a way to generate a <see cref="Config"/> instance fluently.
    /// </summary>
    public interface IConfigBuilder
    {
        /// <summary>
        /// Builds the <see cref="Config"/> instance based on the previously set values.
        /// </summary>
        /// <remarks>>
        /// If no value was set for a property the defaults specified in <see cref="Config.DefaultConfig"/> will be used.
        /// </remarks>
        /// <returns>A <see cref="Config"/> instance.</returns>
        Config ToConfig();
        
        /// <summary>
        /// Sets the <see cref="Config"/> to use TLS if <paramref name="level"/> is <c>true</c>.
        /// </summary>
        /// <param name="level"><see cref="EncryptionLevel.Encrypted"/> enables TLS for the connection, <see cref="EncryptionLevel.None"/> otherwise. See <see cref="EncryptionLevel"/> for more info</param>.
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithEncryptionLevel(EncryptionLevel level);

        /// <summary>
        /// Sets <see cref="Config"/> to use the verification <paramref name="strategy"/> when establishing TLS connections.
        /// The <paramref name="strategy"/> will not take effects if <see cref="Config.EncryptionLevel"/> decides to use no TLS encryption on the connections.
        /// </summary>
        /// <param name="strategy">See <see cref="TrustStrategy"/> for available strategies.</param>
        /// <returns></returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithTrustStrategy(TrustStrategy strategy);

        /// <summary>
        /// Sets the <see cref="Config"/> to use a given <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use, if <c>null</c> no logging will occur.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithLogger(ILogger logger);
        
        /// <summary>
        /// Sets the size of the idle session pool.
        /// </summary>
        /// <param name="size">The size of the <see cref="Config.MaxIdleSessionPoolSize"/>, set to <see cref="Config.InfiniteMaxIdleSessionPoolSize"/> to pool all sessions.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithMaxIdleSessionPoolSize(int size);
    }

    /// <summary>
    /// Strategy for how to trust encryption certificate.
    /// </summary>
    public class TrustStrategy
    {
        internal enum Strategy
        {
            TrustOnFirstUse,
            TrustSystemCaSignedCertificates
        }

        private readonly string _fileName;
        private readonly Strategy _strategy;

        private TrustStrategy(Strategy strategy, string fileName = null)
        {
            _strategy = strategy;
            _fileName = fileName;
        }

        internal string FileName()
        {
            return _fileName;
        }

        internal Strategy ServerTrustStrategy()
        {
            return _strategy;
        }

        /// <summary>
        /// Automatically trust a Neo4j instance the first time we see it - but fail to connect if its encryption certificate ever changes.
        /// This is similar to the mechanism used in SSH, and protects against man-in-the-middle attacks that occur after the initial setup of your application.
        /// </summary>
        /// <param name="knownHostsFileName">Optional. The file name where known certificates are stored. Default to be a file in the current user's folder</param>
        /// <returns>An <see cref="TrustStrategy"/> accepted by <see cref="Config.TrustStrategy"/></returns>
        public static TrustStrategy TrustOnFirstUse(string knownHostsFileName = null)
        {
            return new TrustStrategy(Strategy.TrustOnFirstUse, knownHostsFileName);
        }

        /// <summary>
        /// Only turst connections from the server whose certifact is trusted by the operation system
        /// </summary>
        /// <returns>An <see cref="TrustStrategy"/> accepted by <see cref="Config.TrustStrategy"/></returns>
        public static TrustStrategy TrustSystemCaSignedCertificates()
        {
            return new TrustStrategy(Strategy.TrustSystemCaSignedCertificates);
        }
    }
}