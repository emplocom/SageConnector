using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
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
        
        public void SyncVacationData(List<string> employeeIdentifiers = null)
        {
            List<IKDFEmployee> employeeList = new List<IKDFEmployee>();

            if (employeeIdentifiers == null || !employeeIdentifiers.Any())
            {
                //TODO: which statuses should be ignored?
                _logger.WriteLine("Starting vacation balance synchronization for all employees...");
                var employeeResult = FKDFEmployees.GetKDFEmployees(out employeeList, EEmployeeType.cooperate, EEmployeeType.employee, EEmployeeType.civilian, EEmployeeType.planned, EEmployeeType.undefined);

                if (!employeeResult.Success)
                {
                    _logger.WriteLine($"An error occurred while fetching the employee list, message: {employeeResult.ToString()}");
                    return;
                }   
            }
            else
            {
                _logger.WriteLine($"Starting vacation balance synchronization for employees with ids: {string.Join(",", employeeIdentifiers)}");
                foreach (var employeeId in employeeIdentifiers)
                {
                    IKDFEmployee employee;
                    var result = FKDFEmployees.GetKDFEmployee(new CIdentifier(employeeId), out employee);

                    if (result.Success)
                    {
                        employeeList.Add(employee);
                    }
                    else
                    {
                        _logger.WriteLine($"An error occurred while getting employee with Id {employeeId}, synchronization will be skipped for this user! Error message: {result.ToString()}", LogLevelEnum.Warning);
                    }
                }
            }

            Dictionary<IKDFEmployee, List<IBalanceEmployee>> employeeBalancesCollections;
            var balanceResult = FKDFAbsences.GetAbsenceBalancesForEmployees(out employeeBalancesCollections, employeeList, null);

            if (!balanceResult.Success)
            {
                _logger.WriteLine($"An error occurred while fetching absence balances for employees, message: {balanceResult.ToString()}");
                return;
            }

            ImportEmployeeBalancesCollections(employeeBalancesCollections);
        }
        
        private void ImportEmployeeBalancesCollections(
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
                        _logger.WriteLine($"Employee {employeeBalancesCollection.Key.Identifier.GetExternalIdForEmplo()}: Could not find vacationTypeBalance for mapping {vacationTypeMapping.VacationType}, skipping synchronization for this type", LogLevelEnum.Warning);
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
                            _logger.WriteLine($"Employee {employeeBalancesCollection.Key.Identifier.GetExternalIdForEmplo()}: Could not find onDemandTypeBalance for mapping {vacationTypeMapping.OnDemandType}, skipping synchronization for it and it's base type ({vacationTypeMapping.VacationType})", LogLevelEnum.Warning);
                            continue;
                        }

                        vacationBalanceDto.OnDemandDays = Convert.ToDecimal(onDemandTypeBalance.CurrentSize);
                    }

                    allVacationBalancesToImport.Add(vacationBalanceDto);
                }
            }

            if (allVacationBalancesToImport.Any())
            {
                Task.Run(() => allVacationBalancesToImport.Chunk(10).ToList().ForEachAsync(
                    async dataChunk => await Import(dataChunk.ToList())
                ));
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