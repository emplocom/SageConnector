using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebPages;
using EmploApiSDK.ApiModels.IntegratedVacations;
using EmploApiSDK.Client;
using EmploApiSDK.Logger;
using Forte.Common.Interfaces;
using Forte.Kadry.KDFAppServices;
using Newtonsoft.Json;
using SageConnector.ImportableVacationTypesConfiguration;
using Symfonia.Common.Defs;

namespace SageConnector.Logic
{
    public class SyncVacationDataLogic
    {
        private readonly ILogger _logger;
        private readonly ApiClient _apiClient;
        private readonly VacationTypeImportConfiguration _vacationTypeImportConfiguration;

        readonly ApiConfiguration _apiConfiguration = new ApiConfiguration()
        {
            EmploUrl = ConfigurationManager.AppSettings["EmploUrl"],
            ApiPath = ConfigurationManager.AppSettings["ApiPath"] ?? "apiv2",
            Login = ConfigurationManager.AppSettings["Login"],
            Password = ConfigurationManager.AppSettings["Password"]
        };

        public SyncVacationDataLogic(ILogger logger)
        {
            _logger = logger;
            _apiClient = new ApiClient(_logger, _apiConfiguration);
            _vacationTypeImportConfiguration = new VacationTypeImportConfiguration(logger);
        }

        public async Task SyncVacationData(List<string> employeeIdentifiers = null)
        {
            List<IKDFEmployee> employeeList = new List<IKDFEmployee>();

            if (employeeIdentifiers == null || !employeeIdentifiers.Any())
            {
                //TODO: which statuses should be ignored?
                var employeeResult = FKDFEmployees.GetKDFEmployees(out employeeList, EEmployeeType.cooperate, EEmployeeType.employee, EEmployeeType.civilian, EEmployeeType.planned, EEmployeeType.undefined);

                if (!employeeResult.Success) return;
            }
            else
            {
                foreach (var employeeId in employeeIdentifiers)
                {
                    IKDFEmployee employee;
                    var result = FKDFEmployees.GetKDFEmployee(new CIdentifier(employeeId), out employee);

                    if (result.Success)
                    {
                        employeeList.Add(employee);
                    }
                }
            }

            Dictionary<IKDFEmployee, List<IBalanceEmployee>> employeeBalancesCollections;
            var balanceResult = FKDFAbsences.GetAbsenceBalancesForEmployees(out employeeBalancesCollections, employeeList, null);

            if (!balanceResult.Success) return;

            await ImportEmployeeBalancesCollections(employeeBalancesCollections);
        }

        public async Task SyncVacationDataForEmployeeFromBalance(List<IBalanceEmployee> employeeBalances)
        {
            if (employeeBalances.Any())
            {
                var employeeBalanceDataAsDict = new Dictionary<IKDFEmployee, List<IBalanceEmployee>>();
                employeeBalanceDataAsDict.Add(employeeBalances.First().Employee, employeeBalances);
                await ImportEmployeeBalancesCollections(employeeBalanceDataAsDict);
            }
            else
            {
                //TODO: log error - balances collection was empty
            }
        }

        private async Task ImportEmployeeBalancesCollections(
            Dictionary<IKDFEmployee, List<IBalanceEmployee>> employeeBalancesCollections)
        {
            List<IntegratedVacationsBalanceDto> allVacationBalancesToImport = new List<IntegratedVacationsBalanceDto>();

            foreach (var employeeBalancesCollection in employeeBalancesCollections)
            {
                foreach (var vacationTypeMapping in _vacationTypeImportConfiguration.VacationTypeMappings)
                {
                    var vacationTypeBalance = employeeBalancesCollection.Value.FirstOrDefault(b =>
                        b.Type.Identifier.GetExternalIdForEmplo().Equals(vacationTypeMapping.VacationType));

                    if (vacationTypeBalance == null)
                    {
                        //TODO: log
                        continue;
                    }

                    var vacationBalanceDto = new IntegratedVacationsBalanceDto();
                    vacationBalanceDto.ExternalEmployeeId = vacationTypeBalance.Employee.Identifier.GetExternalIdForEmplo();
                    vacationBalanceDto.ExternalVacationTypeId = vacationTypeBalance.Type.Identifier.GetExternalIdForEmplo();
                    vacationBalanceDto.AvailableDays = Convert.ToDecimal(vacationTypeBalance.CurrentSize);
                    vacationBalanceDto.OutstandingDays = Convert.ToDecimal(vacationTypeBalance.BehindDimension);

                    if (!vacationTypeMapping.OnDemandType.IsEmpty())
                    {
                        var onDemandTypeBalance = employeeBalancesCollection.Value.FirstOrDefault(b =>
                            b.Type.Identifier.GetExternalIdForEmplo().Equals(vacationTypeMapping.OnDemandType));

                        if (onDemandTypeBalance == null)
                        {
                            //TODO: log
                            continue;
                        }

                        vacationBalanceDto.OnDemandDays = Convert.ToDecimal(onDemandTypeBalance.CurrentSize);
                    }

                    allVacationBalancesToImport.Add(vacationBalanceDto);
                }
            }

            if (allVacationBalancesToImport.Any())
            {
                await allVacationBalancesToImport.Chunk(10).ToList().ForEachAsync(
                    async dataChunk => await Import(dataChunk.ToList())
                );
            }
        }

        private async Task Import(List<IntegratedVacationsBalanceDto> vacationsBalanceDtosToImport)
        {
            var request = JsonConvert.SerializeObject(new ImportIntegratedVacationsBalanceDataRequestModel()
            {
                BalanceList = vacationsBalanceDtosToImport
            });

            bool dryRun;

            if (bool.TryParse(ConfigurationManager.AppSettings["DryRun"], out dryRun) && dryRun)
            {
                _logger.WriteLine(
                    "Importer is in DryRun mode, data retrieved from Sage will be printed to log, but it won't be sent to emplo.");
                _logger.WriteLine(request);
            }
            else
            {
                var response = await _apiClient.SendPostAsync<ImportIntegratedVacationsBalanceDataResponseModel>(
                    request, _apiConfiguration.ImportIntegratedVacationsBalanceDataUrl);

                response.resultRows = response.resultRows.OrderBy(r => r.ExternalEmployeeId).ToList();

                response.resultRows.ForEach(r =>
                    _logger.WriteLine(
                        
                        $"Employee Id: {r.ExternalEmployeeId}, Import result status: [{r.OperationStatus.ToString()}]{(r.Message.IsEmpty() ? string.Empty : $", Message: {r.Message}")}",
                        MapImportStatusToLogLevel(r.OperationStatus)));
            }
        }

        private LogLevelEnum MapImportStatusToLogLevel(ImportVacationDataStatusCode status)
        {
            switch (status)
            {
                case ImportVacationDataStatusCode.Warning:
                    return LogLevelEnum.Warning;
                case ImportVacationDataStatusCode.Error:
                    return LogLevelEnum.Error;
                default:
                    return LogLevelEnum.Information;
            }
        }
    }
}