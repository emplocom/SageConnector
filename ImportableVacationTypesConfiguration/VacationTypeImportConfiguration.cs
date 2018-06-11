using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.WebPages;
using EmploApiSDK.Logger;

namespace SageConnector.ImportableVacationTypesConfiguration
{
    public class VacationTypeImportConfiguration
    {
        public List<VacationTypeMapping> VacationTypeMappings { get; private set; } = new List<VacationTypeMapping>();

        ///<exception cref = "EmploApiClientFatalException" > Thrown when a fatal error, requiring request abortion, has occurred </exception>
        public VacationTypeImportConfiguration(ILogger logger)
        {
            if (!(ConfigurationManager.GetSection(VacationTypeAttributeMappingSection.SectionName) is VacationTypeAttributeMappingSection configSection))
            {
                logger.WriteLine($"Attributes mapping in {VacationTypeAttributeMappingSection.SectionName} is empty", LogLevelEnum.Error);
                throw new EmploApiClientFatalException($"A fatal error has occurred while reading configuration.");
            }

            foreach (VacationTypeMapping e in configSection.Instances)
            {
                VacationTypeMappings.Add(e);
            }
        }

        public List<string> GetAllMappedVacationTypes_Flat()
        {
            return VacationTypeMappings.Select(m => m.VacationType)
                .Union(VacationTypeMappings.Where(m => !m.OnDemandType.IsEmpty()).Select(m => m.OnDemandType)).ToList();
        }
    }
}
