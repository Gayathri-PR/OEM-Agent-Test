using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class GeminiService
{
    private readonly IHttpClientFactory _http;
    private readonly ToolDispatcher _tools;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(
        IHttpClientFactory http,
        ToolDispatcher tools)
    {
        _http = http;
        _tools = tools;

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new Exception("GEMINI_API_KEY is missing.");

        _model = Environment.GetEnvironmentVariable("GEMINI_MODEL")
            ?? "gemini-2.5-flash";
    }

    public async Task<AgentResult> RunAsync(
        ConversationSession session,
        string message,
        CancellationToken cancellationToken)
    {
        session.Messages.Add(new ChatMessage("user", message));

        var requestBody = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = SystemPrompt
                    }
                }
            },
            ["contents"] = BuildConversation(session),
            ["tools"] = BuildTools()
        };

        var response = await SendAsync(
            requestBody,
            cancellationToken);

        var functionCall = ExtractFunctionCall(response);

        if (functionCall != null)
        {
            var toolName =
                functionCall["name"]?.GetValue<string>();

            var arguments =
                functionCall["args"]?.AsObject()
                ?? new JsonObject();

            if (!string.IsNullOrWhiteSpace(toolName))
            {
                var toolResult = await _tools.ExecuteAsync(
                    toolName,
                    arguments,
                    session,
                    cancellationToken);

                session.LastTool = toolName;

                var followUpContents = BuildConversation(session);

                followUpContents.Add(
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["functionResponse"] = new JsonObject
                                {
                                    ["name"] = toolName,
                                    ["response"] =
                                        JsonSerializer.SerializeToNode(
                                            toolResult)
                                }
                            }
                        }
                    });

                var followUpRequest = new JsonObject
                {
                    ["systemInstruction"] = new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["text"] = SystemPrompt
                            }
                        }
                    },
                    ["contents"] = followUpContents,
                    ["tools"] = BuildTools()
                };

                response = await SendAsync(
                    followUpRequest,
                    cancellationToken);

                var finalText = ExtractText(response)
                    ?? "I couldn't generate a response.";

                session.Messages.Add(
                    new ChatMessage("assistant", finalText));

                return new AgentResult(
                    finalText,
                    toolName);
            }
        }

        var text = ExtractText(response)
            ?? "I couldn't generate a response.";

        session.Messages.Add(
            new ChatMessage("assistant", text));

        return new AgentResult(text, null);
    }

    private JsonArray BuildConversation(
        ConversationSession session)
    {
        var contents = new JsonArray();

        foreach (var message in session.Messages.TakeLast(12))
        {
            var role = message.Role == "assistant"
                ? "model"
                : "user";

            contents.Add(
                new JsonObject
                {
                    ["role"] = role,
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["text"] = message.Text
                        }
                    }
                });
        }

        return contents;
    }

    private JsonArray BuildTools()
    {
        var declarations = _tools.Declarations();

        var declarationsNode =
            JsonSerializer.SerializeToNode(declarations);

        return new JsonArray
        {
            new JsonObject
            {
                ["functionDeclarations"] =
                    declarationsNode?.DeepClone()
            }
        };
    }

    private async Task<JsonObject> SendAsync(
        JsonObject requestBody,
        CancellationToken cancellationToken)
    {
        var client = _http.CreateClient();

        var url =
            $"https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{_model}:generateContent";

        using var request =
            new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("x-goog-api-key", _apiKey);

        request.Content = new StringContent(
            requestBody.ToJsonString(),
            Encoding.UTF8,
            "application/json");

        using var response =
            await client.SendAsync(
                request,
                cancellationToken);

        var raw =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Gemini API error: {raw}");
        }

        return JsonNode.Parse(raw)?.AsObject()
            ?? new JsonObject();
    }

    private static JsonObject? ExtractFunctionCall(
        JsonObject response)
    {
        var parts =
            response["candidates"]?[0]?["content"]?["parts"]
                ?.AsArray();

        if (parts == null)
        {
            return null;
        }

        foreach (var part in parts)
        {
            var functionCall =
                part?["functionCall"]?.AsObject();

            if (functionCall != null)
            {
                return functionCall;
            }
        }

        return null;
    }

    private static string? ExtractText(
        JsonObject response)
    {
        var parts =
            response["candidates"]?[0]?["content"]?["parts"]
                ?.AsArray();

        if (parts == null)
        {
            return null;
        }

        foreach (var part in parts)
        {
            var text =
                part?["text"]?.GetValue<string>();

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private const string SystemPrompt = """
You are an AI conversational assistant for an automotive company.

Help customers through four lifecycle stages.

NEW LEAD:
Answer questions about vehicle models, variants, features and pricing.
For test drives or contact requests, collect full name, phone,
email, city and vehicle model. Then create a lead.

ONGOING PIPELINE:
For existing prospects, use their phone number or deal ID to
retrieve their deal information. Help with test drive status,
quotation status and dealer information.

BOOKED VEHICLE:
For a booking ID or phone number, retrieve booking information
and help with delivery timeline, VIN and payment status.

POST-PURCHASE / SERVICE:
For service requests or complaints, collect registration number,
odometer reading, issue/service type and preferred service center.
Then create a service case.

RULES:
- Do not invent CRM information.
- Use tools when CRM information is required.
- Ask for missing information.
- Keep responses conversational and concise.
- Do not expose internal tool names.
""";
}
