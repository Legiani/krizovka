#!/usr/bin/env python3
"""Create independent development secrets once. Never prints their values."""
import json
import os
from pathlib import Path
import secrets

root = Path(__file__).resolve().parent.parent
directory = root / ".local"
directory.mkdir(mode=0o700, exist_ok=True)
target = directory / "k8s-secrets.json"
if target.exists():
    raise SystemExit("Existing .local/k8s-secrets.json preserved; credentials were not rotated.")

postgres_password, api_password, users_password, internal_key, jwt_key, webhook_key = (
    secrets.token_urlsafe(48) for _ in range(6)
)

def secret(name, data):
    return {"apiVersion": "v1", "kind": "Secret", "type": "Opaque",
            "metadata": {"name": name, "namespace": "krizovka"}, "stringData": data}

items = [
    secret("postgres-credentials", {"POSTGRES_PASSWORD": postgres_password,
           "API_DB_PASSWORD": api_password, "USERS_DB_PASSWORD": users_password}),
    secret("api-credentials", {"ConnectionStrings__DefaultConnection":
           f"Host=krizovka-postgres;Database=krizovka;Username=krizovka_api;Password={api_password}",
           "INTERNAL_API_KEY": internal_key}),
    secret("users-credentials", {"DB_CONNECTION_STRING":
           f"Host=krizovka-postgres;Database=krizovka_users;Username=krizovka_users;Password={users_password}",
           "INTERNAL_API_KEY": internal_key, "BackofficeAuth__GlobalSecret": jwt_key,
           "WEBHOOK_SECRET": webhook_key}),
]
with open(target, "x", opener=lambda path, flags: os.open(path, flags, 0o600)) as file:
    json.dump({"apiVersion": "v1", "kind": "List", "items": items}, file)
print("Created .local/k8s-secrets.json (Git-ignored, mode 0600).")
