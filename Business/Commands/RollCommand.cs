using Discord.Commands;
using DiscordBot.Business.Modules.Dice;
using DiscordBot.Data.Core;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using DiscordBot.Common.Core;
using DiscordBot.Common.Utils;

namespace DiscordBot.Business.Commands
{
	[DiscordCommand]
	public class RollCommand : ModuleBase<SocketCommandContext>
	{
		private ILogger Log = Logger.GetLogger(nameof(RollCommand));
		private Dictionary<string, string> nickNames { get; set; } = new Dictionary<string, string>
		{
			{"KeyserSoze", "Jacques"},
			{"Gen. Lee Bad", "Holgar" },
			{"K-Hop", "The DM" },
			{"ZSA", "Dragar" },
			{"Hopper", "Unuuth" },
			{"Noikkor", "Badric" },
		};


		[Command("roll")]
		[Alias("r")]
		[Summary("Roll some dice!")]
		public async Task RollAsync([Summary("e.g. 2d6+5")] string expression)
		{
			var key = nickNames.Keys.FirstOrDefault(k => Context.User.Username == k || Context.User.Username.Contains(k));
			var name = !key.IsNullOrEmpty()
				? nickNames[key]
				: Context.User.Username;
			var result = new Dice().Roll(expression);
			await ReplyAsync(result.Valid 
				? $"```css\n [{name}] rolled [{expression}] and got [{result.Result}]. {{{result.Arithmetic}}}\n```"
				: $"Huh?");

		}

		[Command("roll")]
		[Alias("r")]
		[Summary("Roll some dice!")]
		public async Task RollAsync([Summary("e.g. 2d6+5")] string expression, string reason)
		{

			var key = nickNames.Keys.FirstOrDefault(k => Context.User.Username == k || Context.User.Username.Contains(k));
			var name = !key.IsNullOrEmpty() 
				? nickNames[key] 
				: Context.User.Username;

			//await Context.Message.DeleteAsync(new Discord.RequestOptions
			//{
			//	AuditLogReason = "Deleting Message to reduce dice roll verbosity."
			//});

			Log.LogInformation($"Username that rolled: {Context.User.Username}, Expression: {expression}, Reason: {reason}");

			var result = new Dice().Roll(expression);
			await ReplyAsync(result.Valid
				? $"```css\n [{name}] rolled [{reason}] and got [{result.Result}]. {{{expression} => {result.Arithmetic}}}\n```"
				: $"Huh?");

		}
	}


}
