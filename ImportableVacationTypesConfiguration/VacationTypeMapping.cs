using System.Configuration;

namespace SageConnector.ImportableVacationTypesConfiguration
{
    public class VacationTypeMapping : ConfigurationElement
    {
        [ConfigurationProperty("vacationType", IsKey = true, IsRequired = true)]
        public string VacationType
        {
            get { return (string)base["vacationType"]; }
            set { base["vacationType"] = value; }
        }

        [ConfigurationProperty("onDemandType", IsRequired = false)]
        public string OnDemandType
        {
            get { return (string)base["onDemandType"]; }
            set { base["onDemandType"] = value; }
        }
    }
}
