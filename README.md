﻿# 🚀 SQL Server Automated Backup Suite

A professional Windows Service-based solution for automated SQL Server database backups. This repository demonstrates two distinct architectural for handling scheduled tasks in Windows, both designed for high reliability and automated deployment.

---

## 📁 Repository Structure

* **`/Timer-Based-Service`**: Traditional architecture using `System.Threading.Timer` for continuous background monitoring and execution.
* **`/Task-Scheduler-Service`**: Modern hybrid approach combining a **Manual Windows Service** with the **Windows Task Scheduler API** for peak resource efficiency.

---

## 🛠 Tech Stack

* **Language:** C# (.NET Framework)
* **Database:** SQL Server (T-SQL)
* **APIs:** Microsoft.Win32.TaskScheduler, System.ServiceProcess, System.Configuration
* **Deployment:** Microsoft Visual Studio Installer Projects (MSI)

---

## 🏗 Architectural Overview

### 1️⃣ Timer-Based Approach
* **Execution**: The service remains in a `Running` state 24/7.
* **Trigger**: An internal timer triggers the backup logic at defined intervals.
* **Failure Policy**: Configured with an automatic restart policy via `sc failure` command during installation to ensure the service resumes if it crashes.

### 2️⃣ Task Scheduler Hybrid Approach (Auto-Provisioned)
* **Automatic Setup**: Upon installation, the `ProjectInstaller` automatically registers a new task in the **Windows Task Scheduler**.
* **Resource Efficiency**: The service is set to `Manual` startup. The Task Scheduler "wakes up" the service at the scheduled time, and the service terminates itself using `this.Stop()` once the backup is complete.
* **Failure Policy**: Both the Windows Service and the Scheduled Task include recovery actions. The service is configured with `sc failure` reset actions to restart automatically upon unexpected termination.

---

## 🚀 Key Features & Solutions

* **Automated Task Injection**: The Task Scheduler version programmatically injects the backup schedule into the Windows OS during the `OnAfterInstall` event, ensuring the system is ready immediately after deployment.
* **Dynamic Interval Configuration**: All backup settings, including database intervals and connection strings, are fully dynamic. Users can update frequency or targets directly via the `App.config` file without needing to recompile or reinstall the service.
* **Optimized Execution**: Utilizes `ProcessPriorityClass.BelowNormal` to manage system resources effectively, ensuring backup operations run in the background without affecting the performance of other critical server applications.
* **Resilient Failure Policy**: Both versions are equipped with a robust recovery policy (`sc failure`) that automatically restarts the service in case of unexpected crashes, ensuring continuous data protection.
---

## 📝 Installation & Deployment

1.  **Clone** the repository.
2.  **Configure** the `App.config` with your `ConnectionString`, `BackupFolder`, and `LogFolder`.
3.  **Build** the solution in Release mode.
4.  **Rebuild** the **Setup Project** to generate the `.msi` package.
5.  **Install** the MSI as Administrator. The Task Scheduler and Failure Policies will be configured automatically.
> **Note**: This project is configured to run in **Console Mode** by default to allow immediate testing and debugging. To deploy as a formal service, use the provided Setup Project.

---
🔗 **Connect with me:**
* [GitHub Profile](https://github.com/KhaledAbuAl-Majd)
* [LinkedIn Profile](https://www.linkedin.com/in/khaledabualmajd1)

