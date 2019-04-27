using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Common.Contract;
using DiscordBot.Common.Core;
using DiscordBot.Common.Core.MEF;
using DiscordBot.Common.Utils;
using DiscordBot.Data.Contract;
using DiscordBot.Data.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace DiscordBot.Business.Core
{
	[Export(typeof(IConnectionService))]
	[SharedPolicyCreation(true)]
	public class DiscordConnectionService : IConnectionService
	{
		public DiscordSocketClient Client { get; private set; }
		public CommandService Commands { get; private set; }
		public ServiceProvider Services { get; private set; }

		private ILogger Log = Logger.GetLogger("DiscordConnectionService");
		private Timer _timer;
		private EventWaitHandle _waitHandle;

		[Import] private IAssemblyFactory _assemblyFactory { get; set; } = null;
		public bool PendingDisconnect { get; private set; }

		public DiscordConnectionService()
		{
			MEFLoader.SatisfyImportsOnce(this);

			Client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Debug
			});
			Client.Log += async (message) => await Task.Run(() => Log.LogInformation($"LOG: {message.ToJsonString()}"));

			Commands = new CommandService(new CommandServiceConfig
			{
				CaseSensitiveCommands = true,
				DefaultRunMode = RunMode.Async,
				LogLevel = LogSeverity.Debug
			});
		}

		public async Task InitializeAsync(Func<Task> OnReady, string token, int timeToRun, EventWaitHandle waitHandle)
		{

			_waitHandle = waitHandle;
			Log.LogInformation("InitializeAsync");

			StartTimer(timeToRun);

			await RemoveAllCommandsAsync();

			var serviceCollection = new ServiceCollection().AddSingleton(Client).AddSingleton(Commands);
			Client.MessageReceived += OnMessageReceived;
			foreach (var ass in _assemblyFactory.GetAssemblies().ToList())
			{
				var assembly = Assembly.LoadFile(ass);
				var innerServiceCollection = new ServiceCollection().AddSingleton(Client).AddSingleton(Commands);
				assembly.GetTypes()
				   .Where(t => t.GetCustomAttribute<DiscordCommandAttribute>(true) is DiscordCommandAttribute).ToList()
				   .ForEach(s =>
				   {
					   Log.LogInformation($"Found Command Class: {s.Name}");
					   innerServiceCollection = innerServiceCollection.AddSingleton(s);
					   serviceCollection = serviceCollection.AddSingleton(s);
				   });

				await Commands.AddModulesAsync(assembly, innerServiceCollection.BuildServiceProvider());
			}
			Client.Ready += OnReady;
			Services = serviceCollection.BuildServiceProvider();

			await Client.LoginAsync(TokenType.Bot, token);
			await Client.StartAsync();
		}
		public async Task RemoveAllCommandsAsync()
		{
			
			foreach (var assembly in _assemblyFactory.GetAssemblies().ToList())
			{
				var matches = Assembly.LoadFile(assembly).GetTypes().Where(t => t.GetCustomAttribute<DiscordCommandAttribute>(true) is DiscordCommandAttribute).ToList();
				foreach (var match in matches)
				{
					try
					{
						await Commands.RemoveModuleAsync(match.GetType());
					}
					catch (Exception ex)
					{
						//suppress, command may not exist
						Log.LogError(ex.ToJsonString());

					}
				}
			}
		}

		public async Task DisconnectAsync()
		{
			Log.LogInformation("DisconnectAsync");
			await RemoveAllCommandsAsync();
			Client.MessageReceived -= OnMessageReceived;
			await Client.StopAsync();
			await Client.LogoutAsync();
			await Task.Delay(1000);
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_waitHandle?.Set();
			PendingDisconnect = true;
			_timer?.Dispose();
			_timer = null;
		}

		

		private void StartTimer(int timeToRun)
		{
			_timer = new Timer(timeToRun);
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();
		}
		private async Task OnMessageReceived(SocketMessage arg)
		{
			var message = arg as SocketUserMessage;

			if (message is null || message.Author.IsBot) return;

			int argPos = 0;
			Log.LogInformation($"Message: {message.Author}: {message.Content}");
			if (message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos))
			{
				var context = new SocketCommandContext(Client, message);
				var result = await Commands.ExecuteAsync(context, argPos, Services);

				if (!result.IsSuccess)
				{
					Log.LogInformation("Error executing command: " + result.ErrorReason);
				}

			}
		}
	}

}
