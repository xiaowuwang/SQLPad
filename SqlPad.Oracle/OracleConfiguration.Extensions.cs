﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SqlPad.Oracle
{
	public partial class OracleConfiguration
	{
		private static readonly XmlSerializer XmlSerializer = new XmlSerializer(typeof(OracleConfiguration));
		private static readonly string ConfigurationFilePath = Path.Combine(ConfigurationProvider.FolderNameApplication, "OracleConfiguration.xml");

		public static OracleConfiguration Configuration { get; }

		private IReadOnlyDictionary<string, OracleConfigurationConnection> _connectionConfigurations = new Dictionary<string, OracleConfigurationConnection>().AsReadOnly();

		public string GetRemoteTraceDirectory(string connectionName)
		{
			OracleConfigurationConnection configuration;
			return _connectionConfigurations.TryGetValue(connectionName, out configuration)
				? configuration.RemoteTraceDirectory
				: String.Empty;
		}

		public string GetConnectionStartupScript(string connectionName)
		{
			OracleConfigurationConnection configuration;
			return _connectionConfigurations.TryGetValue(connectionName, out configuration)
				? configuration.StartupScript
				: String.Empty;
		}

		public OracleConfigurationConnection GetConnectionConfiguration(string connectionName)
		{
			OracleConfigurationConnection configuration;
			if (_connectionConfigurations.TryGetValue(connectionName, out configuration) ||
				_connectionConfigurations.TryGetValue("*", out configuration))
			{
				return configuration;
			}

			throw new InvalidOperationException($"Connection '{connectionName}' configuration not found in '{ConfigurationFilePath}' file. ");
		}

		static OracleConfiguration()
		{
			Configuration =
				new OracleConfiguration
				{
					Connections = new OracleConfigurationConnection[0]
				};

			if (!File.Exists(ConfigurationFilePath))
			{
				return;
			}

			try
			{
				using (var reader = XmlReader.Create(ConfigurationFilePath))
				{
					Configuration = (OracleConfiguration)XmlSerializer.Deserialize(reader);
				}

				if (Configuration.Connections != null)
				{
					Configuration._connectionConfigurations = Configuration.Connections.ToDictionary(c => c.ConnectionName);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Configuration loading failed: " + e);
			}
		}
	}
}