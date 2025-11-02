from fastapi import FastAPI, UploadFile, File
from typing import List
from bs4 import BeautifulSoup
from readability import Document as RDoc
from pypdf import PdfReader
import docx

app = FastAPI()

@app.get("/health")
async def health():
    return {"status":"ok","service":"ingestor"}

@app.post("/extract")
async def extract(file: UploadFile = File(...)):
    name = (file.filename or "").lower()
    data = await file.read()
    parts: List[str] = []

    if name.endswith(".pdf"):
        r = PdfReader(bytes(data))
        for p in r.pages:
            txt = p.extract_text() or ""
            txt = txt.strip()
            if txt:
                parts.append(txt)

    elif name.endswith(".docx"):
        d = docx.Document(bytes(data))
        buf = []
        for para in d.paragraphs:
            t = (para.text or "").strip()
            if t: buf.append(t)
        if buf: parts.append("\n".join(buf))

    elif name.endswith(".html") or name.endswith(".htm"):
        html = data.decode("utf-8", errors="ignore")
        art = RDoc(html).summary()
        soup = BeautifulSoup(art, "lxml")
        text = soup.get_text("\n")
        if text.strip(): parts.append(text.strip())

    else:
        # plain text fallback
        text = data.decode("utf-8", errors="ignore")
        if text.strip(): parts.append(text.strip())

    return {"parts": parts[:200]}  # hard cap
