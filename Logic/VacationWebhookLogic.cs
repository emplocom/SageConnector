using System;
using System.Collections.Generic;
using System.Linq;
using EmploApiSDK.ApiModels.Vacations.IntegratedVacationWebhooks.RequestModels;
using EmploApiSDK.Logger;
using Forte.Common.Interfaces;
using Forte.Kadry.KDFAppCOMServer;
using Forte.Kadry.KDFAppServices;
using mxKD;
using Symfonia.Common.Defs;

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

        public IResult SendVacationCreatedRequest(VacationCreatedWebhookModel emploRequest,
            out string newAbsenceIdentifier)
        {
            newAbsenceIdentifier = string.Empty;
            IKDFEmployee employee;
            var getEmployeeResult =
                FKDFEmployees.GetKDFEmployee(new CIdentifier(emploRequest.ExternalEmployeeId), out employee);
            _logger.WriteLine($"GetKDFEmployee result: {getEmployeeResult.ToString()}",
                getEmployeeResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!getEmployeeResult.Success) return getEmployeeResult;


            List<IEventType> eventTypes;
            var getEventTypesResult = FKDFAbsences.GetEventTypesValidForAbsenceUse(out eventTypes, employee);
            _logger.WriteLine($"GetEventTypesValidForAbsenceUse result: {getEventTypesResult.ToString()}",
                getEventTypesResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!getEventTypesResult.Success) return getEventTypesResult;


            IEventType eventType = eventTypes.FirstOrDefault(et => et.HasBalance &&
                                                                   et.BalanceType.Identifier.IntId.ToString()
                                                                       .Equals(emploRequest.ExternalVacationTypeId));
            if (eventType == null)
                throw new Exception(
                    $"Could not find event type with Id {emploRequest.ExternalVacationTypeId} for employee {emploRequest.ExternalVacationTypeId}");


            IAbsence createdAbsence;
            var guid = Guid.NewGuid();
            _logger.WriteLine(
                $"Calling InsertEmployeeAbsence with parameters: guid = {guid}, employee (id) = {employee.Identifier.ToString()}, eventType (id) = {eventType.Identifier.ToString()}, newStatus = {MapVacationStatus(emploRequest.Status)}, beginDate = {emploRequest.Since}, endDate = {emploRequest.Until}");

            var insertAbsenceResult = FKDFAbsences.InsertEmployeeAbsence(guid, out createdAbsence, employee, eventType,
                MapVacationStatus(emploRequest.Status), emploRequest.Since,
                emploRequest.Until);
            _logger.WriteLine($"InsertEmployeeAbsence result: {insertAbsenceResult.ToString()}",
                insertAbsenceResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!insertAbsenceResult.Success) return insertAbsenceResult;

            newAbsenceIdentifier = createdAbsence.Identifier.IntId.ToString();

            if (emploRequest.HasManagedVacationDaysBalance)
                _syncVacationDataLogic.SyncVacationData(new List<string> {employee.Identifier.IntId.ToString()});

            return insertAbsenceResult;
        }

        public IResult SendVacationEditedRequest(VacationEditedWebhookModel emploRequest)
        {
            using (var scope = new KDFAppCOMObjectScope())
            {
                MxKdPracownicy pracownicy = scope.KDFirm.Pracownicy;
                pracownicy.UstawWarunki($"IdPracownika={emploRequest.ExternalEmployeeId}");

                if (pracownicy.LiczbaPracownikow < 1)
                {
                    throw new Exception($"Nie znaleziono pracownika o Id {emploRequest.ExternalEmployeeId}");
                }

                MxKdPracownik pracownik = pracownicy.Pracownik[0];

                var zdarzenia = pracownik.Zdarzenia;
                zdarzenia.UstawWarunki($"IdZdarzenia={emploRequest.ExternalVacationId}");

                if (zdarzenia.LiczbaZdarzen < 1)
                {
                    throw new Exception($"Nie znaleziono zdarzenia urlopowego o Id {emploRequest.ExternalVacationId}");
                }

                MxKdZdarzenie zdarzenieUrlopowe = zdarzenia.Zdarzenie[0];

                if (zdarzenieUrlopowe.DataOd.Date != emploRequest.Since.Date ||
                    zdarzenieUrlopowe.DataDo.Date != emploRequest.Until.Date)
                {
                    zdarzenieUrlopowe.CzasOd = emploRequest.Since.Date;
                    zdarzenieUrlopowe.CzasDo = emploRequest.Until.Date;
                }

                if (zdarzenieUrlopowe.GodzinaOd.TimeOfDay != emploRequest.Since.TimeOfDay ||
                    zdarzenieUrlopowe.GodzinaDo.TimeOfDay != emploRequest.Until.TimeOfDay)
                {
                    zdarzenieUrlopowe.SetCzas(emploRequest.Since.Date, emploRequest.Since, emploRequest.Until,
                        MxKdGodziny.MxKdZAktualnegoDnia);
                }
            }

            return new CResult(String.Empty);
        }

        public IResult SendVacationStatusChangedRequest(VacationStatusChangedWebhookModel emploRequest)
        {
            IAbsence inputAbsence;
            var getAbsenceResult =
                FKDFAbsences.GetAbsence(out inputAbsence, new CIdentifier(emploRequest.ExternalVacationId));
            _logger.WriteLine($"GetAbsence result: {getAbsenceResult.ToString()}",
                getAbsenceResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!getAbsenceResult.Success) return getAbsenceResult;
            if (inputAbsence == null)
                throw new Exception($"Could not find absence with Id {emploRequest.ExternalVacationId}");


            IResult changeAbsenceStateResult;
            if (emploRequest.NewStatus == VacationStatusEnum.Removed)
            {
                changeAbsenceStateResult =
                    FKDFAbsences.RemoveEmployeeAbsence(inputAbsence);
            }
            else
            {
                IAbsence outputAbsence;
                changeAbsenceStateResult = FKDFAbsences.ChangeAbsenceState(out outputAbsence, inputAbsence,
                    MapVacationStatus(emploRequest.NewStatus));
            }

            if (!changeAbsenceStateResult.Success) return changeAbsenceStateResult;


            if (emploRequest.HasManagedVacationDaysBalance)
                _syncVacationDataLogic.SyncVacationData(new List<string>
                {
                    inputAbsence.Employee.Identifier.IntId.ToString()
                });

            return changeAbsenceStateResult;
        }

        private EAbsenceStatus MapVacationStatus(VacationStatusEnum emploVacationStatus)
        {
            switch (emploVacationStatus)
            {
                case VacationStatusEnum.Accepted:
                    return EAbsenceStatus.accepted;
                case VacationStatusEnum.Canceled:
                    return EAbsenceStatus.cancelled;
                case VacationStatusEnum.Executed:
                    return EAbsenceStatus.closed;
                case VacationStatusEnum.ForApproval:
                    return EAbsenceStatus.toAccept;
                case VacationStatusEnum.ForWithdrawal:
                    return EAbsenceStatus.toCancel;
                case VacationStatusEnum.Rejected:
                    return EAbsenceStatus.rejected;
                case VacationStatusEnum.Removed:
                    throw new Exception("This status should have been handled by separate removal logic!");
                default:
                    throw new Exception($"Unknown status: {emploVacationStatus.ToString()}");
            }
        }
    }
}