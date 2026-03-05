# SombaOpu_Inventory

![SombaOpu_Inventory](https://github.com/user-attachments/assets/2c956ae9-e9bb-4e67-81d5-389fb052f689)

📌 Project Overview  
A custom-built WPF application designed to automate the weighing and logging process. It interfaces directly with digital scales via COM ports, handles sequential task automation, and syncs data to Google Sheets via API.

🚀 Key Features  
- **Auto-COM Detection**: Scans and identifies the correct COM port for scales sending weight data.  
- **Real-Time Data Processing**: Receives raw bits from the scale and converts them into readable weight in grams (g).  
- **Sequential Tray Automation**: Features a chain-list of buttons (trays). The app automatically progresses through the list  
- **Hardware & Network Resilience:**    
  - Scale Monitoring: If the scale is disconnected mid-session, the app pauses and waits for a reconnection before allowing the workflow to continue.  
  - Connectivity Safeguard: Before pushing data to the cloud, the app verifies internet status. If offline, it waits for a stable connection to ensure zero data loss during the Google Sheets sync.  

🧠 Data Recovery (Local Persistence)  
- The application features a built-in Memory system using a local JSON database to ensure no day is left blank in your records:  
  - **Gap Detection:** The app compares the current date with the LastUpdated timestamp in the local database.  
  - **Auto-Backfill:** If a user misses a day (or multiple days), the system automatically retrieves the data from the most recent successful session and copies it forward to the missing dates.  
  - **Consistency:** This ensures that your Google Sheets database remains a continuous, unbroken timeline even if a weighing session was skipped.  

🛠️ Tech Stack  
- **Framework**: .NET / WPF (C#)  
- **Communication**: SerialPort (System.IO.Ports) for RS232/Scale communication.  
- **Persistence**: JSON-based local save system.  
- **Cloud Integration**: Google Sheets API v4.  

⚙️How it Works  
1. **Startup**: The app performs a "handshake" with available COM ports to find the scale.
2. **The Loop**: The user places an item on the scale. Once the app detects a stable weight, it logs the value and moves the "Active" focus to the next tray in the sequence.
3. **Completion**: Once the chain is finished, the app verifies the internet connection and pushes the entire session's data to the cloud.

🔒 Portfolio & Security Notice  
This repository is for demonstration purposes only. To protect private data and cloud resources, the application is locked behind a custom authentication layer.
