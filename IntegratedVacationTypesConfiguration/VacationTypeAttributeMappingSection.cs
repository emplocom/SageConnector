using System.Configuration;
using EmploApiSDK.Logic.EmployeeImport;

namespace SageConnector.IntegratedVacationTypesConfiguration
{
    public class VacationTypeAttributeMappingSection : ConfigurationSection
    {
        public const string SectionName = "VacationTypeAttributeMappingSection";
        private const string EndpointCollectionName = "VacationTypeAttributeMapping";

        [ConfigurationProperty(EndpointCollectionName)]
        [ConfigurationCollection(typeof(AttributeMapping), AddItemName = "add")]
        public VacationTypeAttributeMapping Instances
        {
            get
            {
                return (VacationTypeAttributeMapping)base[EndpointCollectionName];
            }
        }
    }
}
