# Automotive Conversational AI Assistant

A conversational AI assistant for an automotive customer journey\. It uses Gemini for conversation and tool calling, a React frontend, a \.NET 8 backend, and Zoho CRM for real CRM operations\.

## What it can do

The assistant supports four main customer stages:

1. **New Lead**
   - Answer basic vehicle questions\.
   - Handle test\-drive requests\.
   - Collect customer details\.
   - Create a Lead in Zoho CRM\.
2. **Ongoing Pipeline**
   - Find an existing customer using their phone number\.
   - Find the related Deal in Zoho CRM\.
   - Show vehicle and deal\-stage information\.
3. **Booked Vehicle**
   - Handle booking and delivery\-related queries where the required CRM information is available\.
4. **Post\-Purchase / Service**
   - Collect registration number, odometer reading, issue/service type and preferred service centre\.
   - Create a Case in Zoho CRM\.

## Tech Stack

- **Frontend:** React, TypeScript
- **Backend:** \.NET 8 / C\#
- **LLM:** Google Gemini
- **CRM:** Zoho CRM
- **Authentication:** Zoho OAuth 2\.0
- **Development:** GitHub Codespaces

## Project Structure

```text
.
├── backend/
│   ├── Program.cs
│   ├── GeminiService.cs
│   ├── ToolDispatcher.cs
│   └── ZohoService.cs
│
├── frontend/
│   └── src/
│       ├── main.tsx
│       └── style.css
│
└── README.md
```

## How it works

The frontend sends each user message to the \.NET backend\.

The backend keeps the conversation session and sends the conversation to Gemini\. Gemini decides whether it can answer directly or needs to use one of the available tools\.

The `ToolDispatcher` routes the requested operation to the appropriate service\.

For CRM operations, `ZohoService` communicates with Zoho CRM using OAuth access tokens\.

For example, an existing customer lookup works like this:

```text
Customer phone number
        ↓
Search Contact in Zoho
        ↓
Get Contact ID
        ↓
Search Deals using Contact_Name
        ↓
Return Deal information
```

For a service request:

```text
Customer service request
        ↓
Collect required details
        ↓
Create Case in Zoho CRM
        ↓
Return confirmation to customer
```

## Setup

### Backend

```bash
cd backend
dotnet build
dotnet run --urls http://0.0.0.0:5000
```

### Frontend

```bash
cd frontend
npm install
npm run dev -- --host 0.0.0.0
```

## Environment Variables

Create a `.env` file in the `backend` folder:

```env
GEMINI_API_KEY=YOUR_GEMINI_KEY
GEMINI_MODEL=gemini-2.5-flash

USE_MOCK_CRM=false

ZOHO_CLIENT_ID=YOUR_CLIENT_ID
ZOHO_CLIENT_SECRET=YOUR_CLIENT_SECRET
ZOHO_REFRESH_TOKEN=YOUR_REFRESH_TOKEN

ZOHO_ACCOUNTS_URL=https://accounts.zoho.in
ZOHO_API_URL=https://www.zohoapis.in/crm/v8
```

Do not commit the `.env` file or any API keys/secrets to GitHub\.

## Zoho OAuth

The application uses a Zoho OAuth refresh token to obtain access tokens for CRM API calls\.

The Zoho client needs permission to work with the CRM modules used by the application, including Leads, Contacts, Deals and Cases\.

## Demo Scenarios

### New Lead

Ask about a vehicle and request a test drive\. Provide the customer’s name, phone, email, city and vehicle model\. A Lead is created in Zoho CRM\.

### Vehicle Information

Ask about a vehicle such as the XUV700\. The assistant can provide available variants and fuel options from the demo vehicle catalog\.

### Existing Prospect

Provide the phone number of an existing Contact\. The assistant finds the Contact and then retrieves the associated Deal from Zoho CRM\.

### Service Request

Request periodic maintenance or report a service issue\. Provide the registration number, odometer reading, service type/issue and preferred service centre\. A Case is created in Zoho CRM\.

## Notes

- The project uses real Zoho CRM data when `USE_MOCK_CRM=false`\.
- Gemini handles the conversation and decides when a tool is required\.

- <img width="977" height="747" alt="image" src="https://github.com/user-attachments/assets/964f8579-e7eb-4ef5-ba24-331bed0040c7" />

- CRM information is retrieved through backend tools rather than being invented by the assistant\.
- API keys, OAuth secrets and refresh tokens should be kept out of source control\.
