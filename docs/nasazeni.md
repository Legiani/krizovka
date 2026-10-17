# Nasazení Krizovka do Kubernetes (GitOps & ArgoCD)

Server `kuba@192.168.1.38` běží Minikube s Ingress kontrolerem a ArgoCD. Aplikace Krizovka běží v plně izolovaném namespace `krizovka`.

## Přístup k aplikacím na interní síti (192.168.1.38)

Ingress kontroler naslouchá na portu `80` serveru `192.168.1.38`.

### 1. Klientská aplikace (Flutter Web)
- **Výchozí URL (přímá IP):** `http://192.168.1.38/`
- **DNS / Host:** `http://krizovka.qubert.cz/` nebo `http://app.krizovka.local/`
- Všechna API volání `/api/` jsou na úrovni NGINX kontejneru automaticky směrována na interní backend `http://krizovka-api:8080`.

### 2. Krizový Velín (Next.js 16 Admin Dashboard)
- **URL:** `http://krizovka-velin.qubert.cz/` nebo `http://krizovka-admin.qubert.cz/` nebo `http://velin.krizovka.local/`
- **Výchozí přihlašovací účty:**
  - **Operátor:** `operator@krizovka.cz` / `Operator123!` (organizace: Kampus Sever, role: OPERATOR)
  - **Super Admin:** `superadmin@krizovka.cz` / `SuperAdmin123!` (role: SUPER_ADMIN)

### 3. Backend API (.NET 10)
- **URL:** `http://krizovka-api.qubert.cz/` nebo `http://api.krizovka.local/`
- **Health checky:**
  - `http://krizovka-api.qubert.cz/health/live`
  - `http://krizovka-api.qubert.cz/health/ready`

---

## Lokální DNS `/etc/hosts` (volitelné pro vývojářské stroje)
Pro pohodlné otevírání domén z prohlížeče bez nutnosti veřejného DNS:
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
