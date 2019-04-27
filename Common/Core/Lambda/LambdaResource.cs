using Microsoft.Extensions.Logging;

namespace DiscordBot.Common.Core.Lambda
{
    public abstract class LambdaResource
    {
        private readonly string _name;
        private ILogger _logger = null;
        protected ILogger Log => _logger ?? (_logger = Logger.GetLogger(_name));

        protected LambdaResource(string name)
        {
            _name = name;
        }
    }


}
