using System;
using System.Net;
using System.Net.Security;
using System.Text;
using SimpleJSON;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Net.Mail;

namespace VideoUploadScript
{
    class VideoUploader
    {
        //used to define environment that will be used
        enum Server { test, stage, prod, dev };
        static Server uploadTo = Server.prod; //default to Prod

        //default directory to upload from NAS is 'volume1/ETS_Backup/Recording_Backup/'. Change this in the settings.cfg file
        static private string folderDir = "/volume1/ETS_Backup/Recording_Backup/";

        //variables needed for upload process
        private static string urlParams;
        private static string mVideoDir;
        private static string line;

        //Used for reading config file (settings.cfg)
        private static StreamReader mReader;

        private static int mFilesUploaded = 0;

        //Used for sending email
        static bool mailSent = false;
        private static string sendToEmailAddresses = "";
        private static string emailSubject = "ETS NAS Upload Status";
        private static string sendFromEmailAddress = "mursion.logs@mursion.com";
        static private int failedUploads = 0;
        static private int mSessionsUploaded = 0;

        /// <summary>
        /// By using the print(string) method, a message will both appear in the console as well as in the email upon completion of the upload.
        /// </summary>
        static public string emailMsg = "";
        static public void print(string msg)
        {
            Console.WriteLine(msg);
            emailMsg = String.Format("{0}\n{1}", emailMsg, msg);
            
        }

        /// <summary>
        /// Define each server, their upload URL, upload Status URL, and credentials. 
        /// These can be reassigned by adding the name of the variable = value in the config file
        /// Such as: 
        ///     stageStatusURL = https://ibt2-note-test.ets.org/rs/videoservice/updateuploadstatus
        ///     or
        ///     prodAuthKey = extsysmursionstg:1qwe@ASD
        /// </summary>
        static private string testUploadUrl = "https://ibt2-note-dev.ets.org/rs/videoservice/getuploadurl";
        static private string testStatusUrl = "https://ibt2-note-dev.ets.org/rs/videoservice/updateuploadstatus";
        static private string testAuthKey = "extsysmursiondvi:1qscZDR%";

        static private string stageUploadUrl = "https://ibt2-note-test.ets.org/rs/videoservice/getuploadurl";
        static private string stageStatusUrl = "https://ibt2-note-test.ets.org/rs/videoservice/updateuploadstatus";
        static private string stageAuthKey = "extsysmursiontst:1qscZDR%";

        static private string prodUploadUrl = "https://ibt2-note-uat.ets.org/rs/videoservice/getuploadurl";
        static private string prodStatusUrl = "https://ibt2-note-uat.ets.org/rs/videoservice/updateuploadstatus";
        static private string prodAuthKey = "extsysmursionstg:1qwe@ASD";

        static private string devUploadUrl = "https://ibt2-note-dev.ets.org/rs/videoservice/getuploadurl";
        static private string devStatusUrl = "https://ibt2-note-dev.ets.org/rs/videoservice/getuploadurl";
        static private string devAuthKey = "extsysmursiondvi:1qscZDR%";

