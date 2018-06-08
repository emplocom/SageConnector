using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.WebPages;
using EmploApiSDK.Logger;
using SageConnector.Logic;

namespace SageConnector.Controllers
{
    public class VacationBalanceApiController : ApiController
    {
        SyncVacationDataLogic _sageSyncVacationDataLogic;

        public VacationBalanceApiController()
        {
            ILogger logger = LoggerFactory.CreateLogger(null);
            _sageSyncVacationDataLogic = new SyncVacationDataLogic(logger);
        }

        /// <summary>
        /// Triggers vacations days balance synchronization between Sage and emplo for all employees.
        /// Vacation balance data is retrieved from Sage and passed to emplo's API.
        /// Should be run periodically by a scheduler.
        /// </summary>
        [HttpGet]
        public async Task<HttpResponseMessage> SynchronizeVacationDays([FromUri] string listOfIds = "")
        {
            if (listOfIds.IsEmpty())
            {
                await _sageSyncVacationDataLogic.SyncVacationData();
            }
            else
            {
                await _sageSyncVacationDataLogic.SyncVacationData(listOfIds.Split(',').ToList());
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
