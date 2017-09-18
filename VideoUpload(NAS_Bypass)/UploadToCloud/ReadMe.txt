#The included content is intended to upload videos of the current day to the cloud.

Modify Settings.cfg to point to the directory that videos are stored.

The first line of the cfg file should be in the following format:
   NASDir;C:/Recording_Backup/

Within this folder, the file structure should be as follows:
   YOUR_DIRECTORY/Today's_Date/Session_Name/Stitched_Recordings(5 in total).mp4
Example: 
   C:/Recording_Backup/2016_11_07/HbrhbhOgcsFrNQdsMqzK_897575_11-07-2016_16-07-10/1000092820169661ERS_20161107_EST_AZ204881_SIM.mp4

This file structure is automatically created during the Upload-to-NAS process. Please make sure this location matches the one used there.

The second line of Settings.cfg allows you to set the email address at which the program will send results of the upload
   EmailAddress;<email@example.com>, <email@example.com>
Note that a single email address would look like:
   EmailAddress;<email@example.com>

The last item to add to the config file is the authentication key to be used, such as:
   AuthKey;extsysmursiondvi:1qscZDR%

With the Settings.cfg file configured, and the ICSharpCode DLL in the same directory, run UploadToCloud.exe