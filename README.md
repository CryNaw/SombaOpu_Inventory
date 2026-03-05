# SombaOpu_Inventory

![SombaOpu_Inventory](https://github.com/user-attachments/assets/2c956ae9-e9bb-4e67-81d5-389fb052f689)

📌 Project Overview  
A custom-built WPF application designed to automate the weighing and logging process for multi-item workflows. It interfaces directly with digital scales via COM ports, handles sequential task automation, and syncs data to Google Sheets via API.

🚀 Key Features  
**Auto-COM Detection**: Scans and identifies the correct COM port for scales sending weight data.  
**Real-Time Data Processing**: Receives raw bits from the scale and converts them into a stabilized, readable weight in grams (g).  
**Sequential Tray Automation**: Features a chain-list of buttons (trays). The app automatically progresses through the list  
**Smart Google Sheets Sync**: Automatically checks for internet connectivity after the final item is weighed. Uses the Google Sheets API to locate the correct file. Maps data to specific cells based on the current Month and Day.  

🛠️ Tech Stack  
**Framework**: .NET / WPF (C#)  
**Communication**: SerialPort (System.IO.Ports) for RS232/Scale communication.  
**Persistence**: JSON-based local save system (to track daily progress and last-updated states).  
**Cloud Integration**: Google Sheets API v4.  

⚙️How it Works  
1. **Startup**: The app performs a "handshake" with available COM ports to find the scale.
2. **The Loop**: The user places an item on the scale. Once the app detects a stable weight (no fluctuation for $X$ milliseconds), it logs the value and moves the "Active" focus to the next tray in the sequence.
3. **Completion**: Once the chain is finished, the app verifies the internet connection and pushes the entire session's data to the cloud, organized by date.


🔒 Portfolio & Security Notice  
This repository is for demonstration purposes only. To protect private data and cloud resources, the application is locked behind a custom authentication layer.
To run this locally, you must provide your own credentials:
1. apikey.json: A Google Cloud Service Account key with "Editor" permissions for your target Sheet.
2. app-config.json: A custom mapping file containing the Sheet IDs and GUIDs for the target Google Drive files.
Note: These files are included in the .gitignore and are not part of this public repository.
