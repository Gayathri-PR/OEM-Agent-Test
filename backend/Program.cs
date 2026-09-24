using System.Collections.Concurrent;
using System.Text.Json.Nodes;
var builder = WebApplication.CreateBuilder(args);
// Load values from backend/.env
DotEnv.Load(".env");
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
   options.AddDefaultPolicy(policy =>
   {
      policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
   });
});
builder.Services.AddSingleton < ConversationStore > ();
builder.Services.AddSingleton < ZohoService > ();
builder.Services.AddSingleton < ToolDispatcher > ();
builder.Services.AddSingleton < GeminiService > ();
var app = builder.Build();
app.UseCors();
app.MapGet("/api/health", () =>
{
   return Results.Ok(new
   {
      status = "ok"
   });
});
app.MapPost("/api/chat", async (ChatRequest request, ConversationStore store, GeminiService gemini, CancellationToken cancellationToken) =>
{
   if (string.IsNullOrWhiteSpace(request.Message))
   {
      return Results.BadRequest(new
      {
         error = "Message is required."
      });
   }
   var session = store.Get(request.SessionId);
   session.Messages.Add(new ChatMessage("user", request.Message));
   try
   {
      var result = await gemini.RunAsync(session, request.Message, cancellationToken);
      session.Messages.Add(new ChatMessage("assistant", result.Text));
      if (!string.IsNullOrWhiteSpace(result.ToolName))
      {
         session.LastTool = result.ToolName;
      }
      return Results.Ok(new
      {
         sessionId = session.Id,
            response = result.Text,
            tool = result.ToolName,
            lifecycleStage = session.LifecycleStage
      });
   }
   catch (Exception ex)
   {
      return Results.Ok(new
      {
         sessionId = session.Id,
            response = "I'm sorry, I couldn't complete that request right now. " + "No CRM change was confirmed. Please try again.",
            error = ex.Message
      });
   }
});
app.Run();
public record ChatRequest(string SessionId, string Message);
public record ChatMessage(string Role, string Text);
public record AgentResult(string Text, string ? ToolName);
public sealed class ConversationSession
{
   public string Id
   {
      get;
      init;
   } = Guid.NewGuid().ToString("N");
   public string ? LifecycleStage
   {
      get;
      set;
   }
   public string ? LastTool
   {
      get;
      set;
   }
   public List < ChatMessage > Messages
   {
      get;
   } = new();
   public Dictionary < string, JsonNode ? > State
   {
      get;
   } = new();
}
public sealed class ConversationStore
{
   private readonly ConcurrentDictionary < string,
      ConversationSession > _sessions = new();
   public ConversationSession Get(string ? id)
   {
      if (string.IsNullOrWhiteSpace(id))
      {
         id = Guid.NewGuid().ToString("N");
      }
      return _sessions.GetOrAdd(id, key => new ConversationSession
      {
         Id = key
      });
   }
}
public static class DotEnv
{
   public static void Load(string file)
   {
      if (!File.Exists(file))
      {
         return;
      }
      foreach(var line in File.ReadAllLines(file))
      {
         var value = line.Trim();
         if (value.Length == 0 || value.StartsWith("#") || !value.Contains("="))
         {
            continue;
         }
         var parts = value.Split('=', 2);
         var key = parts[0].Trim();
         var setting = parts[1].Trim().Trim('"');
         Environment.SetEnvironmentVariable(key, setting);
      }
   }
}
