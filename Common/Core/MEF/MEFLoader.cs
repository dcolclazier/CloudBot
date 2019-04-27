using DiscordBot.Common.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DiscordBot.Common.Core.MEF
{
	public static class MEFLoader
	{
		private static CompositionHost _compositionHost;
		private static Dictionary<string, object> _mocked = new Dictionary<string, object>();
		private static readonly string _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static readonly ILogger _logger = Logger.GetLogger("MEFLoader");
		private static string[] assemblies =
		{
			$"{_assemblyPath}\\DiscordBot.Business.dll",
			$"{_assemblyPath}\\DiscordBot.Common.dll",
			$"{_assemblyPath}\\DiscordBot.Data.dll"
		};
		private static readonly object _sync = new object { };

		public static bool IsInitialized
		{
			get
			{
				lock (_sync)
				{
					return _compositionHost != null;
				}
			}
		}

		public static void Dispose()
		{
			lock (_sync)
			{
				if (!IsInitialized) return;

				_compositionHost.Dispose();
				_mocked = new Dictionary<string, object>();
			}
		}


		public static void Init()
		{
			try
			{
				lock (_sync)
				{
					if (IsInitialized)
					{
						Dispose();
					}

					_logger.LogInformation("MEFLoader.Init start");
					var startTime = DateTime.UtcNow;
					var rules = new ConventionBuilder();
					var assemblyList = new List<Assembly>();

					if (!MEFSkip.Skip)
					{
						assemblies.ToList().ForEach(a =>
						{
							var assembly = Assembly.LoadFile(a);
							assemblyList.Add(assembly);

							var sharedExports = assembly.GetTypes()
							   .Where(type => type.GetCustomAttribute<SharedPolicyCreationAttribute>(true) is SharedPolicyCreationAttribute policy && policy.Shared).ToList();
							foreach (var item in sharedExports)
							{
								rules.ForTypesDerivedFrom(item).Shared();
							}

							var debug = assembly.GetTypes().Where(t => t.GetCustomAttribute<ExportAttribute>(true) is ExportAttribute export);
							foreach (var t in debug)
							{
								_logger.LogInformation(t.FullName);
							}
							_logger.LogInformation($"Added types from {a} to the assembly list.");
						});
					}
					_compositionHost = new ContainerConfiguration().WithAssemblies(assemblyList, rules).CreateContainer();

					_logger.LogInformation($"MEF Init Time (in ms): {DateTime.UtcNow - startTime}");
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				ex.LoaderExceptions.ToList().ForEach(e =>
				{
					_logger.LogError($"---: {e.Message}");
					e.InnerException.ActIfNotNull(() => _logger.LogError(e.InnerException.ToJsonString()));
				});
			}
		}

		public static void SatisfyImportsOnce(object obj)
		{

			lock (_sync)
			{
				if (!IsInitialized) Init();
				obj.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
						.Where(prop => Attribute.IsDefined(prop, typeof(ImportAttribute))).ToList()
						.ForEach(prop =>
						{
							try
							{
								prop.SetValue(obj, _mocked.ContainsKey(prop.PropertyType.ToString())
									? _mocked[prop.PropertyType.ToString()]
									: _compositionHost.GetExport(prop.PropertyType));
							}
							catch (Exception e)
							{
								_logger.LogCritical($"Couldn't import MEF property! {e.ToJsonString()}");
							}

						});
			}
		}

		public static void ComposeExportedValue<T>(object obj)
		{

			lock (_sync)
			{
				if (!IsInitialized) Init();
				_mocked[typeof(T).ToString()] = obj;
			}
		}

	}


}
