using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
public sealed class ZohoService
{
   private readonly IHttpClientFactory _http;
   private string ? _accessToken;
   private DateTimeOffset _tokenExpiry;
   private readonly SemaphoreSlim _tokenLock = new(1, 1);
   private readonly bool _useMock;
   private readonly string _accountsUrl;
   private readonly string _apiUrl;
   public ZohoService(IHttpClientFactory http)
   {
      _http = http;
      _useMock = !string.Equals(Environment.GetEnvironmentVariable("USE_MOCK_CRM"), "false", StringComparison.OrdinalIgnoreCase);
      _accountsUrl = Environment.GetEnvironmentVariable("ZOHO_ACCOUNTS_URL") ?? "https://accounts.zoho.in";
      _apiUrl = Environment.GetEnvironmentVariable("ZOHO_API_URL") ?? "https://www.zohoapis.in/crm/v8";
   }
   public async Task < JsonObject > CreateLeadAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (_useMock)
      {
         return new JsonObject
         {
            ["success"] = true,
            ["module"] = "Leads",
            ["id"] = "MOCK-LEAD-1001",
            ["name"] = arguments["fullName"] ? .GetValue < string > (),
               ["vehicle"] = arguments["vehicleModel"] ? .GetValue < string > (),
               ["message"] = "Lead created successfully."
         };
      }
      var data = new JsonObject
      {
         ["Last_Name"] = arguments["fullName"] ? .GetValue < string > () ?? "Unknown",
            ["Phone"] = arguments["phone"] ? .GetValue < string > (),
            ["Email"] = arguments["email"] ? .GetValue < string > (),
            ["City"] = arguments["city"] ? .GetValue < string > (),
            ["Lead_Source"] = "AI Assistant",
            ["Description"] = $"Vehicle interest: " + $"{arguments["vehicleModel"]?.GetValue<string>()}"
      };
      var body = new JsonObject
      {
         ["data"] = new JsonArray(data)
      };
      return await SendAsync(HttpMethod.Post, "Leads", body, cancellationToken);
   }
   public async Task < JsonObject > SearchDealAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (_useMock)
      {
         var phone = arguments["phone"] ? .GetValue < string > ();
         var dealId = arguments["dealId"] ? .GetValue < string > ();
         if ((phone?.Contains("9876543210") ?? false) || string.Equals(dealId, "DEAL-1001", StringComparison.OrdinalIgnoreCase))
         {
            return new JsonObject
            {
               ["found"] = true,
               ["dealId"] = "DEAL-1001",
               ["customer"] = "Priya Patel",
               ["vehicle"] = "XUV700",
               ["testDrive"] = "Scheduled",
               ["date"] = "2026-09-25 11:00",
               ["dealer"] = "Mahindra Pune",
               ["stage"] = "Test Drive Scheduled"
            };
         }
         return new JsonObject
         {
            ["found"] = false,
            ["message"] = "No matching deal found."
         };
      }
      var phoneValue = arguments["phone"] ? .GetValue < string > ();
      if (!string.IsNullOrWhiteSpace(phoneValue))
      {
         var contactResult = await SearchAsync("Contacts", $"Phone:equals:{phoneValue}", cancellationToken);
         var contactData = contactResult["data"]?.AsArray();
         if (contactData == null || contactData.Count == 0)
         {
            return new JsonObject
            {
               ["found"] = false,
               ["message"] = "No contact found with the provided phone number."
            };
         }
         var contactId = contactData[0]?["id"]?.GetValue < string >();
         if (string.IsNullOrWhiteSpace(contactId))
         {
            return new JsonObject
            {
               ["found"] = false,
               ["message"] = "Contact ID not found."
            };
         }
         var dealResult = await SearchAsync("Deals", $"Contact_Name.id:equals:{contactId}", cancellationToken);
         var deals = dealResult["data"]?.AsArray();
         if (deals == null || deals.Count == 0)
         {
            return new JsonObject
            {
               ["found"] = false,
               ["message"] = "No deals found for the contact."
            };
         }
         var deal = deals[0]?.AsObject();
         return new JsonObject
         {
            ["found"] = true,
            ["dealId"] = deal?["id"]?.GetValue < string > (),
            ["customer"] = deal?["Contact_Name"]?["name"]?.GetValue < string > (),
            ["vehicle"] = deal?["Vehicle_Model"]?.GetValue < string > (),
            ["stage"] = deal?["Stage"]?.GetValue < string > (),
            ["amount"] = deal?["Amount"]?.ToString(),
            ["rawDeal"] = deal?.DeepClone()
         };
      }
      var dealIdValue = arguments["dealId"] ? .GetValue < string > ();
      if (!string.IsNullOrWhiteSpace(dealIdValue))
      {
         return await SendAsync(HttpMethod.Get, $"Deals/{Uri.EscapeDataString(dealIdValue)}", null, cancellationToken);
      }
      return new JsonObject
      {
         ["found"] = false,
         ["message"] = "Phone number or deal ID is required."
      };
   }
   public async Task < JsonObject > GetBookingAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (_useMock)
      {
         var bookingId = arguments["bookingId"] ? .GetValue < string > ();
         if (string.Equals(bookingId, "MAH-9921", StringComparison.OrdinalIgnoreCase))
         {
            return new JsonObject
            {
               ["found"] = true,
               ["bookingId"] = "MAH-9921",
               ["customer"] = "Amit Kumar",
               ["vehicle"] = "Scorpio-N Z8L",
               ["status"] = "In Transit",
               ["delivery"] = "Expected within 7 days",
               ["payment"] = "Balance payment link available"
            };
         }
         return new JsonObject
         {
            ["found"] = false,
            ["message"] = "No matching booking found."
         };
      }
      var module = Environment.GetEnvironmentVariable("ZOHO_BOOKING_MODULE") ?? "Bookings";
      var bookingIdValue = arguments["bookingId"] ? .GetValue < string > ();
      if (!string.IsNullOrWhiteSpace(bookingIdValue))
      {
         return await SearchAsync(module, $"Booking_ID:equals:{bookingIdValue}", cancellationToken);
      }
      return new JsonObject
      {
         ["found"] = false,
         ["message"] = "Booking ID is required."
      };
   }
   public async Task < JsonObject > CreateCaseAsync(JsonObject arguments, CancellationToken cancellationToken)
   {
      if (_useMock)
      {
         return new JsonObject
         {
            ["success"] = true,
            ["module"] = "Cases",
            ["id"] = "MOCK-CASE-2001",
            ["status"] = "Open",
            ["registrationNumber"] = arguments["registrationNumber"] ? .GetValue < string > (),
            ["message"] = "Service case created successfully."
         };
      }
      var registration = arguments["registrationNumber"] ? .GetValue < string > ();
      var serviceType = arguments["serviceType"] ? .GetValue < string > ();
      var odometer = arguments["odometer"]?.ToString();
      var issue = arguments["issue"] ? .GetValue < string > ();
      var serviceCenter = arguments["serviceCenter"] ? .GetValue < string > ();
      var data = new JsonObject
      {
        ["Subject"] = $"Service request - {registration}",
        ["Case_Origin"] = "AI Assistant",
        ["Status"] = "Open",
        ["Description"] = $"Service type: {serviceType};" + $"Odometer: {odometer}; " + $"Issue: {issue}; " + $"Preferred service centre: {serviceCenter}",
      };
      var body = new JsonObject
      {
         ["data"] = new JsonArray(data)
      };
      return await SendAsync(HttpMethod.Post, "Cases", body, cancellationToken);
   }
   private async Task < JsonObject > SearchAsync(string module, string criteria, CancellationToken cancellationToken)
   {
      var path = $"{module}/search?criteria=" + Uri.EscapeDataString(criteria);
      return await SendAsync(HttpMethod.Get, path, null, cancellationToken);
   }
   private async Task < JsonObject > SendAsync(HttpMethod method, string path, JsonObject ? body, CancellationToken cancellationToken)
   {
      var token = await GetAccessTokenAsync(cancellationToken);
      var client = _http.CreateClient();
      using
      var request = new HttpRequestMessage(method, $"{_apiUrl.TrimEnd('/')}/{path}");
      request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
      if (body != null)
      {
         request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
      }
      using
      var response = await client.SendAsync(request, cancellationToken);
      var raw = await response.Content.ReadAsStringAsync(cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
         throw new Exception($"Zoho API error: {raw}");
      }
      return JsonNode.Parse(raw) ? .AsObject() ?? new JsonObject();
   }
   private async Task < string > GetAccessTokenAsync(CancellationToken cancellationToken)
   {
      if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-2))
      {
         return _accessToken;
      }
      await _tokenLock.WaitAsync(cancellationToken);
      try
      {
         if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-2))
         {
            return _accessToken;
         }
         var clientId = Environment.GetEnvironmentVariable("ZOHO_CLIENT_ID");
         var clientSecret = Environment.GetEnvironmentVariable("ZOHO_CLIENT_SECRET");
         var refreshToken = Environment.GetEnvironmentVariable("ZOHO_REFRESH_TOKEN");
         if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(refreshToken))
         {
            throw new Exception("Zoho OAuth settings are missing.");
         }
         var form = new Dictionary < string,
            string >
            {
               ["refresh_token"] = refreshToken,
               ["client_id"] = clientId,
               ["client_secret"] = clientSecret,
               ["grant_type"] = "refresh_token"
            };
         var client = _http.CreateClient();
         using
         var response = await client.PostAsync($"{_accountsUrl.TrimEnd('/')}/oauth/v2/token", new FormUrlEncodedContent(form), cancellationToken);
         var raw = await response.Content.ReadAsStringAsync(cancellationToken);
         if (!response.IsSuccessStatusCode)
         {
            throw new Exception($"Zoho OAuth error: {raw}");
         }
         var json = JsonNode.Parse(raw) ? .AsObject();
         _accessToken = json ? ["access_token"] ? .GetValue < string > ();
         if (string.IsNullOrWhiteSpace(_accessToken))
         {
            throw new Exception("Zoho did not return an access token.");
         }
         var expiresIn = json ? ["expires_in"] ? .GetValue < int > () ?? 3600;
         _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
         return _accessToken;
      }
      finally
      {
         _tokenLock.Release();
      }
   }
}
