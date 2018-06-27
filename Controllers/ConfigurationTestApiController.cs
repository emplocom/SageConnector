using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using EmploApiSDK.Logger;
using SageConnector.Logic;

namespace SageConnector.Controllers
{
    public class ConfigurationTestApiController : ApiController
    {
        private ConfigurationTestLogic _configurationTestLogic;
        private ILogger _logger;

        public ConfigurationTestApiController()
        {
            _logger = LoggerFactory.CreateLogger(null);
            _configurationTestLogic = new ConfigurationTestLogic(_logger);
        }

        /// <summary>
        /// Enables testing of Connector's configuration by sending test requests to the Sage and emplo APIs.
        /// </summary>
        [HttpGet]
        public HttpResponseMessage TestConnection()
        {

            var emploResult = Task.Run(() => _configurationTestLogic.TestEmploConnection()).GetAwaiter().GetResult();
            var sageResult = _configurationTestLogic.TestSageConnection();

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"Emplo API connection test: {emploResult}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Sage connection test: {sageResult}") };
        }
    }
}