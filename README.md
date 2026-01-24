# SOMIOD: Middleware for IoT Interoperability

## Overview
This project implements **SOMIOD** (Service Oriented Middleware for Interoperability and Open Data), a RESTful middleware designed to bridge the gap between heterogeneous IoT devices and applications.  
Developed in the context of the **Systems Integration** course, the goal is to break the "Silo of Things" by standardizing how data is accessed, stored, and notified. The system relies on a **Resource-Based Architecture** (Application, Container, Content-Instance, Subscription) and enables real-time communication via **MQTT**.

To validate the middleware, a **Smart Prison Management System** was developed, consisting of two client applications (Controller and Monitor) that interact through the central API.

---

## System Architecture & Resource Hierarchy
The middleware exposes a hierarchical REST API tree structure to manage IoT resources. All data is persisted in a **SQL Server** database, and notifications are dispatched asynchronously.

- **Application (Root)** – Represents a logical project (e.g., `PrisonSystem`).
- **Container (Module)** – Represents a device or sensor bucket (e.g., `Cell_01`, `Main_Gate`).
- **Content-Instance (Data)** – Atomic data or commands sent to the system (e.g., `OPEN`, `CLOSE`).
- **Subscription (Event)** – Configuration for apps to listen to changes via MQTT or HTTP.

---

## Core Technologies
| Technology | Purpose | Configuration Highlights |
|-------------|----------|---------------------------|
| **ASP.NET Web API** | RESTful Middleware Core | MVC Architecture, XML/JSON Serialization |
| **SQL Server** | Data Persistence | ADO.NET connection, Relational Schema |
| **MQTT (Mosquitto)** | Real-time Notifications | Topics: `[Container]/creation`, `[Container]/update` |
| **Windows Forms** | Client Application UI | Used for both Monitor and Controller apps |
| **XML** | Data Exchange | Primary format for MQTT notification payloads |
| **HTTP/REST** | API Communication | Methods: GET, POST, PUT, DELETE |

---

## API Endpoints & Usage

The SOMIOD middleware provides a comprehensive RESTful API. The system uses a **hierarchical structure** (Application -> Container -> Resources) and a special **Discovery Mechanism** to navigate between them.

### 1. The "SOMIOD Discovery" System
The API uses a custom HTTP Header to distinguish between **getting details of a resource** and **discovering its children**.

* **Header Key:** `somiod-discovery`
* **Header Values:** `application`, `container`, `content-instance`, `subscription`

| Request Type | URL | Header (`somiod-discovery`) | Result |
| :--- | :--- | :--- | :--- |
| **Get Details** | `/api/somiod/MyApp` | *(Empty)* | Returns XML/JSON metadata of "MyApp" (ID, Date, etc). |
| **Discover** | `/api/somiod/MyApp` | `container` | Returns a list of **Containers** inside "MyApp". |
| **Discover** | `/api/somiod/MyApp/Sensors` | `content-instance` | Returns a list of **Data Records** inside "Sensors". |

---

### 2. General & Application Level
Operations related to the API status and managing Applications (the root modules).

| Method | Endpoint | Description | Body / Notes |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/somiod/mystatus` | **System Health Check**. Returns "A API está disponível". | No Body. |
| **GET** | `/api/somiod` | List all Applications. | **Header:** `somiod-discovery: application` |
| **POST** | `/api/somiod` | Create a new Application. | `{ "res-type": "application", "resource-name": "SmartHouse" }` |
| **GET** | `/api/somiod/{appName}` | Get Application metadata. | No Header. |
| **PUT** | `/api/somiod/{appName}` | Update Application name. | `{ "res-type": "application", "resource-name": "NewName" }` |
| **DELETE** | `/api/somiod/{appName}` | Delete Application (and all its contents). | |

---

### 3. Container Level (Modules)
Operations to manage Containers (buckets for data) inside an Application.

| Method | Endpoint | Description | Body / Notes |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/somiod/{appName}` | List Containers in this App. | **Header:** `somiod-discovery: container` |
| **POST** | `/api/somiod/{appName}` | Create a new Container. | `{ "res-type": "container", "resource-name": "Temperature" }` |
| **GET** | `/api/somiod/{appName}/{container}` | Get Container metadata. | No Header. |
| **PUT** | `/api/somiod/{appName}/{container}` | Update Container name. | `{ "res-type": "container", "resource-name": "NewName" }` |
| **DELETE** | `/api/somiod/{appName}/{container}` | Delete Container. | |

---

### 4. Content-Instances (Data)
Operations to create and retrieve actual data records (sensor readings, commands, etc.).
*Note: Data creation is done by POSTing to the parent Container.*

| Method | Endpoint | Description | Body / Notes |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/somiod/{appName}/{container}` | List Data in this Container. | **Header:** `somiod-discovery: content-instance` |
| **POST** | `/api/somiod/{appName}/{container}` | **Create Data Record**. | `{ "res-type": "content-instance", "content": "25C", "content-type": "text" }` |
| **GET** | `/api/somiod/{appName}/{container}/data/{dataName}` | Get specific Data details. | Uses virtual path `/data/`. |
| **DELETE** | `/api/somiod/{appName}/{container}/data/{dataName}` | Delete specific Data record. | Uses virtual path `/data/`. |

---

### 5. Subscriptions (Events)
Operations to configure MQTT or HTTP notifications.
*Note: Subscriptions are created by POSTing to the parent Container, but retrieved via a special path.*

| Method | Endpoint | Description | Body / Notes |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/somiod/{appName}/{container}` | List Subscriptions. | **Header:** `somiod-discovery: subscription` |
| **POST** | `/api/somiod/{appName}/{container}` | **Create Subscription**. | `{ "res-type": "subscription", "evt": "1", "endpoint": "mqtt://..." }` |
| **GET** | `/api/somiod/{appName}/{container}/subscription/{subName}` | Get Subscription details. | Uses virtual path `/subscription/` (or `/subs/`). |
| **DELETE** | `/api/somiod/{appName}/{container}/subscription/{subName}` | Delete Subscription. | Uses virtual path `/subscription/` (or `/subs/`). |

---

## Case Study: Smart Prison System
The functionality of the middleware is demonstrated through a scenario of a Smart Prison, where doors and sensors are managed remotely.

### 1. The Controller App (Producer)
This application simulates the control panel used by prison guards.
- **Function:** It allows the user to send commands to specific cells.
- **Logic:** Checks if the `PrisonSystem` application exists (Self-registration) and sends **POST** requests with payloads like `OPEN` or `CLOSE`.

### 2. The Monitor App (Consumer)
This application simulates the physical security systems (lights/locks) at the cell door.
- **Function:** It listens for commands and updates the UI (Red Light = Closed, Green Light = Open).
- **Logic:** Uses `somiod-discovery` headers to find containers and subscribes to the MQTT broker to react to XML notifications in real-time.

---

## Setup & Execution Guide
To run the SOMIOD ecosystem (Middleware + 2 Apps) locally:

1.  **Database Setup:** Execute the provided SQL script (`TabelasBaseDeDados.sql`) in your local SQL Server instance and update the connection string in `Web.config`.
2.  **MQTT Broker:** Ensure the **Mosquitto** service is running on `127.0.0.1` (Port 1883).
3.  **Launch Middleware:** Open the Solution in **Visual Studio 2022**, set `MiddleWare` as the startup project, and run (F5).
4.  **Run Client Apps:** Start the **MonitorPrisao** and **ControladorPrisao** executables to begin the simulation.

