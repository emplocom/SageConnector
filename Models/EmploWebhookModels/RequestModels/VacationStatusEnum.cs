namespace SageConnector.Models.EmploWebhookModels.RequestModels
{
    /// <summary>
    /// Vacation requests statuses in emplo
    /// </summary>
    public enum VacationStatusEnum
    {
        /// <summary>
        /// Request is waiting for acceptance
        /// </summary>
        ForApproval = 1,

        /// <summary>
        /// Request is accepted
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// Request is rejected
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// Request is processed by HR after acceptance
        /// </summary>
        Executed = 4,

        /// <summary>
        /// Request was canceled
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// Request was removed 
        /// </summary>
        Removed = 6,

        /// <summary>
        /// Employee asked to cancel the request
        /// </summary>
        ForWithdrawal = 7
    }
}
