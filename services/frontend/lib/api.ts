const BASE = process.env.NEXT_PUBLIC_API_BASE || "http://localhost:8080";

export async function ingest(source: string, content: string) {
  const r = await fetch(`${BASE}/api/ingest`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ source, content }),
  });
  return r.json();
}

export async function search(query: string, topK = 5) {
  const r = await fetch(`${BASE}/api/query`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ query, topK }),
  });
  return r.json();
}
