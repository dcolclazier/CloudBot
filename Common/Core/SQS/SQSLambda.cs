﻿using Amazon.Lambda.Core;
using Amazon.SQS;
using DiscordBot.Common.Core.Lambda;
using DiscordBot.Common.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Common.Core.SQS
{

	public class CFTLambda
	{
		[LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
		public async Task OnCFTEvent(CloudFormationRequest request, ILambdaContext context)
		{
			await new CFTLambdaProxy().OnCFTEvent(request, context);
		}
	}

	public class CFTLambdaProxy : LambdaResource
	{
		public CFTLambdaProxy() : base(nameof(CFTLambdaProxy)) { }

		private readonly Dictionary<CFTEvent, ICFTStrategy> CFTStrategies = new Dictionary<CFTEvent, ICFTStrategy>
		{
			{CFTEvent.Create, new CloudBotStackCreatedStrategy() }

		};

		public async Task OnCFTEvent(CloudFormationRequest request, ILambdaContext context)
		{
			var response = new CloudFormationResponse();
			if (!Enum.TryParse<CFTEvent>(request.RequestType, out var eventType))
			{
				Log.LogInformation($"Skipping CFT Event {request.RequestType} - no handler found.");
				await response.CompleteCloudFormationResponse(true, request, context);
			}
			else
			{
				var success = await CFTStrategies[eventType].ProcessMessageAsync(request, context);
				await response.CompleteCloudFormationResponse(success, request, context);
			}

			await Task.Run(() =>
			{
				Log.LogInformation($"CFT Event: {request}");
			});
		}

	}

	public class CloudFormationResponse : LambdaResource
	{
		public string Status { get; set; }
		public string Reason { get; set; }
		public string PhysicalResourceId { get; set; }
		public string StackId { get; set; }
		public string RequestId { get; set; }
		public string LogicalResourceId { get; set; }
		public object Data { get; set; }

		public CloudFormationResponse() : base("CloudFormationResponse")
		{
		}

		public async Task<CloudFormationResponse> CompleteCloudFormationResponse(bool success, CloudFormationRequest request, ILambdaContext context)
		{
			Log.LogInformation("Generating cloud formation response...");
			var responseBody = new CloudFormationResponse
			{
				Status = success ? "SUCCESS" : "FAILED",
				Reason = "See the details in CloudWatch Log Stream: " + context.LogStreamName,
				PhysicalResourceId = request.StackId,
				RequestId = request.RequestId,
				LogicalResourceId = request.LogicalResourceId,
				Data = null
			};
			Log.LogInformation("Sending response...");
			try
			{
				var client = new HttpClient();
				var json = new StringContent(JsonConvert.SerializeObject(responseBody));
				json.Headers.Remove("Content-Type");

				var postResponse = await client.PutAsync(request.ResponseURL, json);
				var content = await postResponse.Content.ReadAsStringAsync();

				Log.LogInformation("Response: " + postResponse.ToJsonString() + content);
				postResponse.EnsureSuccessStatusCode();
			}
			catch (Exception ex)
			{
				Log.LogError($"ERROR: {ex.ToJsonString()}");
				responseBody.Status = "FAILED";
				responseBody.Data = ex;
			}
			return responseBody;
		}
	}

	public interface IAmazonSQSFactory
	{
		IAmazonSQS GetSQS();
	}

	public interface ICFTStrategy
	{

		Task<bool> ProcessMessageAsync(CloudFormationRequest message, ILambdaContext context);


	}

	public enum CFTEvent
	{
		Create,
		Update,
		Delete
	}
	public abstract class CFTStrategyBase : LambdaResource, ICFTStrategy
	{
		protected CFTStrategyBase(string name) : base(name)
		{
		}

		public abstract Task<bool> ProcessMessageAsync(CloudFormationRequest message, ILambdaContext context);

	}
	public class CloudBotStackCreatedStrategy : CFTStrategyBase
	{
		public CloudBotStackCreatedStrategy() : base(nameof(CloudBotStackCreatedStrategy)) { }

		public override async Task<bool> ProcessMessageAsync(CloudFormationRequest message, ILambdaContext context)
		{
			Log.LogInformation("Starting StackCreated strategy");


			return await Task.Run(() => true);
			//send sqs to start lambda.
		}
	}
	public class CloudFormationRequest
	{
		public string StackId { get; set; }
		public string ResponseURL { get; set; }
		public string RequestType { get; set; }
		public string ResourceType { get; set; }
		public string RequestId { get; set; }
		public string LogicalResourceId { get; set; }
		public Dictionary<string, string> ResourceProperties { get; set; }

	}
}