        /// <summary>
        /// Retrieve the UploadURL that is to be used. Relies on the UploadTo variable which defines the environment to be used.
        /// </summary>
        /// <returns>String of URL declared above</returns>
        static public string getUploadUrl()
        {
            if (uploadTo == Server.test)
            {
                return testUploadUrl;
            }
            else if (uploadTo == Server.stage)
            {
                return stageUploadUrl;
            }
            else if (uploadTo == Server.prod)
            {
                return prodUploadUrl;
            }
            else if (uploadTo == Server.dev)
            {
                return devUploadUrl;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve the StatusURL that is to be used. Relies on the UploadTo variable which defines the environment to be used.
        /// </summary>
        /// <returns>String of URL declared above</returns>
        static public string getStatusUrl()
        {
            if (uploadTo == Server.test)
            {
                return testStatusUrl;
            }
            else if (uploadTo == Server.stage)
            {
                return stageStatusUrl;
            }
            else if (uploadTo == Server.prod)
            {
                return prodStatusUrl;
            }
            else if (uploadTo == Server.dev)
            {
                return devStatusUrl;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve the credentials to be used. Relies on the UploadTo variable which defines the environment to be used
        /// </summary>
        /// <returns>String with credentials formatted as 'username:password'</returns>
        static public string getAuthKey()
        {
            if (uploadTo == Server.test)
            {
                return testAuthKey;
            }
            else if (uploadTo == Server.stage)
            {
                return stageAuthKey;
            }
            else if (uploadTo == Server.prod)
            {
                return prodAuthKey;
            }
            else if (uploadTo == Server.dev)
            {
                return devAuthKey;
            }
            else
            {
                return null;
            }
        }

        static public void readCFG()
        {
            try
            {
                mReader = File.OpenText("Settings.cfg");
            }
            catch (FileNotFoundException e)
            {
                print("Settings.cfg not found. Proceeding with default values.\n");
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                print("Settings.cfg file not accessible due to permission restrictions. Proceeding with default values.\n");
                return;
            }

            while ((line = mReader.ReadLine()) != null)
            {
                if (line.Contains("NASDir"))
                {
                    folderDir = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("Email"))
                {
                    sendToEmailAddresses = line.Substring(line.IndexOf('=') + 2);
                }

                //test
                else if (line.Contains("testUploadUrl"))
                {
                    testUploadUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("testStatusUrl"))
                {
                    testStatusUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("testAuthKey"))
                {
                    testAuthKey = line.Substring(line.IndexOf('=') + 2);
                }

                //stage
                else if (line.Contains("stageUploadUrl"))
                {
                    stageUploadUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("stageStatusUrl"))
                {
                    stageStatusUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("stageAuthKey"))
                {
                    stageAuthKey = line.Substring(line.IndexOf('=') + 2);
                }

                //prod
                else if (line.Contains("prodUploadUrl"))
                {
                    prodUploadUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("prodStatusUrl"))
                {
                    prodStatusUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("prodAuthKey"))
                {
                    prodAuthKey = line.Substring(line.IndexOf('=') + 2);
                }

                //dev
                else if (line.Contains("devUploadUrl"))
                {
                    devUploadUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("devStatusUrl"))
                {
                    devStatusUrl = line.Substring(line.IndexOf('=') + 2);
                }
                else if (line.Contains("devAuthKey"))
                {
                    devAuthKey = line.Substring(line.IndexOf('=') + 2);
                }

                else if (line.CompareTo("") == 0 || line.CompareTo("\n") == 0 || line.CompareTo("\r") == 0 || line.CompareTo("\r\n") == 0 || line.CompareTo(" ") == 0) { }

                else
                {
                    print(string.Format("The line: \"{0}\" was not recognized from Settings.cfg", line));
                }
            }
        }

        static void Main(string[] args)
        {
            //Unless otherwise set from parameters, we will be uploading recordings from today's date
            DateTime dateToUpload = DateTime.Today;

            //If there are arguments attached, let's process through them and set accordingly
            if (args.Length != 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].CompareTo("-help") == 0)
                    {
                        Console.WriteLine("\n*****************************************************");
                        Console.WriteLine("Upload recordings to AWS S3 Cloud service\n-----------------------------------------");
                        Console.WriteLine("   To upload recordings from a custom date:\n\t-date mm/dd/yyyy");
                        Console.WriteLine("\n   To specify which Server environment to upload to:\n\t-server test|stage|prod|dev");
                        Console.WriteLine("\n   Or use no parameters to default to prod environment on today's date.");
                        Console.WriteLine("*****************************************************");
                        return;
                    }
                    else if (args[i].CompareTo("-date") == 0)
                    {
                        try
                        {
                            dateToUpload = Convert.ToDateTime(args[i + 1]); //Formatted "MM/DD/YYYY"
                        }
                        catch
                        {
                            Console.WriteLine("The entered date appears to be incorrectly formatted. Please enter using the format: '-date mm/dd/yyyy' and try again.");
                            return;
                        }
                        i++;
                    }
                    else if (args[i].CompareTo("-server") == 0)
                    {
                        if (args[i + 1].CompareTo("test") == 0 || args[i + 1].CompareTo("Test") == 0)
                        {
                            uploadTo = Server.test;
                        }
                        else if (args[i + 1].CompareTo("stage") == 0 || args[i + 1].CompareTo("Stage") == 0)
                        {
                            uploadTo = Server.stage;
                        }
                        else if (args[i + 1].CompareTo("prod") == 0 || args[i + 1].CompareTo("Prod") == 0)
                        {
                            uploadTo = Server.prod;
                        }
                        else if (args[i + 1].CompareTo("dev") == 0 || args[i + 1].CompareTo("Dev") == 0)
                        {
                            uploadTo = Server.dev;
                        }
                        else
                        {
                            Console.WriteLine("The Server Environment that you entered was not recognized. Please try again using the format: '-server test|stage|prod|dev'\nFor example, to use the production environment, use: -server prod");
                            return;
                        }
                        i++;
                    }
                }
            }

            //Process Settings.cfg
            readCFG();

            mVideoDir = folderDir + dateToUpload.ToString("yyyy_MM_dd") + "/";

            print(string.Format("Uploading recordings from {0}", mVideoDir));
            print(string.Format(string.Format("Destination URLs:\n\tUpload URL: {0}\n\tUpload Status URL: {1}\n", getUploadUrl(), getStatusUrl())));

            ServicePointManager.ServerCertificateValidationCallback = validateCert;
            
            if (checkDirStatus(mVideoDir))
            {
                //Because this is run on the initial launch of the program, we know that this date is the same day as the recordings
                DateTime startTime = DateTime.Now;

                //Count the number of UploadStatus files so we know how many stations there are
                //We'll need to send out this number later in case a station did not complete post production before this file runs
                DirectoryInfo di = new DirectoryInfo(mVideoDir);
                int stationNum = di.GetFiles("*.txt", SearchOption.TopDirectoryOnly).Length;
                Console.WriteLine(string.Format("Number of Stations found: {0}", stationNum));

                if (!checkNASUploadStatus())
                {
                    print("\n******** Warning!!! ********* Some recordings may not have completed transfer to the NAS! Please verify that all recordings have been uploaded.\n");
                }

                //Upload Recordings
                ProcessDirectory(mVideoDir);

                string _uploadTime = (DateTime.Now - startTime).TotalSeconds.ToString();
                print("\nTime to upload " + mFilesUploaded + " files was :" + _uploadTime + "s");

                // Send email message about NAS Upload                 
                string _msgBody1 = "Upload from NAS to S3 Complete. \n\nNumber of Sessions found: " + mSessionsUploaded + "\nNumber of Files uploaded: " + mFilesUploaded + "\nFailed Uploads: " + failedUploads + "\nNumber of Stations found: " + stationNum + "\nTime Taken for Upload: " + _uploadTime + "s" + "\n\n*************************************\n\nUpload Output: \n" + emailMsg;
                string success_fail;
                if (failedUploads > 0)
                {
                    success_fail = " - ***Alert!***";
                }
                else
                {
                    success_fail = " - Success";
                }

                if (sendToEmailAddresses.CompareTo("") != 0)
                {
                    MailMessage message = new MailMessage(sendFromEmailAddress, sendToEmailAddresses, emailSubject + success_fail, _msgBody1);
                    sendEmail(message);
                }
            }
        }


        

        /// <summary>
        /// Async callback for send e-mail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            String token = (string)e.UserState;

            if (e.Cancelled)
            {
                print(string.Format("[{0}] Send canceled.", token));
            }
            if (e.Error != null)
            {
                print(string.Format("[{0}] {1}", token, e.Error.ToString()));
            }
            else
            {
                print("Message sent.");
            }
            mailSent = true;
        }
        /// <summary>
        /// Method to send automated email from inside the program
        /// </summary>
        /// <param name="args"></param>
        static public void sendEmail(MailMessage _msg)
        {
            print("******** Sending Status Update Email ********");
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential("mursion.logs@mursion.com", "Mursion2015");
            
            _msg.BodyEncoding = UTF8Encoding.UTF8;
            _msg.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            try { client.Send(_msg); }
            catch
            {
                Console.WriteLine("Email Unsuccessful! \nPlease make sure you have an internet connection available, and that your specified email addresses are formatted properly.");
            }
        }

