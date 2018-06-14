using System;
using System.Configuration;
using System.Threading.Tasks;
using EmploApiSDK.Client;
using EmploApiSDK.Logger;
using Forte.Kadry.KDFAppServices;

namespace SageConnector.Logic
{
    public class ConfigurationTestLogic
    {
        private readonly ILogger _logger;

        public ConfigurationTestLogic(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> TestEmploConnection()
        {
            try
            {
                ApiConfiguration _apiConfiguration = new ApiConfiguration()
                {
                    EmploUrl = ConfigurationManager.AppSettings["EmploUrl"],
                    ApiPath = ConfigurationManager.AppSettings["ApiPath"] ?? "apiv2",
                    Login = ConfigurationManager.AppSettings["Login"],
                    Password = ConfigurationManager.AppSettings["Password"]
                };

                var apiClient = new ApiClient(_logger, _apiConfiguration);

                var response = await apiClient
                    .SendGetAsync<Object>(_apiConfiguration.CheckUserHasAccessUrl);

                return "Success!";
            }
            catch (Exception e)
            {
                return ExceptionLoggingUtils.ExceptionAsString(e);
            }
        }

        public string TestSageConnection()
        {
            try
            {
                var result = FKDFAppServices.CheckFirmOpened();

                if (result.Success)
                {
                    return "Success!";
                }

                return result.ToString();
            }
            catch (Exception e)
            {
                return ExceptionLoggingUtils.ExceptionAsString(e);
            }
        }
    }
}