# 🚀 Projekt Krizovka Handoff: Interaktivní testování Flutter aplikace v prohlížeči

Předávací protokol pro spuštění backendu a komplexní verifikaci klientské Flutter aplikace v prohlížeči.

## 📁 Pracovní prostředí a větve
- **Kořen projektu:** `/Users/jakubbednar/Developer/Krizovka` (větev: `main`)
- **Backend API:** `backend/Krizovka.Api` (.NET 10, port `5080`)
- **Klientská aplikace:** `apps/client` (Flutter 3.x, web port `3080`)
- **Admin Velín:** `apps/admin` (Next.js 16, port `3100`)
- **Databáze:** PostgreSQL na Minikube nebo SQLite in-memory / dev DB
- **Specifikace Use Cases:** `docs/use-cases.md` (UC-01 až UC-13)

---

## 🎯 Cíl nového chatu: Verifikace Flutter aplikace v prohlížeči
Spustit lokální backend API a Flutter web klienta a krok za krokem ověřit v prohlížeči tyto use cases:
1. **Cold-start Geofencing (UC-13):**
   - Po načtení webu ověřit, že hlavička zobrazuje: `Aktivní krizový řetězec: 📍 Areál Kampus Sever... · Velín: velin-kampus-sever`.
2. **Nouzové hlášení (UC-01 & UC-06):**
   - Stisknout červené tlačítko `POTŘEBUJI POMOC`.
   - Zkontrolovat přechod do stavu `Hlášení doručeno – Čeká na velín` a zobrazení klientského ID incidentu.
3. **Krizové metodiky (UC-08):**
   - Přejít do záložky `Metodiky`.
   - Zkontrolovat načtené postupy (Aktivní střelec, Požár a evakuace).
   - Otestovat fulltextové vyhledávání (např. dotaz "střelec").
   - Rozkliknout detail metodiky: zkontrolovat očíslované prioritní kroky (1. UTEČ, 2. SCHOVEJ SE, 3. BOJUJ), červeně zvýrazněné zákazy a telefonní kontakty.
4. **Profil a Nastavení (UC-09):**
   - Otevřít ikonu nastavení vpravo nahoře (`/nastaveni`).
   - Upravit jméno, telefon a přepnout organizaci. Uložit nastavení.
   - Vrátit se na hlášení a ověřit promítnutí nového profilu.

---

## 🛠️ Příkazy pro spuštění prostředí
1. **Backend API:**
   ```bash
   cd backend/Krizovka.Api && dotnet run --urls "http://127.0.0.1:5080"
   ```
2. **Flutter Web Client:**
   ```bash
   cd apps/client && fvm flutter run -d web-server --web-port 3080 --web-hostname 127.0.0.1
   ```
3. **Případné spuštění Admin Velínu:**
   ```bash
   cd apps/admin && npm run dev
   ```

---

## 📜 Závazná pravidla pro agenta
- **Caveman Mode:** Maximální stručnost, zero fluff, přesné cesty a příkazy.
- **Prompt Master:** Formulovat vylepšený prompt před započetím práce.
- **Integrita:** Všechny testy (`dotnet test backend/Krizovka.slnx`, `cd apps/client && fvm flutter test`) musí zůstat 100% zelené.
