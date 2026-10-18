# 🚀 Projekt Krizovka Handoff: Pokračování úkolu (Odpověď z administrace)

Předávací protokol pro plynulé pokračování v novém okně chatu s čistým kontextem a úsporou tokenů.

## 📁 Pracovní repozitáře a prostředí
- **Repozitář:** `/Users/jakubbednar/Developer/Krizovka` (GitHub: `https://github.com/Legiani/krizovka.git`)
- **Aktivní větev:** `main` (čistý stav, synchronizováno s `origin/main`)
- **Prostředí:**
  - Cluster: Minikube na `kuba@192.168.1.38`
  - Web klient: `http://krizovka.192.168.1.38.nip.io/`
  - Administrace (Velín): `http://velin.192.168.1.38.nip.io/`
  - API: `http://api.192.168.1.38.nip.io/`
  - Users: `http://users.192.168.1.38.nip.io/` (nebo interně `http://krizovka-users:8080`)
  - Testovací přihlášení: `operator@krizovka.cz` / `Operator123!`, `superadmin@krizovka.cz` / `SuperAdmin123!`

---

## 🎯 Vyřešeno v předchozím chatu
1. **Oprava HTTP 500 při nahrávání příloh (fotografie):**
   - V [IAttachmentStorage.cs](file:///Users/jakubbednar/Developer/Krizovka/backend/Krizovka.Api/Reports/IAttachmentStorage.cs) přidán fallback pro dočasný adresář.
   - V [Dockerfile](file:///Users/jakubbednar/Developer/Krizovka/backend/Krizovka.Api/Dockerfile) opravena oprávnění složky pro neprivilegovaného uživatele `app`.
   - V [applications.yaml](file:///Users/jakubbednar/Developer/Krizovka/infra/k8s/applications.yaml) namontován `emptyDir` svazek `/app/data`.
   - V [ReportEndpoints.cs](file:///Users/jakubbednar/Developer/Krizovka/backend/Krizovka.Api/Reports/ReportEndpoints.cs) opraven `INSERT` do DB (`db.Set<IssueAttachment>().Add(attachment)`).
2. **Oprava zasekávání na „Čekám na velín“:**
   - `GET /api/reports/{id}` a `GET /api/reports/{id}/attachments/{id}` uvolněny pro anonymní přístup s multi-tenant kontrolou pro operátory.
   - Mobilní klient se po převzetí operátorem (`Accepted`) ihned přepne na zelený stav „Převzato operátorem – Pomoc je na cestě“.
3. **Live incident stream v administraci a klientu:**
   - Ve velínu ([apps/admin/src/app/page.tsx](file:///Users/jakubbednar/Developer/Krizovka/apps/admin/src/app/page.tsx)) zaveden periodický live polling (2.5 s) otevřeného detailu.
   - V klientu ([apps/client/lib/app.dart](file:///Users/jakubbednar/Developer/Krizovka/apps/client/lib/app.dart)) vytvořen interaktivní Online stream incidentu s odznaky a časovou osou.
4. **Testy a nasazení:**
   - 34/34 .NET testů zelených.
   - 19/19 Flutter testů zelených.
   - CI/CD proběhlo na GitHub Actions, kontejnery nasazeny a ověřeny na clusteru (18/18 smoke testů PASS).

---

## 🎯 Vyřešeno v tomto úkolu (Odpověď z administrace / Velínu)
1. **Backend (`ReportEndpoints.cs` & `Issue.cs`):**
   - V `POST /api/reports/{id}/messages` zavedena tenant kontrola pro přihlášeného operátora (`report.OrganizationId == callerOrgId` nebo role `SuperAdmin`).
   - Přidána doménová metoda `report.AddOperatorMessage(...)`, která do historie ukládá `action: "OperatorMessageAdded"` a `metadata: "operator"`.
   - Občanské zprávy zůstávají `action: "MessageAdded"` s `metadata: "citizen"`.
   - V `ReportTenantSecurityTests.cs` přidány 3 nové testy na tenant bezpečnost operátora a anonymní doplňující zprávy občanů.
2. **Administrace (`apps/admin`):**
   - V `features/reports/api.ts` exportována metoda `sendReportMessage(id, message)`.
   - V modalu detailu incidentu (`apps/admin/src/app/page.tsx`) integrován obousměrný komunikační kanál s odlišením bublin (Velín vs. Oznamovatel) a formulář „Odpovědět oznamovateli“ s indikací odesílání a okamžitou aktualizací historie.
   - V mapě akcí historie doplněno `OperatorMessageAdded: 'Odpověď z velínu'`.
3. **Mobilní klient (`apps/client`):**
   - V `ReportHistoryItem` přidán pomocný getter `isOperatorMessage`.
   - V časové ose incidentu (`_buildTimelineItem`) zavedeno výrazné vizuální odlišení zprávy z velínu („🏢 Zpráva z velínu“, barvy `#FEF3C7`/`#B45309`, žluté orámování a podbarvení) oproti zprávě občana („💬 Vy (občan)“).
   - Do `reporting_actions_test.dart` přidán integrační test ověřující odlišení operátorské zprávy v timeline při pollingu.
4. **Verifikace:**
   - 37/37 .NET testů v `Krizovka.Api.Tests` (celkem 53 testů) PASS.
   - 20/20 Flutter testů PASS.
   - `npm run build` administrace PASS.

---

## 📜 Závazná pravidla pro agenta
- **Caveman Mode:** Maximální stručnost a úspora tokenů.
- **K8s & GitOps standard:** Žádné ad-hoc port-forwardy na hostiteli, vše přes Git commit, GitHub Actions a K8s Ingress.
- **TDD:** Spustit testy před dokončením (`dotnet test backend/Krizovka.slnx`, `fvm flutter test`).

Načti handoff.md a ihned začni řešit implementaci odesílání odpovědí z administrace.
