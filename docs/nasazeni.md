# Nasazení Krizovka do Kubernetes (GitOps & ArgoCD)

Server `kuba@192.168.1.38` běží Minikube s Ingress kontrolerem a ArgoCD. Aplikace Krizovka běží v plně izolovaném namespace `krizovka`.

## Přístup k aplikacím

### 1. Veřejný přístup přes Cloudflare Zero Trust Tunnel (Produkce / MVP náhled)
Tunel běží v klastru a směruje veřejné HTTPS domény přímo na interní Kubernetes Services (bez nutnosti otevírání portů na routeru či veřejné IP):

| Veřejná URL (Destination) | Interní Service v klastru | Účel |
| --- | --- | --- |
| `https://krizovka.qubert.cz` | `http://krizovka-client.krizovka.svc.cluster.local:80` | Klientská aplikace (Flutter Web pro občana) |
| `https://krizovka-velin.qubert.cz` | `http://krizovka-admin.krizovka.svc.cluster.local:3000` | Krizový Velín (Next.js 16 dohledové centrum) |

> [!NOTE]
> **API není nutné vystavovat veřejně:**
> - Flutter Web klient volá `/api/` na svém vlastním originu (`krizovka.qubert.cz/api/...`), NGINX v podu jej interně přesměruje na `http://krizovka-api:8080`.
> - Next.js Velín volá `/api/` na svém originu (`krizovka-velin.qubert.cz/api/...`), serverový proxy handler v Next.js jej interně přepošle na `krizovka-api:8080` a `krizovka-users:8080`.

---

### 2. Lokální přístup v LAN síti (192.168.1.38)
Přímý přístup bez nutnosti Cloudflare tunelu přes NGINX Ingress kontroler:
- **Klientská aplikace:** [http://krizovka.192.168.1.38.nip.io/](http://krizovka.192.168.1.38.nip.io/) (nebo `http://192.168.1.38/`)
- **Krizový Velín:** [http://velin.192.168.1.38.nip.io/](http://velin.192.168.1.38.nip.io/)
- **API Healthcheck:** [http://api.192.168.1.38.nip.io/health/live](http://api.192.168.1.38.nip.io/health/live)

#### Výchozí přihlašovací účty pro Velín:
- **Operátor:** `operator@krizovka.cz` / `Operator123!` (organizace: Kampus Sever, role: `OPERATOR`)
- **Super Admin:** `superadmin@krizovka.cz` / `SuperAdmin123!` (role: `SUPER_ADMIN`)

---

### 3. Lokální DNS `/etc/hosts` (volitelné pro vývojářské stroje)
Pro pohodlné otevírání vývojových domén v LAN bez `nip.io`:
```
192.168.1.38 krizovka.qubert.cz krizovka-velin.qubert.cz krizovka-admin.qubert.cz krizovka-api.qubert.cz velin.krizovka.local app.krizovka.local api.krizovka.local
```

---

## Architektura GitOps & CI/CD
1. **GitHub Repozitář:** `https://github.com/Legiani/krizovka`
2. **GitHub Actions:** `.github/workflows/ci.yml` automaticky spouští unit testy (.NET, Flutter, Next.js), kompiluje web klienta a pushuje Docker obrazy do GHCR:
   - `ghcr.io/legiani/krizovka/api:latest`
   - `ghcr.io/legiani/krizovka/users:latest`
   - `ghcr.io/legiani/krizovka/admin:latest`
   - `ghcr.io/legiani/krizovka/client:latest`
3. **ArgoCD:** Aplikace `krizovka-app` na klastru automaticky synchronizuje manifesty z větve `main` z adresáře `infra/k8s`.

---

## Verifikace stavu prostředí

```bash
# Kontrola podů
ssh kuba@192.168.1.38 'kubectl -n krizovka get pods,svc,ingress'

# Spuštění smoke testů přímo v klastru
ssh kuba@192.168.1.38 'kubectl -n krizovka exec -i deployment/krizovka-admin -- node --input-type=module' < scripts/cluster-smoke.mjs
```
