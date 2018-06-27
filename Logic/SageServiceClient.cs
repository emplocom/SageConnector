using System;
using System.Configuration;
using EmploApiSDK.Logger;
using Forte.Kadry.KDFAppCOMServer;
using Forte.Kadry.KDFAppServices;
using Symfonia.Common.Application;
using Symfonia.Common.Defs;

namespace SageConnector.Logic
{
    public class SageServiceClient
    {
        public static void EnsureConnectionOpen(ILogger logger)
        {
            EnterInstance(logger);
            OpenFirmAndLogOnUser(logger);
            //FKDFAppCOMServer.MaxInstancesCount = Environment.ProcessorCount;
        }

        private static void EnterInstance(ILogger logger)
        {
            CResult res = CResult.New("EnterInstance");

            try
            {
                SCTUtility.Verify(FKDFAppServices.EnterInstance("Nieobecności", "Absence", "Absence_V1", true, true));
                FTemporaryOnlineLicencing.Settings.Online = bool.Parse(ConfigurationManager.AppSettings["TemporaryOnlineLicencing"]);
            }
            catch (Exception ex)
            {
                res.Add(ex);
                logger.WriteLine(res.ToString(), LogLevelEnum.Error);
            }
            finally
            {
                logger.WriteLine(res.ToString());
            }
        }

        private static IResult OpenFirmAndLogOnUser(ILogger logger)
        {
            CResult res = CResult.New("OpenFirmAndLogOnUser");

            if (!res.Check(FKDFAppServices.OpenFirm(
                ConfigurationManager.AppSettings["DatabaseServer"], ConfigurationManager.AppSettings["DatabaseName"],
                ConfigurationManager.AppSettings["DBUserLogin"], ConfigurationManager.AppSettings["DBUserPassword"],
                bool.Parse(ConfigurationManager.AppSettings["DBUserIntegrated"]),
                ConfigurationManager.AppSettings["DBAdminLogin"], ConfigurationManager.AppSettings["DBAdminPassword"],
                bool.Parse(ConfigurationManager.AppSettings["DBAdminIntegrated"]),
                ConfigurationManager.AppSettings["AppUserLogin"], ConfigurationManager.AppSettings["AppUserPassword"],
                bool.Parse(ConfigurationManager.AppSettings["AppUserIntegrated"]))))
            {
                logger.WriteLine(res.ToString(), LogLevelEnum.Error);
                return res;
            }


            logger.WriteLine($"Firma '{ConfigurationManager.AppSettings["DatabaseServer"]}.{ConfigurationManager.AppSettings["DatabaseName"]}' została otwarta.");

            return res;
        }

        private static IResult LogOffUserAndCloseFirm(ILogger logger)
        {
            CResult res = CResult.New("LogOffUserAndCloseFirm");

            try
            {
                if (!res.Check(FKDFAppServices.CloseFirm()))
                    return res;

                logger.WriteLine("Firma została zamknięta");

            }
            catch (Exception ex)
            {
                res.Add(ex);
            }
            return res;
        }

        private static void ExitInstance(ILogger logger)
        {
            CResult res = CResult.New("ExitInstance");

            try
            {
                SCTUtility.Verify(FKDFAppServices.ExitInstance());
            }
            catch (Exception ex)
            {
                res.Add(ex);
                logger.WriteLine(res.ToString(), LogLevelEnum.Error);
            }
            finally
            {
                logger.WriteLine(res.ToString());
            }
        }

        public static void CloseConnection(ILogger logger)
        {
            LogOffUserAndCloseFirm(logger);
            ExitInstance(logger);
        }
    }
}