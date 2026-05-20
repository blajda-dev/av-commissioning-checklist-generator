# Commissioning Checklist Generator

## Overview
This application is intended to streamline the process of comissioning av integration projects by nearly automatically generating a list of commissioning tasks for devices present in a system. 

Provided with a DXF or DWG file of a line drawing, it will parse any available devices from the system that can be commissioned by a Field Engineer or Quality Assurance Engineer and generate an Excel checklist containing pertinent tasks for that device. The application utilizes a SQLite database to determine what a device is capable of.

For example, a system expander *(denoted by the prefix **EXP**)* can be capable of expanding the audio, video, **or** control aspects of a system. Without knowing what type of expander is installed, the generator will retrieve all tasks a generic **EXP** device is capable of. By using the database, we can narrow down the capability of the device using it's checking the **model** of the device against the database. If the model of the expander in question is a QSC QIO-S4 for example, only the **control** related commissioning tasks will be retrieved, as that is the capabilities of a QIO-S4 as defined in the database.


## Configuration

### Application Settings

In the toolbar you will need to open the Settings window to configure the server url to download the remote database. An example database is hosted at: https://blajda-gen2.loginto.me. Before closing the settings window, save the configuration and this URL will be used in the future. You'll only need to do this once on startup or if the file ever gets deleted somehow.

### Database Configuration

SQLite scripts to get a new database fired up on your server can be found in the docs folder. A diagram of the relationships is also provided

### CAD Drawing Block Configuration

The blocks in your drawings **MUST** have the following attribute tags. These are used to determine what inserts in the CAD drawing are actually commissionable devices. Without this the application cannot 
parse out what devices it cares about, nor can it later on query the database to retrieve appropriate tasks later on when you try to generate the checklist.

- ID
	- a unique identifier for the device
		- CPR-101, MON-001, TPL-010 etc
	- this **MUST** be in the following format
		- XXX-NNN
			- XXX is an abbreviation for the device
				- CPR, MON, TPL
			- NNN is a numeric identifier of the instance of the unit in the system
				- 101, 001
			- see source code for regex pattern
	- *the prefixes used here **MUST** exist in the database to retrieve device **PREFIX** commissioning tasks*
- MAKE / MFG
	- the manufacturer of the device
	- this is just part of the database relationships, not used when generating tasks
- MODEL / PN
	- the model or part number of the device
	- when a checklist is generated, the application checks to see if a device matching the model exists. 
		- *if commisioning tasks have been assigned at the device **MODEL** level, these override any device **PREFIX** commissioning tasks*
- DESCRIPTION / DESC
	- not as important, but injected into the checklist in the device's checklist section

## Usage

### Startup

The application will open the settings window when you run it for the first time prompting you to configure the server url that is hosting the sqlite database. If you dont configure this, youll never download updates and only have the embedded version of the database that the app was shipped with, which is limited in is functionality at this time. Configure this url to point at your server. 

*(there is a field that auto updates to indicate where the final file should be located on the server, as you input the server URL)*

### Toolbar

There is a toolbar with 3 options:

- Help
	- opens a small message box that hopefully provides assistance in using the app
- Settings
	- allows for you to adjust the server URL that hosts the SQLite database
- Download Database
	- this is a manual override to immediately download a database update
	- use this as needed, by default the app will auto-download every hour while the app is running.
	- a progress window will indicate download progress, and disable the download button until complete

### Generating Checklists

#### Step 1 - Import Devices

To begin you'll need to import the devices from the system that can be configured. This can be done one of two ways, using the drawing parser, or an existing system
configuration file.

1. Import CAD File
	1. find the system DWG or DXF file from the CAD folder, create a copy and import into the application.
1. Import JSON File
	1. if you have used the generator previously on an identical system or are generating them for multiple rooms in a system you can import the json file, which will be quicker than parsing the DXF drawing file.
	
#### Step 2 - System Capabilities

After you have imported the devices, you need to indicate what other capabilities the system has. There are 4 options:

- Audio Conferencing
	- conferencing using a traditional POTS or VOIP interface
- Video Conferencing
	- conferencing using a traditional hard codec
- Soft Conferencing
	- conferencing using an installed pc, or byod device running Microsoft Teams, Zoom, Webex, etc.
- Room Combining
	- either master/slave or true combining functionality across multiple rooms

#### Step 3 - Save System Configuration

If you want to save the system configuration for re-use later, you can export the system devices and functionality to a json file that you can re-use later. Parsing the DXF file can take some time,
so using the configuration file will save time as it reads in the file directly. This step is optional.

#### Step 4 - Generate Checklist

Finally, you can generate the checklist. This can take some time, so a progress window will show you the current step. Once completed, a file dialog will open to prompt you to save the excel checklist
to disk with the project number so that field engineers or project managers can easily identify the file.

#### Step 5 - Profit ?

After this, its up to you, either complete or hand off the checklist to the QA or Field Engineer

## Checklist

The checklist is has pre-built conditional formatting that will colorize the row based on the completion status of the task, and is  broken out into a few worksheets to prevent visual overload:

- Sources
	- any audio or video device that acts as an input endpoint of content into the system
- Destinations
	- any audio or video device that acts as an output endpoing of content from the sytem
- User Interfaces
	- button panels, keypads, touchpanels, ipads, etc.
- Controlled Devices
	- any other device that doesnt fall into the previous 3 categories
- Soft Conferencing
	- tasks specific to using Teams, Zoom, Webex, etc
- Video Conferencing
	- tasks specific to hard codecs, Cisco, etc.
- Audio Conferencing
	- tasks specific to POTS or VOIP
- Room Combining
	- tasks specific to systems capable of combining

These worksheets will be generated automatically, or left out if no matching devies are found with such capability to warrant generating the worksheet

## Troubleshooting

The database, configuration, and logs are all stored in here (type into a file explorer) -> %LOCALAPPDATA%/CommissioningChecklistGenerator/

The application should handle all exceptions, and output them in the logs, or show a messagebox if something goes terribly wrong.