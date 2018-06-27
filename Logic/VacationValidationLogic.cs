using System;
using System.Collections.Generic;
using System.Linq;
using EmploApiSDK.ApiModels.Vacations.IntegratedVacationValidation;
using EmploApiSDK.Logger;
using Forte.Common.Interfaces;
using Forte.Kadry.KDFAppCOMServer;
using Forte.Kadry.KDFAppServices;
using mxKD;
using Symfonia.Common.Defs;

namespace SageConnector.Logic
{
    public class VacationValidationLogic
    {
        ILogger _logger;

        public VacationValidationLogic(ILogger logger)
        {
            _logger = logger;
        }

        public VacationValidationResponseModel ValidateVacationRequest(VacationValidationRequestModel emploRequest)
        {
            var responseModel = new VacationValidationResponseModel();


            #region FKDFAppComServer
            List<TimeSpan> pelnaListaCzasowPracy = new List<TimeSpan>();

            using (var scope = new KDFAppCOMObjectScope())
            {
                MxKdPracownicy pracownicy = scope.KDFirm.Pracownicy;
                pracownicy.UstawWarunki($"IdPracownika={emploRequest.ExternalEmployeeId}");

                if (pracownicy.LiczbaPracownikow < 1)
                {
                    responseModel.Message = $"Nie odnaleziono pracownika o Id {emploRequest.ExternalEmployeeId}";
                    return responseModel;
                }

                MxKdKalendarz kalendarz = pracownicy.Pracownik[0].Kalendarz;

                if (kalendarz == null)
                {
                    responseModel.Message = $"Nie odnaleziono kalendarza dla pracownika {emploRequest.ExternalEmployeeId}";
                    return responseModel;
                }

                kalendarz.UstawDaty(emploRequest.Since, emploRequest.Until);

                foreach (var emploDay in emploRequest.Since.EachDayTo(emploRequest.Until))
                {
                    MxKdDzien dzien = kalendarz.Dzien[emploDay];

                    if (dzien == null)
                    {
                        responseModel.Message = $"Nie odnaleziono obiektu dnia {emploDay.ToShortDateString()} w kalendarzu dla pracownika {emploRequest.ExternalEmployeeId}";
                        return responseModel;
                    }

                    MxKdHarmonogramPracy harmonogram = dzien.HarmonogramPracy;

                    if (harmonogram == null)
                    {
                        responseModel.Message = $"Nie odnaleziono harmonogramu dla dnia {emploDay.ToShortDateString()} dla pracownika {emploRequest.ExternalEmployeeId}";
                        return responseModel;
                    }

                    List<MxKdCzasPracy> listaCzasowPracyPerDzien = new List<MxKdCzasPracy>();

                    for (int i = 0; i < harmonogram.IloscPozycji; i++)
                    {
                        listaCzasowPracyPerDzien.Add(harmonogram.CzasPracy[i]);
                    }

                    pelnaListaCzasowPracy.AddRange(listaCzasowPracyPerDzien.Select(cp => cp.Koniec - cp.Poczatek).ToList());

                    string dayDescription;

                    if (dzien.JestRoboczy)
                    {
                        dayDescription = $"{dzien.WzorzecDznia.Nazwa}, godziny: {string.Join(",", listaCzasowPracyPerDzien.Select(c => $"{c.Poczatek.TimeOfDay} - {c.Koniec.TimeOfDay}"))}";
                    }
                    else
                    {
                        dayDescription = dzien.WzorzecDznia.Nazwa;
                    }

                    responseModel.AdditionalMessagesCollection.Add($"{dzien.Data.ToShortDateString()}: {dayDescription}");
                }
            }
            #endregion


            #region FKDFAppServices
            IKDFEmployee employee;
            var getEmployeeResult =
                FKDFEmployees.GetKDFEmployee(new CIdentifier(emploRequest.ExternalEmployeeId), out employee);
            _logger.WriteLine($"GetKDFEmployee result: {getEmployeeResult.ToString()}",
                getEmployeeResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!getEmployeeResult.Success) return new VacationValidationResponseModel() { RequestIsValid = false, Message = $"Employee not found: {getEmployeeResult.ToString()}" };

            List<IEventType> eventTypes;
            var getEventTypesResult = FKDFAbsences.GetEventTypesValidForAbsenceUse(out eventTypes, employee);
            _logger.WriteLine($"GetEventTypesValidForAbsenceUse result: {getEventTypesResult.ToString()}",
                getEventTypesResult.Success ? LogLevelEnum.Information : LogLevelEnum.Error);
            if (!getEventTypesResult.Success) return new VacationValidationResponseModel() { RequestIsValid = false, Message = $"Get event types error: {getEventTypesResult.ToString()}" };

            IEventType eventType = eventTypes.FirstOrDefault(et => et.HasBalance &&
                                                                   et.BalanceType.Identifier.IntId.ToString()
                                                                       .Equals(emploRequest.ExternalVacationTypeId));

            IBalanceEmployee balance;
            var balanceResult = FKDFAbsences.GetAbsenceBalanceForEmployee(out balance, employee, eventType.BalanceType);
            if (!balanceResult.Success) return new VacationValidationResponseModel() { RequestIsValid = false, Message = $"Get balance error: {balanceResult.ToString()}" };

            double dostepneDni = balance.CurrentSize;
            #endregion


            double przelicznik = 8.0;
            var availableHours = dostepneDni * przelicznik;
            var hoursUsedByRequest = pelnaListaCzasowPracy.Sum(ts => ts.TotalHours);

            responseModel.RequestIsValid = availableHours >=
                                           hoursUsedByRequest;

            responseModel.Message = $"Dostępne godziny: {availableHours} h, wniosek zużywa: {hoursUsedByRequest} h";
            return responseModel;
        }
    }
}