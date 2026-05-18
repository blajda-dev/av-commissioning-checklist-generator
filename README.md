# Commissioning Checklist Generator

## Overview
A nearly complete revamp of the old generator. This new application will take a DXF or DWG file of a line drawing, and parse any available 
devices from the system that can be commissioning by a field engineer or quality assurance engineer and generate an excel checklist 
containing pertinent tasks for that device, using a database of functionality that the device should/could be capable of. If the device
has a model defined in the database that functionality will override any basic prefix-based functionality. 

For example, a system expander (EXP) can be capable of expanding the audio, video, or control aspects of a system. without knowing what type
of expander is installed, all tasks are valid for a generic expander. However, if the model of expander is specified in the database (for example a QSC QIO-S4) 
only the control related commissioning tasks will be retrieved, as this type of expander is only capable of serial control, not video or audio.


## Configuration

In the toolbar you will need to open the Settings window to configure the server url to download the remote database. currently this database is hosted at: https://blajda-gen2.loginto.me 
this url will then be baked into a configuration used by the application. You'll only need to do this once on startup or if the file ever gets deleted somehow. 
The application should prompt you for this when you run it for the first time.

## Usage

### Import Devices

#### Step 1

To begin you'll need to import the devices from the system that can be configured. This can be done one of two ways, using the drawing parser, or an existing system
configuration file.


1. Import CAD File
	1. find the system DWG or DXF file from the CAD folder, create a copy and import into the application.
1. Import JSON File
	1. if you have used the generator previously on an identical system or are generating them for multiple rooms in a system you can import the json file, which will be quicker than parsing the DXF drawing file.
	
#### Step 2

After you have imported the devices, you need to indicate what other capabilities the system has. There are 4 options:

- Audio Conferencing
	- conferencing using a traditional POTS or VOIP interface
- Video Conferencing
	- conferencing using a traditional hard codec
- Soft Conferencing
	- conferencing using an installed pc, or byod device running Microsoft Teams, Zoom, Webex, etc.
- Room Combining
	- either master/slave or true combining functionality across multiple rooms

#### Step 3

If you want to save the system configuration for re-use later, you can export the system devices and functionality to a json file that you can re-use later. Parsing the DXF file can take some time,
so using the configuration file will save time as it reads in the file directly. This step is optional.

#### Step 4

Finally, you can generate the checklist. This can take some time, so a progress window will show you the current step. Once completed, a file dialog will open to prompt you to save the excel checklist
to disk with the project number so that field engineers or project managers can easily identify the file.

### Step 5


profit?

## Troubleshooting

The database, configuration, and logs are all stored in here (type into a file explorer) -> %LOCALAPPDATA%/CommissioningChecklistGenerator/

The application should handle all exceptions, and output them in the logs, or show a messagebox if something goes terribly wrong.