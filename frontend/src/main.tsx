import React, { useEffect, useRef, useState } from "react";
import { createRoot } from "react-dom/client";
import "./style.css";

type Msg = { role: "user" | "assistant"; text: string };
const sessionId = crypto.randomUUID();

function App() {
  const [messages, setMessages] = useState<Msg[]>([
    {
      role: "assistant",
      text: "Hi! I can help with vehicle discovery, test-drive/deal status, bookings, and service requests. What would you like to do?",
    },
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const bottom = useRef<HTMLDivElement>(null);
  useEffect(() => {
    bottom.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);
  async function send() {
    const text = input.trim();
    if (!text || loading) return;
    setInput("");
    setMessages((m) => [...m, { role: "user", text }]);
    setLoading(true);
    try {
      const r = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId, message: text }),
      });
      const d = await r.json();
      setMessages((m) => [
        ...m,
        { role: "assistant", text: d.response || "Something went wrong." },
      ]);
    } catch {
      setMessages((m) => [
        ...m,
        {
          role: "assistant",
          text: "I could not reach the backend. Please check that the API is running.",
        },
      ]);
    } finally {
      setLoading(false);
    }
  }
  return (
    <div className="page">
      <div className="card">
        <header>
          <div>
            <div className="eyebrow">ABC AUTOMOTIVE</div>
            <h1>Customer Assistant</h1>
            <p>Sales • Booking • Service</p>
          </div>
          <span className="dot">● Online</span>
        </header>
        <main>
          {messages.map((m, i) => (
            <div key={i} className={"row " + m.role}>
              <div className="bubble">{m.text}</div>
            </div>
          ))}
          {loading && (
            <div className="row assistant">
              <div className="bubble typing">Thinking…</div>
            </div>
          )}
          <div ref={bottom} />
        </main>
        <div className="chips">
          {[
            "Tell me about the Thar",
            "Check booking MAH-9921",
            "When is my XUV700 test drive?",
            "I need a service",
          ].map((x) => (
            <button key={x} onClick={() => setInput(x)}>
              {x}
            </button>
          ))}
        </div>
        <footer>
          <input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && send()}
            placeholder="Type your message…"
          />
          <button className="send" onClick={send} disabled={loading}>
            Send
          </button>
        </footer>
      </div>
    </div>
  );
}
createRoot(document.getElementById("root")!).render(<App />);
