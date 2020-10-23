using BasicallyMe.RobinhoodNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RhAutoHSOTP.classes
{
    class RhLogins
    {
        static string __tokenFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CONSTANTS.TOKEN_FILE_FOLDER,
            CONSTANTS.TOKEN_FILE_NAME);

        static string __appFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            CONSTANTS.AUTH_APP_FOLDER,
            CONSTANTS.AUTH_APP_FILE_NAME);

        public static async Task authenticate(RobinhoodClient client)
        {
            /// See if we need a new token file - either none exists or file is over X hours old
            bool shouldGetNewTokenFile = !System.IO.File.Exists(__tokenFile)
                || !IsTokenFileAgeLessThan(CONSTANTS.TOKEN_FILE_MAX_AGE_HOURS);


            if (shouldGetNewTokenFile)
                await GenerateTokenFile().ConfigureAwait(continueOnCapturedContext: false); ;
                //startApp();  /// Run a console app to create a token file

            /// If the file exists  and it's less than X hours old
            if (System.IO.File.Exists(__tokenFile)
                && IsTokenFileAgeLessThan(CONSTANTS.TOKEN_FILE_MAX_AGE_HOURS))
            {
                /// Read the access token from the token file
                var token = System.IO.File.ReadAllText(__tokenFile);

                /// Use the access token to authenticate 
                await client.Authenticate(token);
            }
 
        }

        static bool IsTokenFileAgeLessThan(int nHours)
        {
            var ageOfTokenFile = (DateTime.Now - System.IO.File.GetLastWriteTime(__tokenFile)).TotalHours;
            return ageOfTokenFile < nHours;
        }

        static async Task GenerateTokenFile()
        {
            //Console.Write("username: ");

            var rh = new RobinhoodClient();

            await rh.Authenticate(CONSTANTS.USER_NAME, CONSTANTS.PASSWORD).ConfigureAwait(continueOnCapturedContext: false); ;

            System.IO.Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(__tokenFile));

            System.IO.File.WriteAllText(__tokenFile, rh.AuthToken);

        }


        private static void startApp()
        {
            string command = " -h"; //common help flag for console apps
            System.Diagnostics.Process pRun;
            pRun = new System.Diagnostics.Process();
            pRun.EnableRaisingEvents = true;
            pRun.Exited += new EventHandler(pRun_Exited);
            pRun.StartInfo.FileName = __appFile;
            pRun.StartInfo.Arguments = command;
            pRun.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

            pRun.Start();
            pRun.WaitForExit();
        }

        private static void pRun_Exited(object sender, EventArgs e)
        {
            //Do Something Here
        }

    }
}
