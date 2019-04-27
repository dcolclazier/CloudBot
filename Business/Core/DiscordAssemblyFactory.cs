using DiscordBot.Common.Contract;
using DiscordBot.Common.Core.MEF;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace DiscordBot.Business.Core
{
	[Export(typeof(IAssemblyFactory))]
	[SharedPolicyCreation(false)]
	public class DiscordAssemblyFactory : IAssemblyFactory
	{
		//hard coded - for some reason, executing assembly is /var, even though dll's live in /var/task, and in MEF, the executing assembly is /var/task... who knows.
		private static string[] assemblies =
		{
			$"/var/task\\DiscordBot.Business.dll",
			$"/var/task\\DiscordBot.Common.dll",
			$"/var/task\\DiscordBot.Data.dll"
		};
		//todo - pull assemblies from s3 bucket for pluggability
		public IEnumerable<string> GetAssemblies()
		{
			return assemblies.ToList().AsReadOnly();
		}

		public DiscordAssemblyFactory() { }
	}



}
