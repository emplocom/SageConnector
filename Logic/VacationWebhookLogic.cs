using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using EmploApiSDK.Logger;
using Forte.Common.Interfaces;
using Forte.Kadry.KDFAppServices;
using SageConnector.Models.EmploWebhookModels.RequestModels;
using Symfonia.Common.Defs;
using Symfonia.Common.UnitTest.Framework;

namespace SageConnector.Logic
{
    public class VacationWebhookLogic
    {
        private readonly ILogger _logger;
        private readonly SyncVacationDataLogic _syncVacationDataLogic;

        public VacationWebhookLogic(ILogger logger, SyncVacationDataLogic syncVacationDataLogic)
        {
            _logger = logger;
            _syncVacationDataLogic = syncVacationDataLogic;
        }

        public IResult SendVacationCreatedRequest(VacationCreatedWebhookModel emploRequest, out string newAbsenceIdentifier)
        {
            newAbsenceIdentifier = string.Empty;
            IKDFEmployee employee;
            var getEmployeeResult = FKDFEmployees.GetKDFEmployee(new CIdentifier(emploRequest.ExternalEmployeeId), out employee);
            if (!getEmployeeResult.Success) return getEmployeeResult;

            List<IEventType> eventTypes;
            var getEventTypesResult = FKDFAbsences.GetEventTypesValidForAbsenceUse(out eventTypes, employee);
            if (!getEventTypesResult.Success) return getEventTypesResult;

            IEventType eventType = eventTypes.FirstOrDefault(et =>
                et.BalanceType.Identifier.IntId == int.Parse(emploRequest.ExternalVacationTypeId));

            IAbsence createdAbsence;
            List<IBalanceEmployee> balancesForSync;
            var insertAbsenceResult = FKDFAbsences.InsertEmployeeAbsence(new Guid(), out createdAbsence, employee, eventType, EAbsenceStatus.toAccept, emploRequest.Since,
                emploRequest.Until, out balancesForSync);
            if (!insertAbsenceResult.Success) return insertAbsenceResult;

            newAbsenceIdentifier = createdAbsence.Identifier.GetExternalIdForEmplo();

            if(emploRequest.HasManagedVacationDaysBalance)
                Task.Run(() => _syncVacationDataLogic.SyncVacationDataForEmployeeFromBalance(balancesForSync));
            return insertAbsenceResult;
        }

        public IResult SendVacationEditedRequest(VacationEditedWebhookModel emploRequest)
        {
            throw new NotImplementedException();
        }

        public IResult SendVacationStatusChangedRequest(VacationStatusChangedWebhookModel emploRequest)
        {
            IResult changeAbsenceStateResult;
            List<IBalanceEmployee> balancesForSync;
            IAbsence inputAbsence;
            FKDFAbsences.GetAbsence(out inputAbsence, new CIdentifier(emploRequest.ExternalVacationId));

            if (new List<VacationStatusEnum>(){VacationStatusEnum.Canceled, VacationStatusEnum.Rejected, VacationStatusEnum.Removed}.Contains(emploRequest.NewStatus))
            {
                changeAbsenceStateResult =
                    FKDFAbsences.RemoveEmployeeAbsence(inputAbsence, out balancesForSync, DateTime.MinValue);
            }
            else
            {
                IAbsence outputAbsence;
                changeAbsenceStateResult = FKDFAbsences.ChangeAbsenceState(out outputAbsence, inputAbsence, EAbsenceStatus.toAccept, out balancesForSync);
            }
            
            if (emploRequest.HasManagedVacationDaysBalance)
                Task.Run(() => _syncVacationDataLogic.SyncVacationDataForEmployeeFromBalance(balancesForSync));
            return changeAbsenceStateResult;
        }
    }
}