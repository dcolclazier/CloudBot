using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using DiscordBot.Common.Core.Lambda;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Timers;
using DiscordBot.Common.Core.MEF;
using DiscordBot.Data.Contract;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Business.Lambda
{
	public class DiscordBot : LambdaResource
	{

		[Import]
		private IConnectionService _connectionService { get; set; } = null;


		//todo - IOC?
		private const string Token = "NTU5NDcwOTkxNjc2NDA3OTE1.XKKoKA.ny2SJLDLQC3KeQj2cGeKXG4zzZk";

		public DiscordBot() : base("TestLambda")
		{
			MEFLoader.SatisfyImportsOnce(this);

		}

		[LambdaSerializer(typeof(JsonSerializer))]
		public async Task RunAsync(ILambdaContext context)
		{
			var waitHandle = new AutoResetEvent(false);
			await _connectionService.InitializeAsync(Client_Ready, Token, 884000, waitHandle);

			waitHandle.WaitOne();

			await _connectionService.DisconnectAsync();

		}

		

		
		public async Task Client_Ready()
		{
			await _connectionService.Client.SetGameAsync("Serverless Bot Config");
		}


	}



}
