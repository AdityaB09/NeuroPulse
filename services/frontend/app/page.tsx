"use client";
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { ingest, search } from "../lib/api";

export default function Home() {
  const [q, setQ] = useState("how to deploy this?");
  const [src, setSrc] = useState("notes");
  const [text, setText] = useState("NeuroPulse is a .NET + RAG platform.");
  const [hits, setHits] = useState<any[]>([]);
  const [events, setEvents] = useState<string[]>([]);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl((process.env.NEXT_PUBLIC_API_BASE || "http://localhost:8080") + "/hub/graph")
      .withAutomaticReconnect()
      .build();
    conn.start().then(() => console.log("SignalR connected"));
    conn.on("ingested", (evt) => setEvents((e) => [JSON.stringify(evt), ...e]));
    return () => { conn.stop(); };
  }, []);

  return (
    <main className="max-w-5xl mx-auto p-6 space-y-6">
      <h1 className="text-3xl font-semibold">NeuroPulse — Cognitive Workflow (MVP)</h1>

      <section className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="p-4 bg-zinc-900 rounded-2xl space-y-3">
          <h2 className="font-medium">Ingest</h2>
          <input className="w-full p-2 rounded bg-zinc-800" placeholder="source" value={src} onChange={e=>setSrc(e.target.value)} />
          <textarea className="w-full p-2 rounded bg-zinc-800 h-32" value={text} onChange={e=>setText(e.target.value)} />
          <button className="px-3 py-2 bg-emerald-600 rounded" onClick={async()=>{
            await ingest(src, text);
          }}>Ingest</button>
        </div>

        <div className="p-4 bg-zinc-900 rounded-2xl space-y-3">
          <h2 className="font-medium">Query</h2>
          <input className="w-full p-2 rounded bg-zinc-800" value={q} onChange={e=>setQ(e.target.value)} />
          <button className="px-3 py-2 bg-blue-600 rounded" onClick={async()=>{
            const r = await search(q, 5); setHits(r.hits||[]);
          }}>Search</button>
          <ul className="text-sm space-y-2">
            {hits.map((h, i)=> (
              <li key={i} className="p-2 rounded bg-zinc-800">
                <div className="text-xs opacity-70">{h.id}</div>
                <div className="font-medium">{h.source}</div>
                <div className="opacity-90">{h.content}</div>
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className="p-4 bg-zinc-900 rounded-2xl">
        <h2 className="font-medium mb-2">NeuroGraph Events</h2>
        <div className="grid gap-2">
          {events.map((e, i)=> <code key={i} className="text-xs bg-black/30 p-2 rounded">{e}</code>)}
        </div>
      </section>
    </main>
  );
}
