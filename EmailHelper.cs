using RCBC.BulkTransaction.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace RCBC.BulkTransaction.API.Helpers {
    public class EmailHelper {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //---------------------------------------------------------------------------------------------------------------
        // Constants
        //---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Error Mail Template
        /// </summary>
        private static readonly string ErrorMailTemplate = @"
        <html>
        <head>
            <title>{$$AppName$$} - Exception</title>
	        <style type=text/css>
	        BODY { 
		        color: #000000; 
		        background-color: white; 
		        font-size: 12px; 
		        font-family: Verdana; 
		        margin-left: 0px; 
		        margin-top: 0px; 
	        }
	        .heading1 { 
		        color: #ffffff; 
		        font-family: Tahoma; 
		        font-size: 18px; 
		        font-weight: normal; 
		        background-color: #003366; 
		        margin-top: 0px; 
		        margin-bottom: 0px; 
		        padding-top: 10px; 
		        padding-bottom: 3px; 
		        padding-left: 5px; 
		        width: 100%; 
		        border-bottom: 4px solid; 
		        border-bottom-color: #e5e5cc;
	        }
	        .heading2 { 
		        color: #ffffff; 
		        font-family: Tahoma; 
		        font-size: 14px; 
		        font-weight: normal; 
		        background-color: #003366; 
		        margin-top: 0px; 
		        margin-bottom: 0px; 
		        padding-top: 10px; 
		        padding-bottom: 3px; 
		        padding-left: 5px; 
		        width: 100%; 
	        }
	        .trace { 
		        margin-bottom: 5px; 
		        margin-top: 5px; 
		        background-color: #e5e5cc; 
		        padding: 5px; 
		        margin-left:0px; 
		        font-family: Courier New; 
		        font-size: x-small; 
		        border: 1px #f0f0e0 solid; 
	        }
	        .content { 
		        margin-left:10px; 
	        }
	        .spacer { 
		        height:20px; 
	        }
	        </style>
        </head>
        <body>
	        <p class=heading1>{$$AppName$$} Exception</p>
	        <br/><br/>
	        <div class=content>
	            <p class=heading2>Exception Details</p>
	            <br /><br />
	            <b>Source</b>
	            <p class=trace>{$$Source$$}</p>
	            <br />
	            <b>Message</b>
	            <p class=trace>{$$Message$$}</p>
	            <br />
	            <b>Stack Trace</b>
	            <p class=trace>{$$StackTrace$$}</p>
	        </div>
        </body>
        </html>";

        //---------------------------------------------------------------------------------------------------------------
        // Methods
        //---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="fromAddress">From address.</param>
        /// <param name="toAddress">To address.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        public static bool SendEmail(string fromAddress, string toAddress, string subject, string body, string ccAddresses, string bccAddresses, bool isHtml, int? templateId, List<string> attachmentfiles = null) {
            using (MailMessage email = new MailMessage()) {

                email.From = new MailAddress(fromAddress);
                email.Subject = subject;
                email.Body = body;
                email.IsBodyHtml = isHtml;
                List<MailAddress> tos = GetRecepients(toAddress);
                foreach (var address in tos) {
                    email.To.Add(address);
                }

                List<MailAddress> ccs = GetRecepients(ccAddresses);
                foreach (var address in ccs) {
                    email.CC.Add(address);
                }

                List<MailAddress> bccs = GetRecepients(bccAddresses);
                foreach (var address in bccs) {
                    email.Bcc.Add(address);
                }

                if (attachmentfiles != null) {
                    foreach (var attachmentfilename in attachmentfiles)
                        if (!string.IsNullOrEmpty(attachmentfilename)) {
                            if (File.Exists(attachmentfilename)) {
                                email.Attachments.Add(new Attachment(attachmentfilename));
                            }

                        }
                }

                try {
                    using (SmtpClient smtp = new SmtpClient(EnvironmentConfiguration.Current.SMTP_HOST, EnvironmentConfiguration.Current.SMTP_PORT)) {
                        smtp.Send(email);
                        smtp.Timeout = 180000;
                    }
                } catch (Exception e) {
                    string error = e.Message;
                    log.Error(e);
                    return false;
                }


                return true;
            }
        }

        /// <summary>
        /// Get Receipients
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static List<MailAddress> GetRecepients(string value) {
            List<MailAddress> list = new List<MailAddress>();

            if (string.IsNullOrEmpty(value)) return list;

            List<string> receipients = value.Split(';').ToList();
            foreach (string receipient in receipients) {
                if (string.IsNullOrWhiteSpace(receipient)) continue;
                list.Add(new MailAddress(receipient));
            }

            return list;
        }

        /// <summary>
        /// Send Exception Mail
        /// </summary>
        /// <param name="e"></param>
        public static void SendExceptionMail(Exception e) {

            string stacktrace = e.StackTrace;
            if (e.InnerException != null)
                stacktrace += "<br><br>" + e.InnerException.StackTrace;

            string source = e.Source;
            if (e.InnerException != null)
                source += "<br><br>" + e.InnerException.Source;

            string message = e.Message;
            if (e.InnerException != null)
                message += "<br><br>" + e.InnerException.Message;

            TemplateService engine = new TemplateService(ErrorMailTemplate);
            engine.AddParameter("AppName", "PesonetBulk");
            engine.AddParameter("Source", source);
            engine.AddParameter("Message", message);
            engine.AddParameter("StackTrace", stacktrace);
            engine.Process();

            string fromAddress = EnvironmentConfiguration.Current.EMAIL_FROM;
            string toAddress = EnvironmentConfiguration.Current.EMAIL_ERROR_RECIPIENTS;
            string emailSubject = "PESONETBULK_" + " Exception";
            string emailBody = engine.Text;

            EmailHelper.SendEmail(fromAddress, toAddress, emailSubject, emailBody, null, null, true, null);
        }




    }
}