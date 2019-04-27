using System;

namespace DiscordBot.Common.Core.MEF
{
	public class SharedPolicyCreationAttribute : Attribute
	{
		public bool Shared { get; set; }

		public SharedPolicyCreationAttribute(bool shared)
		{
			Shared = shared;
		}
	}
}