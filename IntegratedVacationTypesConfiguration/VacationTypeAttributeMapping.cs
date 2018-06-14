using System.Configuration;

namespace SageConnector.IntegratedVacationTypesConfiguration
{
    public class VacationTypeAttributeMapping : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new VacationTypeMapping();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((VacationTypeMapping)element).VacationType;
        }
    }
}
