Place these files in the Mursion software folder at the path below:
   C:\Program Files (x86)\MursionSimSpETS\_SupportSoftware\NovaCam

Edit Mursion_Upload-to-NAS.ps1
Change line 16 to point to your NAS directory (or a location on your hard drive if you do not have a NAS)

Example:
   $NASDirectory = "C:/Recording_Backup/"

Save the file and run Begin-Tasks.bat

This will first run post-processing (video stitching) on the videos and then copy them to the NASDirectory location.

--Following this, run the executable for uploading to the cloud, ensuring that Settings.cfg has the same path as $NASDirectory here--