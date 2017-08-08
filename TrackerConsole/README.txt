How to launch Sarsoft and hook up the tracking receiver to show realtime tracks.
NOTE: You need a USB flash drive with the Sarsoft app, map tiles, and TrackerConsole app on it. 
NOTE: If you have a copy of Sarsoft, map tiles, and TrackerDownloder already on your local computer then use that in place of "Sarsoft USB" in these instructions.

START SARSOFT:
Sarsoft directory from the Sarsoft USB or from your local Sarsoft folder if you have it.
Run sarsoft.bat (Windows Batch File)
NOTE: A CMD window will open. You can minimize it.

OPEN MAP:
Open a Browser
Enter http://localost:8080 in the address bar
NOTE: It takes a few moments for the server to start. Your browser may say the site is unavailable until it's finished starting.
NOTE: Sarsoft map opens in browser
NOTE: It may take a few mins to load the map tiles from the USB drive
Add a new Marker somewhere on the map (like CommandPost)
Save the map (give it a handy name, such as the mission number and name (i.e. 17-1234_LostHikerMailboxPeak))
NOTE: Maps save to the Sarsoft folder on the USB drive or local Sarsoft directory

CONNECT ALPHA 100:
NOTE: Garmin USB drivers must be installed on the computer.
Connect Garmin Alpha 100 GPS to the computer
Set device in Basestation mode

START TRACK DOWNLOADER:
Go to Sarsoft folder and open TrackDownloader folder
Run TrackDownloader.exe
Notice a CMD window opens showing the devices it detects.
NOTE: The first number of the line is the Tracking Collar ID Number

ADD TRACKS TO SARSOFT:
In Sarsoft map goto Add New Object and choose Add Locator
	Type: APRS
	Label: Whatever you want (like Team number)
	Callsign: Enter a device ID from the TrackerConsole window
Need to refresh Sarsoft in the browser after adding the first Locator object (from here on new locators will autopopulate)
Add new Locator object for each device listed in TrackerConsole CMD window
Tracks should now be showing on the map
You can edit names and colors of each team in the Locators folder
NOTE: Locators shows the track of each device. These are the teams you are tracking. The Locator Groups is the request to track a Device and can be minimized. 
NOTE: Team tracks are NOT part of the map, and will not be included if you export the map.

*** IMPORTANT NOTE *** The tracks from each team are NOT SAVED to the map! You will either need to download tracks from each team's personal GPS when they return, or you will need to convert the Track log in the TrackerConsole folder.
