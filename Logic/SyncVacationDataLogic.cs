using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using EmploApiSDK.Client;
using EmploApiSDK.Logger;

namespace SageConnector.Logic
{
    public class SyncVacationDataLogic
    {
        private readonly ILogger _logger;
        private readonly ApiClient _apiClient;

        readonly ApiConfiguration _apiConfiguration = new ApiConfiguration()
        {
            EmploUrl = ConfigurationManager.AppSettings["EmploUrl"],
            ApiPath = ConfigurationManager.AppSettings["ApiPath"] ?? "apiv2",
            Login = ConfigurationManager.AppSettings["Login"],
            Password = ConfigurationManager.AppSettings["Password"]
        };

        public SyncVacationDataLogic(ILogger logger)
        {
            _logger = logger;
            _apiClient = new ApiClient(_logger, _apiConfiguration);
        }

        public async Task SyncVacationData(List<string> employeeIdentifiers = null)
        {
            if (employeeIdentifiers == null || !employeeIdentifiers.Any())
            {
                //await SyncVacationData();
            }
            else
            {
                //await SyncVacationData(employeeIdentifiers);
            }
        }
    }
}