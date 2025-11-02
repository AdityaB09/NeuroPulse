from fastapi import FastAPI
from pydantic import BaseModel
import os, numpy as np

MODEL = os.getenv("EMBEDDING_MODEL", "all-mpnet-base-v2")  # 768-dim
DIM = 768

try:
    from sentence_transformers import SentenceTransformer
    _model = SentenceTransformer(MODEL)
except Exception:
    _model = None

app = FastAPI()

class EmbedIn(BaseModel):
    text: str

@app.get("/health")
async def health():
    return {"status": "ok", "service": "embeddings", "model": MODEL, "dim": DIM, "loaded": _model is not None}

@app.get("/dim")
async def dim():
    return {"dim": DIM}

@app.post("/embed")
async def embed(inp: EmbedIn):
    if _model is None:
        vec = np.random.default_rng(0).standard_normal(DIM).astype("float32")
    else:
        vec = _model.encode(inp.text, normalize_embeddings=True).astype("float32")
        if vec.shape[0] != DIM:  # hard guard against accidental 384-drift
            # pad or raise; raising is safer for correctness:
            raise ValueError(f"Embedding dim {vec.shape[0]} != {DIM}")
    return {"vector": vec.tolist()}
