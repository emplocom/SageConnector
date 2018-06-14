using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using EmploApiSDK.ApiModels.Vacations.IntegratedVacationWebhooks.RequestModels;
using EmploApiSDK.ApiModels.Vacations.IntegratedVacationWebhooks.ResponseModels;
using EmploApiSDK.Logger;
using Newtonsoft.Json;
using SageConnector.Logic;
using Symfonia.Common.Defs;

namespace SageConnector.Controllers
{
    /// <summary>
    /// Actions from this controller should be registered as webhook endpoints in emplo
    /// </summary>
    public class VacationRequestsApiController : ApiController
    {
        VacationWebhookLogic _vacationWebhookLogic;
        SyncVacationDataLogic _syncVacationDataLogic;
        ILogger _logger;

        public VacationRequestsApiController()
        {
            _logger = LoggerFactory.CreateLogger(null);

            _syncVacationDataLogic = new SyncVacationDataLogic(_logger);
            _vacationWebhookLogic = new VacationWebhookLogic(_logger, _syncVacationDataLogic);
            
        }

        /// <summary>
        /// Endpoint listening for vacation creation event in emplo
        /// </summary>
        [HttpPost]
        public HttpResponseMessage VacationCreated([FromBody] VacationCreatedWebhookModel model)
        {
            _logger.WriteLine($"Webhook received: VacationCreated, {JsonConvert.SerializeObject(model)}");

            try
            {
                string newAbsenceIdentifier;
                var result = _vacationWebhookLogic.SendVacationCreatedRequest(model, out newAbsenceIdentifier);
                HttpResponseMessage response;

                if (result.Success)
                {
                    response = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(
                            JsonConvert.SerializeObject(
                                new CreatedObjectResponseEmploModel() { CreatedObjectIdentifier = newAbsenceIdentifier }),
                            Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    response = BuildErrorResponseFromIResult(result);
                }

                _logger.WriteLine($"Webhook VacationCreated response: {JsonConvert.SerializeObject(response)}");
                return response;
            }
            catch (Exception e)
            {
                throw new HttpResponseException(BuildErrorResponseFromException(e));
            }
        }

        /// <summary>
        /// Endpoint listening for vacation update event in emplo
        /// </summary>
        [HttpPost]
        public HttpResponseMessage VacationUpdated([FromBody] VacationEditedWebhookModel model)
        {
            _logger.WriteLine($"Webhook received: VacationUpdated, {JsonConvert.SerializeObject(model)}");

            try
            {
                var result = _vacationWebhookLogic.SendVacationEditedRequest(model);
                HttpResponseMessage response;

                if (result.Success)
                {
                    response =
                        new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    response = BuildErrorResponseFromIResult(result);
                    
                }

                _logger.WriteLine($"Webhook VacationUpdated response: {JsonConvert.SerializeObject(response)}");
                return response;
            }
            catch (Exception e)
            {
                throw new HttpResponseException(BuildErrorResponseFromException(e));
            }
        }

        /// <summary>
        /// Endpoint listening for vacation status change event in emplo
        /// </summary>
        [HttpPost]
        public HttpResponseMessage VacationStatusChanged([FromBody] VacationStatusChangedWebhookModel model)
        {
            _logger.WriteLine($"Webhook received: VacationStatusChanged, {JsonConvert.SerializeObject(model)}");

            try
            {
                var result = _vacationWebhookLogic.SendVacationStatusChangedRequest(model);
                HttpResponseMessage response;

                if (result.Success)
                {
                    response =
                        new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    response = BuildErrorResponseFromIResult(result);

                }

                _logger.WriteLine($"Webhook VacationStatusChanged response: {JsonConvert.SerializeObject(response)}", result.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
                return response;
            }
            catch (Exception e)
            {
                throw new HttpResponseException(BuildErrorResponseFromException(e));
            }
        }

        [NonAction]
        private HttpResponseMessage BuildErrorResponseFromException(Exception e)
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(
                        new ErrorMessageResponseEmploModel() { ErrorMessage = ExceptionLoggingUtils.ExceptionAsString(e) }), Encoding.UTF8,
                    "application/json")
            };

            _logger.WriteLine($"Status check result: ERROR, response: {JsonConvert.SerializeObject(response)}", LogLevelEnum.Error);
            return response;
        }

        [NonAction]
        private HttpResponseMessage BuildErrorResponseFromIResult(IResult result)
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(
                        new ErrorMessageResponseEmploModel() { ErrorMessage = result.ToString() }), Encoding.UTF8,
                    "application/json")
            };

            return response;
        }
    }
}
