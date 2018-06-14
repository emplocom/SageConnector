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

        public ConfigurationTestApiController()
        {
            ILogger logger = LoggerFactory.CreateLogger(null);
            _configurationTestLogic = new ConfigurationTestLogic(logger);
        }

        /// <summary>
        /// Enables testing of Connector's configuration by sending test requests to the Sage and emplo APIs.
        /// </summary>
        [HttpGet]
        public async Task<HttpResponseMessage> TestConnection()
        {
            var emploResult = await _configurationTestLogic.TestEmploConnection();
            var sageResult = _configurationTestLogic.TestSageConnection();

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"Emplo API connection test: {emploResult}{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Sage connection test: {sageResult}") };
        }
    }
}