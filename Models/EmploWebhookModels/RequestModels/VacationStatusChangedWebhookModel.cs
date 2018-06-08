namespace SageConnector.Models.EmploWebhookModels.RequestModels
{
    public class VacationStatusChangedWebhookModel
    {
        /// <summary>
        /// Employee identifier used by integrated system
        /// </summary>
        public string ExternalEmployeeId { get; set; }

        /// <summary>
        /// External id of request which was updated
        /// </summary>
        public string ExternalVacationId { get; set; }

        /// <summary>
        /// Vacation type identifier used by external system
        /// </summary>
        public string ExternalVacationTypeId { get; set; }

        /// <summary>
        /// Is there an externally managed vacation days balance for this type, 
        /// that will be synchronized and displayed in Emplo?
        /// </summary>
        public bool HasManagedVacationDaysBalance { get; set; }

        /// <summary>
        /// External id of employee who updated the request
        /// </summary>
        public string ChangingExternalEmployeeId { get; set; }

        /// <summary>
        /// New status of the request
        /// </summary>
        public VacationStatusEnum NewStatus { get; set; }
    }
}
