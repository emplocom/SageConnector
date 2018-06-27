using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EmploApiSDK.Logger;
using SageConnector.Logic;

namespace SageConnector
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private ILogger _logger;
        private SageServiceClient _client;

        protected void Application_Start()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            _logger = LoggerFactory.CreateLogger(null);
            
            SageServiceClient.EnsureConnectionOpen(_logger);
        }

        private Assembly assemblyResolve(object sender, ResolveEventArgs args)
        {
            // Use latest strong name & version when trying to load SDK assemblies
            var requestedAssembly = new AssemblyName(args.Name);
            if (requestedAssembly.Name != "Microsoft.Practices.ServiceLocation")
                return null;

            var alreadyLoadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == requestedAssembly.Name);

            if (alreadyLoadedAssembly != null)
            {
                return alreadyLoadedAssembly;
            }

            requestedAssembly.Version = new Version(1, 3, 0);
            requestedAssembly.SetPublicKeyToken(new AssemblyName("Microsoft.Practices.ServiceLocation, PublicKeyToken=31bf3856ad364e35").GetPublicKeyToken());
            requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(assemblyResolve);

            return Assembly.Load(requestedAssembly);

        }

        protected void Application_End()
        {
            SageServiceClient.CloseConnection(_logger);
        }
    }
}
