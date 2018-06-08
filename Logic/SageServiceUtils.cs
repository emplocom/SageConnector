using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Web;
using EmploApiSDK.Logger;
using Forte.Kadry.KDFAppServices;
using Symfonia.Common.Application;
using Symfonia.Common.Defs;

namespace SageConnector.Logic
{
    public class SageServiceUtils
    {
        private ILogger _logger;

        public SageServiceUtils(ILogger logger)
        {
            _logger = logger;
        }

        public void EnterInstance()
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
                _logger.WriteLine(res.ToString(), LogLevelEnum.Error);
            }
            finally
            {
                _logger.WriteLine(res.ToString());
                //HandleResult(res);
            }
        }

        //private void HandleResult(IResult res)
        //{
        //    if (res.Error)
        //    {
        //        if (FCommonServer.CurrentSessionOpened)
        //            _logger.WriteLine(res.ToString(), LogLevelEnum.Error);
        //    }
        //    else
        //    {
        //        _logger.WriteLine(res.ToString());
        //    }
        //}


        public IResult OpenFirmAndLogOnUser()
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
                _logger.WriteLine(res.ToString(), LogLevelEnum.Error);
                return res;
            }
                

            _logger.WriteLine($"Firma '{ConfigurationManager.AppSettings["DatabaseServer"]}.{ConfigurationManager.AppSettings["DatabaseName"]}' została otwarta.");

            return res;
        }

        public IResult LogOffUserAndCloseFirm()
        {
            CResult res = CResult.New("LogOffUserAndCloseFirm");

            try
            {
                if (!res.Check(FKDFAppServices.CloseFirm()))
                    return res;

                _logger.WriteLine("Firma została zamknięta");

            }
            catch (Exception ex)
            {
                res.Add(ex);
            }
            return res;
        }
    }
}