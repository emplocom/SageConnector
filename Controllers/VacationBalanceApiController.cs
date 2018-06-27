using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.WebPages;
using EmploApiSDK.Logger;
using SageConnector.Logic;

namespace SageConnector.Controllers
{
    public class VacationBalanceApiController : ApiController
    {
        SyncVacationDataLogic _sageSyncVacationDataLogic;
       ILogger _logger;

        public VacationBalanceApiController()
        {
            _logger = LoggerFactory.CreateLogger(null);
            _sageSyncVacationDataLogic = new SyncVacationDataLogic(_logger);
        }

        /// <summary>
        /// Triggers vacations days balance synchronization between Sage and emplo for all employees.
        /// Vacation balance data is retrieved from Sage and passed to emplo's API.
        /// Should be run periodically by a scheduler.
        /// </summary>
        [HttpGet]
        public HttpResponseMessage SynchronizeVacationDays([FromUri] string listOfIds = "")
        {
            //SageServiceClient.EnsureConnectionOpen(_logger);

            if (listOfIds.IsEmpty())
            {
                _sageSyncVacationDataLogic.SyncVacationData();
            }
            else
            {
                _sageSyncVacationDataLogic.SyncVacationData(listOfIds.Split(',').ToList());
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