        /// <summary>
        /// SSL Certificate Validation 
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_cert"></param>
        /// <param name="_chain"></param>
        /// <param name="_sslpolerr"></param>
        /// <returns></returns>
        public static bool validateCert(object _sender, X509Certificate _cert, X509Chain _chain, SslPolicyErrors _sslpolerr)
		{
			// Bypass security certificate validation for now
			return true;

			// For Valid Signed Certificates
			if(_sslpolerr == System.Net.Security.SslPolicyErrors.None)
				return true;

			// If there are errors in the certificate chain, look at each error to determine the cause.
			if ((_sslpolerr & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
			{
				if (_chain != null && _chain.ChainStatus != null)
				{
					foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in _chain.ChainStatus)
					{
						if ((_cert.Subject == _cert.Issuer) &&
								(status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot))
						{
							// Self-signed certificates with an untrusted root are valid.
							continue;
						}
						else
						{
							if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
							{
								// If there are any other errors in the certificate chain, the certificate is invalid,
								// so the method returns false.
								return false;
							}
						}
					}
				}
				// When processing reaches this line, the only errors in the certificate chain are
				// untrusted root errors for self-signed certificates. These certificates are valid
				// for default Exchange Server installations, so return true.
				return true;
			}
			else
			{
				// In all other cases, return false.
				return false;
			}

		}

        /// <summary>
        /// Method that checks the a text file in /volume1/ETS_Backup/Recording_Backup/ to see if the local upload to NAS is complete
        /// </summary>
        /// <returns>
        /// True or false depending on : separated value
        /// NASUploadComplete:true/false
        /// This value should be set to be true by powershell when upload is complete - it is reset automatically to false by the current program in C# once the upload to ETS is complete
        /// </returns>
        static bool checkNASUploadStatus()
        {
            foreach (string fileName in Directory.GetFiles(mVideoDir,"*.txt", SearchOption.TopDirectoryOnly))
            {
                mReader = File.OpenText(fileName);
                string[] components = null;
                while ((line = mReader.ReadLine()) != null)
                {
                    components = line.Split(':');
                }
                if (components != null)
                {
                    if (!Convert.ToBoolean(components[1]))
                        return false;
                }
                else
                    return false;
            }
            return true;

        }

        /// <summary>
        /// Checks to ensure that the directory for that day exists
        /// </summary>
        /// <param name="videodir"></param>
        /// <returns></returns>
        static public bool checkDirStatus(string videodir)
        {            
                DirectoryInfo di = new DirectoryInfo(videodir);
                if (!di.Exists)
                {
                    // Send email message about potential previous day directory       
                    string _msgBody = "Directory not found: " + videodir;
                if (sendToEmailAddresses.CompareTo("") != 0)
                {
                    MailMessage _dirnotfound = new MailMessage(sendFromEmailAddress, sendToEmailAddresses, emailSubject, _msgBody);
                    sendEmail(_dirnotfound);
                }
                return false;
                }
                else
                    return true;
        }

        /// <summary>
        /// Process all files in the directory passed in, recurse on any directories
        /// that are found, and process the files they contain.
        /// </summary>
        static public void ProcessDirectory(string targetDirectory)
        {
            //create an array of folder names and a symmetric dateTime array that can help us determine the order that recordings need to be uploaded
            DirectoryInfo di = new DirectoryInfo(targetDirectory);
            DirectoryInfo[] folderEntries = di.GetDirectories();
            string[] folderNames = new string[folderEntries.Length];
            for(int i = 0; i<folderEntries.Length; i++)
            {
                folderNames[i] = folderEntries[i].FullName;
            }
            DateTime[] folderTimes = new DateTime[folderEntries.Length];
            for (int i = 0; i < folderEntries.Length; i++)
            {
                string[] tokens = folderNames[i].Split('_');
                string[] date = tokens[tokens.Length - 2].Split('-');
                string[] time = tokens[tokens.Length - 1].Split('-');
                folderTimes[i] = new DateTime(Convert.ToInt32(date[2]), Convert.ToInt32(date[0]), Convert.ToInt32(date[1]), Convert.ToInt32(time[0]), Convert.ToInt32(time[1]), Convert.ToInt32(time[2]));
            }
            
            Array.Sort(folderTimes, folderNames);

            for(int u=folderNames.Length; u>0; u--)
            {
                // Process the list of mp4 files found in the directory.         
                DirectoryInfo dir = new DirectoryInfo(folderNames[u-1]);
                FileInfo[] fiArr = dir.GetFiles("*.mp4");

                foreach (FileInfo fInfo in fiArr)
                {
                    ProcessFile(fInfo);
                }

                mSessionsUploaded++;
            }

            
            /*
             *  Old method which recursively (and blindly) uploads all .mp4's
             */
            /*
            // Process the list of mp4 files found in the directory.         
            DirectoryInfo di = new DirectoryInfo(targetDirectory);
            FileInfo[] fiArr = di.GetFiles("*.mp4");

            foreach (FileInfo fInfo in fiArr)
            {
                ProcessFile(fInfo);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
            */
        }

        /// <summary>
        /// Process every mp4 file that you encounter
        /// </summary>
        /// <param name="fName"></param>
        static public void ProcessFile(FileInfo fName)
        {
            print(string.Format("Processing file '{0}'.", fName.FullName));
            string _curFile = fName.FullName;
            if (File.Exists(_curFile))
            {
                print(" *************** Obtaining Upload URL ***************");
                urlParams = "{\"fileName\":\"" + fName.Name + "\"}";
                string _url = restApiReq(getUploadUrl(), urlParams);

                if (_url != null)
                {
                    print(" *************** Uploading Video File ***************");
                    try
                    {
                        UploadObject(_url, _curFile);

                        print(" *************** Upload Status Update ***************");
                        urlParams = "{\"fileName\":\"" + fName.Name + "\", \"fileSize\":\"" + fName.Length + "\"}";
                        restApiReq(getStatusUrl(), urlParams);
                        mFilesUploaded++;
                    }
                    catch
                    {
                        try //a second time
                        {
                            print("!!!! No response from server. Trying again...");
                            UploadObject(_url, _curFile);
                            print(" *************** Upload Status Update ***************");
                            urlParams = "{\"fileName\":\"" + fName.Name + "\", \"fileSize\":\"" + fName.Length + "\"}";
                            restApiReq(getStatusUrl(), urlParams);
                            mFilesUploaded++;
                        }
                        catch
                        {
                            print(string.Format("Upload of {0} unsuccessful!",_curFile));
                        }
                    }

                    
                }                
            }            
        }

        /// <summary>
        /// Upload Object or Video File using this method
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fname"></param>
		static void UploadObject(string url, string fname)
		{             
			HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
			httpRequest.Method = "PUT";
            using (Stream dataStream = httpRequest.GetRequestStream())
			{
				byte[] buffer = new byte[8000];
				using (FileStream fileStream = new FileStream(fname, FileMode.Open, FileAccess.Read))
				{
					int bytesRead = 0;
					while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
					{
                        try
                        {
                            dataStream.Write(buffer, 0, bytesRead);
                        }
                        catch (IOException ioex)
                        {
                            print("File Read Error: " + ioex);
                        }
					}
				}
			}
			HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
		}
        
        /// <summary>
        /// Rest API requests issued through this function
        /// </summary>
        /// <param name="_baseurl"></param>
        /// <param name="_urlparams"></param>
        /// <returns></returns>
		static private string restApiReq(string _baseurl, string _urlparams)
		{
			System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
			client.BaseAddress = new System.Uri(_baseurl);
			byte[] cred = UTF8Encoding.UTF8.GetBytes(getAuthKey());
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(cred));
			client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
			// System.Net.Http.HttpContent content = new StringContent(DATA, UTF8Encoding.UTF8, "application/json");
			System.Net.Http.HttpContent content = new StringContent(_urlparams, UTF8Encoding.UTF8, "application/json");
			HttpResponseMessage messge = client.PostAsync(_baseurl, content).Result;
			string description = string.Empty;
			if (messge.IsSuccessStatusCode)
			{
				string result = messge.Content.ReadAsStringAsync().Result;
				description = result;               
				var response = JSON.Parse(description);
				string responseStatus = response["responseStatus"].Value;
				if (responseStatus.Equals("ERROR"))
				{
					description = response["errorCode"].Value;
					print("Error Code: " + description);
                    failedUploads++;
				}      
				else if(responseStatus.Equals("SUCCESS"))
				{
					print(response["responseStatus"].Value);
					if (description.Contains("s3Url"))
					{
						description = response["s3Url"].Value;
						return description;
					}
				}
                else
                {
                    failedUploads++;
                }     
			}
			return null;
		}
	}
}


