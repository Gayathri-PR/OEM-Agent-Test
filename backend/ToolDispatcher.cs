using System.Text.Json.Nodes;
public sealed class ToolDispatcher
{
   private readonly ZohoService _zoho;
   public ToolDispatcher(ZohoService zoho)
   {
      _zoho = zoho;
   }
   public object[] Declarations()
   {
      return new object[]
      {
         new
         {
            name = "get_vehicle_info",
               description = "Get vehicle information for a model or variant.",
               parameters = new
               {
                  type = "object",
                     properties = new
                     {
                        model = new
                           {
                              type = "string"
                           },
                           variant = new
                           {
                              type = "string"
                           }
                     },
                     required = new []
                     {
                        "model"
                     }
               }
         },
         new
         {
            name = "create_lead",
               description = "Create a new automotive sales lead.",
               parameters = new
               {
                  type = "object",
                     properties = new
                     {
                        fullName = new
                           {
                              type = "string"
                           },
                           phone = new
                           {
                              type = "string"
                           },
                           email = new
                           {
                              type = "string"
                           },
                           city = new
                           {
                              type = "string"
                           },
                           vehicleModel = new
                           {
                              type = "string"
                           }
                     },
                     required = new []
                     {
                        "fullName",
                        "phone",
                        "email",
                        "city",
                        "vehicleModel"
                     }
               }
         },
         new
         {
            name = "search_deal",
               description = "Search for an existing sales deal using phone number or deal ID.",
               parameters = new
               {
                  type = "object",
                     properties = new
                     {
                        phone = new
                           {
                              type = "string"
                           },
                           dealId = new
                           {
                              type = "string"
                           }
                     }
               }
         },
         new
         {
            name = "get_booking",
               description = "Retrieve an existing vehicle booking.",
               parameters = new
               {
                  type = "object",
                     properties = new
                     {
                        bookingId = new
                           {
                              type = "string"
                           },
                           phone = new
                           {
                              type = "string"
                           }
                     }
               }
         },
         new
         {
            name = "create_service_case",
               description = "Create a post-purchase automotive service case.",
               parameters = new
               {
                  type = "object",
                     properties = new
                     {
                        registrationNumber = new
                           {
                              type = "string"
                           },
                           odometer = new
                           {
                              type = "number"
                           },
                           serviceType = new
                           {
                              type = "string"
                           },
                           issue = new
                           {
                              type = "string"
                           },
                           serviceCenter = new
                           {
                              type = "string"
                           }
                     },
                     required = new []
                     {
                        "registrationNumber",
                        "odometer",
                        "serviceType",
                        "serviceCenter"
                     }
               }
         }
      };
   }
   public async Task < JsonObject > ExecuteAsync(string name, JsonObject arguments, ConversationSession session, CancellationToken cancellationToken)
   {
      switch (name)
      {
         case "get_vehicle_info":
            session.LifecycleStage = "NEW_LEAD";
            var model = arguments["model"] ? .GetValue < string > () ?? "";
            var variant = arguments["variant"] ? .GetValue < string > ();
            return VehicleCatalog.Get(model, variant);
         case "create_lead":
            session.LifecycleStage = "NEW_LEAD";
            return await _zoho.CreateLeadAsync(arguments, cancellationToken);
         case "search_deal":
            session.LifecycleStage = "ONGOING_PIPELINE";
            return await _zoho.SearchDealAsync(arguments, cancellationToken);
         case "get_booking":
            session.LifecycleStage = "BOOKED_VEHICLE";
            return await _zoho.GetBookingAsync(arguments, cancellationToken);
         case "create_service_case":
            session.LifecycleStage = "POST_PURCHASE_SERVICE";
            return await _zoho.CreateCaseAsync(arguments, cancellationToken);
         default:
            return new JsonObject
            {
               ["success"] = false,
               ["error"] = "Unknown tool."
            };
      }
   }
}
public static class VehicleCatalog
{
   private static readonly
   Dictionary < string, JsonObject > Vehicles = new(StringComparer.OrdinalIgnoreCase)
   {
      ["Thar"] = new JsonObject
      {
         ["model"] = "Thar",
         ["variants"] = new JsonArray("AX", "LX"),
         ["fuel"] = new JsonArray("Petrol", "Diesel")
      },
      ["XUV700"] = new JsonObject
      {
         ["model"] = "XUV700",
         ["variants"] = new JsonArray("MX", "AX3", "AX5", "AX7"),
         ["fuel"] = new JsonArray("Petrol", "Diesel")
      },
      ["Scorpio-N"] = new JsonObject
      {
         ["model"] = "Scorpio-N",
         ["variants"] = new JsonArray("Z2", "Z4", "Z6", "Z8", "Z8L"),
         ["fuel"] = new JsonArray("Petrol", "Diesel")
      }
   };
   public static JsonObject Get(string model, string ? variant)
   {
      if (!Vehicles.TryGetValue(model, out
            var vehicle))
      {
         return new JsonObject
         {
            ["found"] = false,
            ["message"] = "Vehicle was not found in the demo catalog."
         };
      }
      var result = vehicle.DeepClone().AsObject();
      result["found"] = true;
      if (!string.IsNullOrWhiteSpace(variant))
      {
         result["requestedVariant"] = variant;
      }
      return result;
   }
}
