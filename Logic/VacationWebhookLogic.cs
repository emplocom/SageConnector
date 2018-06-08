using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EmploApiSDK.Logger;

namespace SageConnector.Logic
{
    public class VacationWebhookLogic
    {
        private readonly ILogger _logger;

        public VacationWebhookLogic(ILogger logger)
        {
            _logger = logger;
        }
    }
}