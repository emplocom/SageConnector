using System;

namespace SageConnector.Models.EmploWebhookModels.RequestModels
{
    public class VacationEditedWebhookModel
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
        /// Vacation start date
        /// </summary>
        public DateTime Since { get; set; }

        /// <summary>
        /// Vacation end date
        /// </summary>
        public DateTime Until { get; set; }

        /// <summary>
        /// Number of days used by this vacation request (after subtracting all free days from 
        /// assigned free days calendar id present)
        /// </summary>
        public decimal Duration { get; set; }

        /// <summary>
        /// How many hours requested if it not a full day leave
        /// </summary>
        public decimal? AbsenceHours { get; set; }

        /// <summary>
        /// Description for accepting manager / HR
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Public description visible for all employees
        /// </summary>
        public string PublicInformation { get; set; }
    }
}
