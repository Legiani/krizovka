# 📋 Validační případy užití (Use Cases) pro krizový řetězec Krizovka

**Verze:** 1.0  
**Datum:** 17. 10. 2026  
**Cíl dokumentu:** Validační báze pro ověření, že implementovaný systém pokrývá všechny funkční, bezpečnostní a krizové požadavky definované v [Produktovém záměru](../produktovy-zamer.md) a [Technické dokumentaci](../technicka-dokumentace.md).

---

## 🧭 Přehled a klasifikace případů užití

| ID | Název scénáře | Priorita | Primární aktér | Dotčené komponenty |
|---|---|---|---|---|
| **UC-01** | Okamžité nouzové hlášení (One-Click SOS) | **P1 (Kritická)** | Oznamovatel v nouzi | Flutter, Krizovka.Api, DB |
| **UC-02** | Asynchronní doplnění GPS polohy | **P1 (Kritická)** | Klient (Background GPS) | Flutter, Krizovka.Api, Velín |
| **UC-03** | Přiložení fotografií a hlasové zprávy | **P1 (Kritická)** | Oznamovatel / Klient | Flutter, Krizovka.Api, Storage, Velín |
| **UC-04** | Živý jednosměrný audio/video přenos | **P1 (Kritická)** | Oznamovatel / Operátor | Flutter, LiveKit, Velín |
| **UC-05** | Autentizace operátora a ochrana velínu | **P2 (Vysoká)** | Operátor velínu / Admin | Next.js velín, Krizovka.Users |
| **UC-06** | Převzetí a uzavření hlášení operátorem | **P1 (Kritická)** | Operátor velínu | Velín, Krizovka.Api, Flutter |
| **UC-07** | Multi-tenancy a izolace organizací | **P1 (Kritická)** | Systém / Operátor | Krizovka.Api, Velín |
| **UC-08** | Prohlížení a offline čtení metodik v aplikaci | **P2 (Vysoká)** | Oznamovatel / Uživatel | Flutter (offline cache), Krizovka.Api |
| **UC-09** | Správa lokálního profilu oznamovatele | **P2 (Vysoká)** | Oznamovatel | Flutter (lokální úložiště) |
| **UC-10** | Odolnost proti výpadku sítě a obnova | **P1 (Kritická)** | Mobilní klient | Flutter, Krizovka.Api |
| **UC-11** | Převzetí (klonování) šablony z knihovny do organizace | **P2 (Vysoká)** | Správce organizace | Next.js velín, Krizovka.Api |
| **UC-12** | Editace, lokalizace a publikace metodiky | **P2 (Vysoká)** | Správce organizace | Next.js velín, Krizovka.Api, Flutter |
| **UC-13** | Geofencing a směrování poplachu na alokovaný velín | **P1 (Kritická)** | Oznamovatel / Geofence | Flutter (GPS), Krizovka.Api, Velín |

---

## 📌 Detailní specifikace Use Cases

### UC-01: Okamžité nouzové hlášení (One-Click SOS)
- **Cíl:** Umožnit člověku v bezprostředním ohrožení života jedním dotykem tiše odeslat žádost o pomoc bez nutnosti vyplňovat formuláře, vybírat kategorie nebo čekat na GPS fix.
- **Aktéři:** Oznamovatel v nouzi, Backend Krizovka.Api.
- **Vstupní podmínky:**
  1. Aplikace nainstalována / otevřena v prohlížeči.
  2. V aplikaci je zvolena aktivní organizace (např. Gymnázium Na Pražačce).
- **Hlavní scénář (Happy Path):**
  1. Uživatel klepne na velké červené tlačítko **POTŘEBUJI POMOC**.
  2. Klient vygeneruje UUID `ClientReportId` pro zajištění idempotence.
  3. Klient načte lokální profil z `SharedPreferences` (jméno, telefon, email, osobní číslo) a okamžitě dostupné souřadnice (pokud jsou ihned k dispozici).
  4. Klient odešle `POST /api/reports` s tělem:
     ```json
     {
       "clientReportId": "b1a2c3d4-...",
       "organizationId": "11111111-...",
       "description": "POTŘEBUJI POMOC",
       "latitude": 50.087,
       "longitude": 14.420,
       "accuracyMeters": 15.0,
       "contact": { "firstName": "Jan", "lastName": "Novák", "phone": "+420 777 123 456" }
     }
     ```
  5. Backend zkontroluje unikátnost dvojice `(OrganizationId, ClientReportId)`:
     - Založí záznam v tabulce `reports` ve stavu `New`.
     - Vytvoří úvodní auditní záznam `Created` v `report_history`.
     - Vrátí HTTP 201 Created s `ReportDetailDto`.
  6. Klient přejde do stavu `submitted` ("ČEKÁ NA PŘEVZETÍ VELÍNEM") a spustí 2s polling dotazů na stav.
  7. Telefon **nevydá žádný zvuk, nevibruje a nezapíná blesk** (tichý režim).
- **Alternativní / Chybové toky:**
  - *GPS souřadnice nejsou v okamžiku stisku dostupné:* Hlášení odejde s `latitude: null, longitude: null`. Na GPS se nečeká; souřadnice se doposílají asynchronně (viz UC-02).
  - *Uživatel v panice klepne na tlačítko opakovaně:* Díky fixnímu `ClientReportId` backend vrátí existující hlášení (HTTP 200 OK) a nevznikne duplicitní záznam.
- **Akceptační kritéria:**
  - Odezva odeslání do 500 ms při funkčním připojení.
  - V databázi existuje záznam se stavem `New` a vazbou na organizaci.
  - Zařízení zůstává zcela tiché.

---

### UC-02: Asynchronní doplnění GPS polohy (Late Location Streaming)
- **Cíl:** Zajistit, že hlášení odeslané bez polohy (nebo s hrubou polohou) je operativně zpřesněno, jakmile geolokační modul získá přesný fix.
- **Aktéři:** Mobilní klient (geolokační služba), Backend Krizovka.Api, Velín.
- **Vstupní podmínky:** Hlášení bylo odesláno (UC-01), klient zná `reportId`.
- **Hlavní scénář (Happy Path):**
  1. GPS hardware získá přesnou polohu (např. přesnost 5 metrů).
  2. Klient na pozadí odešle `POST /api/reports/{id}/location`:
     ```json
     {
       "latitude": 50.08721,
       "longitude": 14.42084,
       "accuracyMeters": 5.2,
       "locationMeasuredAt": "2026-10-17T14:30:00Z"
     }
     ```
  3. Backend aktualizuje pole `Latitude`, `Longitude`, `AccuracyMeters`, `LocationMeasuredAt` a `UpdatedAt` v tabulce `reports`.
  4. Backend zapíše záznam do `report_history` s akcí `LocationUpdated`.
  5. Při dalším live polling cyklu velínu (interval 2.5 s) se u hlášení objeví aktualizovaná poloha a tlačítko **📍 Otevřít v mapách**.
- **Alternativní toky:**
  - *Uživatel zamítl oprávnění k poloze:* Žádný update se neposílá; velín zobrazuje zřetelné upozornění „Poloha nezjištěna“.
- **Akceptační kritéria:**
  - Poloha v DB je aktualizována bez změny stavu incidentu.
  - Historie obsahuje přesný čas naměření i záznamu.

---

### UC-03: Přiložení fotografií a hlasové zprávy z místa incidentu
- **Cíl:** Umožnit odeslat doplňující důkazní materiál (fotografie situace, tichá hlasová zpráva) bez přerušení krizového stavu.
- **Aktéři:** Oznamovatel / Klient, Backend Krizovka.Api, Souborové úložiště, Operátor.
- **Vstupní podmínky:** Hlášení existuje na backendu (`reportId`).
- **Hlavní scénář (Happy Path):**
  1. Uživatel pořídí fotografii nebo nahraje audio nahrávku (m4a/aac).
  2. Klient odešle multipart/form-data na `POST /api/reports/{id}/attachments`.
  3. Backend ověří:
     - Soubor existuje a má nenulovou velikost.
     - Velikost nepřesahuje 25 MB.
     - Validní Content-Type (`image/*`, `audio/*`).
  4. `FileSystemAttachmentStorage` uloží soubor do izolovaného adresáře `{reportId}/{attachmentId}_{fileName}` s ochranou proti path traversal.
  5. Vytvoří se záznam v tabulce `report_attachments` a audit v `report_history` (`AttachmentAdded`).
  6. Backend vrátí HTTP 201 Created s metadaty přílohy.
  7. Operátor velínu vidí v modalu hlášení novou položku:
     - Pro fotografii: náhledový thumbnail + možnost otevřít v plné velikosti.
     - Pro hlasovou zprávu: HTML5 audio přehrávač (`<audio controls>`) se streamováním z `GET /api/reports/{id}/attachments/{attId}`.
- **Alternativní / Chybové toky:**
  - *Soubor přesahuje 25 MB:* Backend vrátí HTTP 400 s popisem chyby; klient informuje uživatele.
  - *Pokus o nahrání k neexistujícímu hlášení:* HTTP 404 Not Found.
- **Akceptační kritéria:**
  - Soubor je bezpečně uložen na disku.
  - Audio přehrávač na velínu podporuje range-requesty (přetáčení v nahrávce).

---

### UC-04: Živý jednosměrný audio/video přenos (LiveKit)
- **Cíl:** Poskytnout operátorovi velínu přímý poslech a obraz z místa incidentu v reálném čase.
- **Aktéři:** Oznamovatel, Operátor velínu, Mediální server (LiveKit).
- **Vstupní podmínky:** Zařízení má kameru/mikrofon a uživatel povolil oprávnění.
- **Hlavní scénář (Happy Path):**
  1. Uživatel klepne na **Živě audio** (pouze mikrofon) nebo **Živě video** (kamera + mikrofon).
  2. Základní SOS hlášení je odesláno okamžitě (UC-01), na navázání streamu se nečeká.
  3. Klient získá token do vyhrazené LiveKit místnosti incidentu `room_{reportId}`.
  4. Klient publikuje audio/video stream **pouze směrem ven (uplink)**.
  5. Telefon uživatele **nepřehrává žádný zpětný zvuk z velínu** (ochrana před prozrazením úkrytu).
  6. Operátor v detailu hlášení vidí indikátor živého přenosu a vědomě spustí přehrávání.
  7. Uživatel může přenos kdykoliv ukončit; ukončení streamu neuzavírá hlášení.
- **Alternativní toky:**
  - *Výpadek streamu z důvodu slabého signálu:* Klient indikuje rozpojení, základní hlášení zůstává platné, uživatel může pokračovat textem nebo hlasovkou.
- **Akceptační kritéria:**
  - Jednosměrný přenos: nulový zvukový výstup na reproduktoru mobilního zařízení.
  - Latence zvuku do 500 ms.

---

### UC-05: Autentizace operátora a ochrana velínu
- **Cíl:** Zamezit neoprávněnému přístupu k velínu a datům hlášení; povolit přístup pouze autorizovaným operátorům a správcům.
- **Aktéři:** Operátor / Administrátor, Next.js velín, Backend Krizovka.Users.
- **Vstupní podmínky:** Uživatel má vytvořený účet v `krizovka_users` s rolí (např. `BO_SUPER_ADMIN` nebo `OPERATOR`).
- **Hlavní scénář (Happy Path):**
  1. Uživatel přistoupí na adresu velínu (`http://localhost:3100/`).
  2. Next.js `middleware.ts` detekuje absenci `admin_session` cookie a přesměruje na `/login`.
  3. Uživatel vyplní email a heslo a potvrdí formulář.
  4. Požadavek odejde na `POST /api/backoffice/auth/login`.
  5. Krizovka.Users ověří BCrypt hash hesla, vygeneruje JWT a vrátí HTTP 200 s hlavičkou:
     `Set-Cookie: admin_session=<jwt>; HttpOnly; SameSite=Lax; Path=/`
  6. Prohlížeč uloží cookie a přesměruje na `/`.
  7. `AppShell` zavolá `GET /api/backoffice/auth/current-user`, zobrazí jméno a roli v záhlaví.
  8. Při kliknutí na **Odhlásit se** je zavolán `POST /api/backoffice/auth/logout`, cookie je smazána a uživatel je vrácen na `/login`.
- **Alternativní / Chybové toky:**
  - *Chybné heslo:* Backend vrátí HTTP 401, přihlašovací obrazovka zobrazí „Neplatné přihlašovací údaje“.
  - *Pokus o přímé volání API bez cookie:* Gateway proxy nebo backend vrátí HTTP 401.
- **Akceptační kritéria:**
  - Žádná chráněná stránka (`/`, `/uzivatele`, `/role`, `/metodiky`, `/organizace`) není přístupná bez platné `admin_session`.
  - Cookie má nastaven příznak `HttpOnly`.

---

### UC-06: Převzetí a uzavření hlášení operátorem velínu
- **Cíl:** Zajištění celého životního cyklu krizového hlášení na dispečerském velínu s okamžitou zpětnou vazbou pro oznamovatele.
- **Aktéři:** Operátor velínu, Backend Krizovka.Api, Oznamovatel.
- **Vstupní podmínky:** Existuje hlášení ve stavu `New`, operátor je přihlášen.
- **Hlavní scénář (Happy Path):**
  1. Velín v rámci live-polling cyklu (2.5 s) načte nové hlášení; v tabulce svítí žlutý odznak **Nové**.
  2. Operátor klikne na řádek hlášení a otevře detailní modal.
  3. Operátor prozkoumá popis, polohu, kontaktní údaje oznamovatele a případné přílohy.
  4. Operátor klikne na tlačítko **Převzít hlášení**.
  5. Velín odešle `POST /api/reports/{id}/status` s `status: "Accepted"`.
  6. Backend změní stav na `Accepted`, nastaví `UpdatedAt` a do `report_history` zapíše `StatusChange: New → Accepted`.
  7. Mobilní klient oznamovatele při svém 2s pollingu detekuje změnu a přepne obrazovku na zelený stav **PŘEVZATO OPERÁTOREM [čas]**. Oznamovatel ví, že pomoc je na cestě.
  8. Po vyřešení incidentu operátor klikne na **Uzavřít hlášení** (`Closed`).
  9. Klient oznamovatele zobrazí stav **UZAVŘENO**.
- **Akceptační kritéria:**
  - Zpoždění promítnutí převzetí do mobilního klienta nepřesáhne 3 sekundy.
  - Všechny přechody stavů jsou zaznamenány v historii s identifikátorem operátora.

---

### UC-07: Multi-tenancy a izolace organizací
- **Cíl:** Zaručit, že žádná organizace neuvidí hlášení, kontakty ani interní metodiky cizí organizace.
- **Aktéři:** Operátor Organizace A, Operátor Organizace B.
- **Vstupní podmínky:** V databázi existují data Organizace A a Organizace B.
- **Hlavní scénář (Happy Path):**
  1. Oznamovatel odešle hlášení pro Organizaci A (`OrganizationId = A`).
  2. Operátor přihlášený pod Organizací A vidí toto hlášení v seznamu i na mapě.
  3. Operátor Organizace B má nastaven kontext Organizace B:
     - Volání `GET /api/reports?organizationId=B` toto hlášení nevrátí.
     - Pokus o přímé volání `GET /api/reports/{id_A}` vrátí 404 nebo 403.
     - Přílohy z Organizace A nejsou pro B dostupné.
- **Akceptační kritéria:**
  - Striktní filtrace dat na úrovni SQL dotazů (`WHERE OrganizationId = @orgId`).
  - Úplná izolace hlášení, příloh a krizových manuálů.

---

### UC-08: Prohlížení a offline čtení metodik v aplikaci (Krizový manuál „Jak se zachovat“)
- **Cíl:** Umožnit studentovi/zaměstnanci v ohrožení či před incidentem okamžitě vyhledat krizovou situaci a číst stručný návod krok za krokem i při totálním výpadku mobilní sítě.
- **Aktéři:** Oznamovatel / Uživatel mobilní aplikace, Krizovka.Api.
- **Vstupní podmínky:**
  1. Aplikace nainstalována / otevřena v prohlížeči.
  2. V aplikaci je zvolena aktivní organizace (např. *Gymnázium Na Pražačce*).
  3. Organizace má publikované metodiky (`Status = Published`).
- **Hlavní scénář (Happy Path):**
  1. Uživatel klepne na záložku **Metodiky**.
  2. Aplikace zobrazí fulltextové vyhledávací pole a přehled krizových situací (např. *Aktivní střelec / Ozbrojený útok*, *Požár a evakuace*, *Nález podezřelého předmětu*, *Únik nebezpečných látek*, *Zdravotní kolaps / První pomoc*).
  3. Uživatel zadá hledaný výraz (např. „střelec“ nebo „zbraň“) – seznam se bleskově vyfiltruje (do 100 ms).
  4. Uživatel klepne na detail situace a zobrazí se přehledná struktura:
     - **1. Prioritní kroky (Okamžitá akce):** Velká, očíslovaná a stručná doporučení (např. *1. UTEČ – opusť budovu bezpečným směrem; 2. SCHOVEJ SE – zamkni a zataras dveře, zhasni, přepni telefon na tichý režim bez vibrací; 3. BOJUJ – improvizovanými prostředky pouze při bezprostředním ohrožení života*).
     - **2. Zákazy (Čeho se vyvarovat):** Nepoužívat výtahy, neshlukovat se u oken, nevycházet se zbraní v ruce vstříc zásahové jednotce Policie ČR.
     - **3. Místní tísňové kontakty organizace:** Tlačítka pro okamžité vytočení místní ostrahy, velína školy nebo linky 112/158.
  5. **Offline fungování:** Všechny publikované metodiky dané organizace jsou uloženy v lokální cache (např. SQLite / Hive / SharedPreferences). Při zapnutí režimu Letadlo nebo výpadku BTS je celý manuál 100% přístupný bez omezení.
- **Alternativní / Chybové toky:**
  - *Organizace nemá žádné publikované metodiky:* Aplikace zobrazí upozornění „Organizace zatím nepublikovala žádné metodické postupy“ a nabídne standardní krizová telefonní čísla (112, 150, 155, 158).
  - *Změna organizace v Nastavení:* Mezipaměť se automaticky přečte pro nově zvolenou organizaci.
- **Akceptační kritéria:**
  - V aplikaci se zobrazují výhradně metodiky ve stavu `Published` patřící aktivní organizaci.
  - Zobrazení detailu a vyhledávání funguje do 300 ms i bez připojení k internetu.

---

### UC-09: Správa lokálního profilu oznamovatele
- **Cíl:** Mít předpřipravené kontaktní údaje na zařízení uživatele bez nutnosti registrace občana/studenta v centrální databázi.
- **Aktéři:** Oznamovatel.
- **Vstupní podmínky:** Uživatel poprvé spustil aplikaci.
- **Hlavní scénář (Happy Path):**
  1. Uživatel přejde do **Nastavení** (`/nastaveni`).
  2. Vyplní:
     - Jméno a příjmení (např. *Jan Novák*).
     - Telefon (*+420 777 123 456*).
     - E-mail (*jan.novak@skola.cz*).
     - Osobní / studentské číslo (*ST12345*).
     - Zvolí výchozí organizaci.
  3. Data se uloží do chráněného lokálního úložiště zařízení.
  4. Centrální backend o uživateli neví (neexistuje žádný uživatelský účet v DB).
  5. Při stisku POMOC se data zkopírují jako snapshot do hlášení.
- **Akceptační kritéria:**
  - Nulová závislost na přihlašování koncových uživatelů (GDPR minimalizace).
  - Možnost údaje kdykoliv lokálně smazat nebo změnit.

---

### UC-10: Odolnost proti výpadku sítě a obnova bez duplicit
- **Cíl:** Zabezpečit, že hlášení nezmizí při výpadku spojení a jeho opětovné odeslání nezahltí velín duplikáty.
- **Aktéři:** Mobilní klient, Backend Krizovka.Api.
- **Vstupní podmínky:** Uživatel stiskne POMOC v místě se špatným signálem (např. suterén, výtah).
- **Hlavní scénář (Happy Path):**
  1. Klient se pokusí odeslat hlášení s vygenerovaným `ClientReportId`.
  2. HTTP požadavek selže na timeout / socket error.
  3. Klient **nezobrazí falešné potvrzení**, ale indikuje stav „Odesílání selhalo – opakuji spojení...“.
  4. Klient drží hlášení v lokální frontě a v intervalech opakuje požadavek se **stejným** `ClientReportId`.
  5. Jakmile se telefon připojí na Wi-Fi nebo LTE, požadavek projde na backend.
  6. Backend hlášení zaeviduje a potvrdí.
- **Akceptační kritéria:**
  - Žádné ztracené hlášení při krátkodobém výpadku sítě.
  - Stoprocentní ochrana proti vzniku vícenásobných hlášení ze stejného incidentu.

---

### UC-11: Převzetí (klonování) šablony z centrální knihovny do organizace (Velín)
- **Cíl:** Umožnit správci organizace snadno zavést osvědčené standardizované krizové postupy jedním kliknutím bez nutnosti psát metodiky od nuly.
- **Aktéři:** Správce organizace / Operátor s oprávněním `GUIDES:WRITE`, Next.js velín, Backend Krizovka.Api.
- **Vstupní podmínky:**
  1. Správce je přihlášen do velínu s aktivní organizací.
  2. Centrální knihovna šablon obsahuje sadu standardních metodik (20 základních scénářů dle metodiky MV ČR a R. Kukly).
- **Hlavní scénář (Happy Path):**
  1. Správce otevře ve velínu sekci **Metodiky** a přepne na záložku **Knihovna šablon**.
  2. Zobrazí se katalog ověřených šablon (např. *Ozbrojený útok v budově*, *Nález podezřelého předmětu*, *Požár a žhářství*, *Havárie v laboratoři*, *Psychická krize*).
  3. Správce klikne na **Zobrazit šablonu** a prostuduje výchozí strukturu kroků a doporučení.
  4. Správce klikne na tlačítko **Převzít do organizace**.
  5. Velín odešle požadavek na backend:
     `POST /api/guides/clone`
     ```json
     {
       "templateId": "tmpl-active-shooter-v1",
       "organizationId": "11111111-1111-1111-1111-111111111111"
     }
     ```
  6. Backend vytvoří novou nezávislou entitu v tabulce `guides`:
     - Vygeneruje nové `Id` (GUID).
     - Přiřadí `OrganizationId = activeOrgId`.
     - Zaznamená metadata původu: `SourceTemplateId = "tmpl-active-shooter-v1"`, `SourceVersion = 1`.
     - Nastaví výchozí stav `Status = Draft` (Rozpracováno).
     - Zkopíruje všechny kroky, doporučení a strukturu.
  7. Backend vrátí nově vytvořenou metodiku organizace.
  8. Velín přesměruje správce rovnou do editoru metodiky pro doplnění místních údajů.
- **Alternativní toky:**
  - *Šablona již byla dříve převzata:* Velín zobrazí upozornění „Tuto šablonu již máte v organizaci převzatou. Chcete vytvořit další variantu (např. pro jiný areál či budovu)?“ a umožní pokračovat.
  - *Aktualizace centrální šablony v budoucnu:* Změna v centrální knihovně se **nikdy automaticky nepropisuje** do převzatých metodik organizace, aby nepřepsala místní specifika. U metodiky se pouze zobrazí volitelné upozornění „K dispozici je nová verze šablony v1.2 – Zobrazit rozdíly / Převzít znovu“.
- **Akceptační kritéria:**
  - Vzniklá entita je 100% vlastněna organizací a izolována od ostatních tenantů.
  - Výchozí stav po naklonování je vždy `Draft` (nezobrazuje se koncovým uživatelům v mobilu, dokud není schválena).

---

### UC-12: Editace, lokalizace a publikace metodiky organizace (Velín)
- **Cíl:** Umožnit organizaci upravit krizové kroky na míru vlastním budovám (konkrétní úkrytové místnosti, evakuační shromaždiště), doplnit lokální kontakty (vrátnice, ostraha, krizový štáb) a zveřejnit metodiku pro mobilní klienty.
- **Aktéři:** Správce organizace / Operátor s oprávněním `GUIDES:WRITE`, Next.js velín, Backend Krizovka.Api.
- **Vstupní podmínky:** Existující metodika organizace ve stavu `Draft` nebo `Published`.
- **Hlavní scénář (Happy Path):**
  1. Správce otevře přehled metodik své organizace ve velínu a klikne na **Upravit metodiku**.
  2. Zobrazí se formulářový editor s těmito sekcemi:
     - **Základní údaje:** Název metodiky, stručný anotovaný souhrn, cílová role (Všichni / Učitelé / Ostraha).
     - **Prioritní kroky (První reakce):** Editovatelný seznam očíslovaných kroků. Správce může přidávat, mazat a přetahovat pořadí (např. upraví krok 2 na: „Úkryt: místnosti 204, 210 a aula – zamknout zevnitř a zabarikádovat stoly“).
     - **Zákazy a čeho se vyvarovat:** Seznam bodů chování, které zhoršují situaci.
     - **Místní tísňové kontakty:** Definice místních telefonních čísel specifických pro budovu (např. *Vrátnice pavilon A: 222 111 001*, *Vedoucí ostrahy: 777 888 999*, *Školní psycholog: 777 111 222*).
  3. Správce klikne na **Uložit koncept** (`PUT /api/guides/{id}` se stavem `Draft`): změny se uloží do DB, ale neprojeví se v mobilu.
  4. Po schválení správce klikne na **Zveřejnit metodiku**:
     - Backend přepne `Status = Published`, zaznamená `PublishedAt = UtcNow` a ID publikujícího operátora.
  5. Mobilní klienti dané organizace si při dalším spuštění nebo aktualizaci na pozadí stáhnou publikovanou metodiku.
  6. V případě potřeby (např. rekonstrukce budovy či neplatnost) může správce kliknout na **Archivovat metodiku** (`Status = Archived`), čímž se metodika okamžitě stáhne z mobilních aplikací.
- **Alternativní toky:**
  - *Tvorba metodiky zcela od nuly (bez šablony):* Správce klikne na **+ Nová metodika**, vyplní prázdný formulář s `SourceTemplateId = null` a uloží ji.
  - *Smazání konceptu:* Metodiku ve stavu `Draft` lze trvale smazat (`DELETE /api/guides/{id}`).
- **Akceptační kritéria:**
  - Mobilní klienti mají přístup pouze k metodikám se stavem `Published`.
  - Správce může libovolně upravovat kroky a lokální kontakty bez omezení původní šablonou.
  - Změny stavu `Draft` ➔ `Published` ➔ `Archived` jsou auditovány v historii.

---

### UC-13: Automatická detekce areálu při spuštění aplikace a dynamické směrování poplachu (Geofencing s offsetem a default dispečinkem)
- **Cíl:** Plně autonomně a bez jakéhokoliv zásahu či potvrzování uživatelem detekovat při spuštění aplikace areál (výrobní závod, kampus, nemocnice, administrativní centrum) na základě polygonu a konfigurovatelného offsetu, okamžitě načíst jeho specifické metodiky a v případě spuštění poplachu směrovat hlášení přímo do velínu alokovaného pro tuto oblast. Pokud uživatel není v žádném registrovaném areálu, hlášení je spolehlivě doručeno na centrální defaultní dispečink.
- **Aktéři:** Oznamovatel (zaměstnanec, student, návštěvník), Mobilní klient (background geolokační bootstrap), Backend Krizovka.Api (Geospatial resolver s PostGIS), Operátor alokovaného velínu areálu, Operátor výchozího (default) dispečinku.
- **Vstupní podmínky:**
  1. Organizace mají v systému definovány hranice areálů: geometrický polygon hranice pozemku/budov v GeoJSON + **konfigurovatelný offset (toleranční buffer v metrech, např. +50 m)** pokrývající bezprostřední předpolí (příjezdové komunikace, parkoviště, zastávky u vrátnic) a kompenzující nepřesnosti mobilní GPS. Ke každému areálu je přiřazen alokovaný velín (`AllocatedVelinId`).
  2. V systému je nakonfigurován minimálně jeden **Defaultní dispečink (Fallback Velín)** pro spolehlivý 24/7 příjem hlášení z oblastí mimo registrované areály.
  3. Klient má systémově povolen přístup k poloze zařízení.
- **Hlavní scénář (Happy Path – 100% automatický běh):**
  1. **Spuštění aplikace (Cold Start):** Uživatel otevře aplikaci Krizovka.
  2. **Okamžitá lokalizace na pozadí:** Aplikační bootstrap během inicializace (do 200 ms) získá aktuální GPS polohu zařízení bez zdržení startu UI.
  3. **Autonomní vyhodnocení areálu (Polygon + Offset):**
     - Klient na pozadí ověří souřadnice vůči geofence areálům (rychlé volání `GET /api/organizations/resolve-location?lat=...&lon=...` s fallbackem na lokálně cachované polygony).
     - Backend vyhodnotí prostorový dotaz:
       `ST_DWithin(area.Polygon, ST_SetSRID(ST_MakePoint(lon, lat), 4326)::geography, area.OffsetMeters)`
     - Pokud bod leží přímo v polygonu areálu NEBO v jeho offsetovém perimetru (např. na parkovišti 35 metrů před vstupní branou při offsetu 50 m), je detekována shoda s areálem *Firma XYZ – Závod 01* (`organizationId: 22222222-...`, `velinId: velin-xyz-01`).
  4. **Tichá adaptace aplikace bez ptaní uživatele:**
     - Aplikace **bez jakýchkoliv dialogů či potvrzování** okamžitě nastaví aktivní kontext na areál *Firma XYZ – Závod 01*.
     - V záhlaví se pouze pasivně zobrazí štítek: *📍 Areál: Firma XYZ – Závod 01*.
     - Záložka **Metodiky** se automaticky naplní postupy platnými pro areál XYZ (např. *Únik nebezpečných látek z haly 2*, *Požární poplach lakovna*) a místními tísňovými kontakty (závodní hasiči, dispečink ostrahy).
     - Tlačítko **POTŘEBUJI POMOC** je okamžitě interně přednastaveno na odeslání do velínu areálu XYZ.
  5. **Odeslání poplachu v místě (POTŘEBUJI POMOC):**
     - Uživatel stiskne tlačítko pomoci.
     - Klient odešle `POST /api/reports` přímo s `organizationId` detekovaného areálu XYZ a souřadnicemi.
  6. **Reakce místního velínu:**
     - Do 2 sekund se poplach rozezní na dispečinku **Firma XYZ – Závod 01**.
     - Dispečer vidí přesný bod v polygonu areálu (včetně informace, zda je v jádru areálu nebo v zóně offsetu/parkoviště) i to, zda je oznamovatel kmenový zaměstnanec nebo externí návštěvník.
- **Alternativní / Chybové toky:**
  - *Mimo jakýkoliv registrovaný areál i jeho offset (Veřejný prostor / terén):*
    - Bod neleží v žádném polygonu ani v tolerančním offsetu.
    - **Aktivace Defaultního dispečinku:** Hlášení nespadne do prázdna, ale je automaticky směrováno na **centrální výchozí dispečink (Default Velín)** s nepřetržitou službou.
    - Operátor defaultního dispečinku vidí GPS polohu, údaje oznamovatele a okamžitě koordinuje výjezd složek IZS (112, 158, 155).
    - Mobilní aplikace zobrazí standardní krizová čísla a tlačítko přímého volání linky 112.
  - *Nastavení offsetu perimetru v administraci velínu:*
    - Správce/dispečer může v konfiguraci areálu zadat polygon a nastavit hodnotu `offsetMeters` (např. `0 m` pro striktní hranice budovy školy, `50 m` pro areál s předpolím a parkovištěm, `150 m` pro rozsáhlý kampus).
  - *Nepřesná GPS na rozhraní zón:* Systém vybere areál deterministicky (areál s největším překryvem toleranční kružnice přesnosti / nejvyšší bezpečnostní priorita). Žádné dotazování uživatele v ohrožení.
  - *GPS nedostupná (podzemní kryt / vypnutá GPS):* Aplikace ticho použije domovskou organizaci z lokálního nastavení bez zdržení; při odeslání poplachu je hlášení záložně doručeno do domovského velínu nebo na default dispečink.
  - *Externista / návštěva:* Pokud je v areálu XYZ přítomen zaměstnanec cizí firmy, aplikace se bez ptaní adaptuje na areál XYZ. Poplach jde místní ostraze XYZ, která jediná může v areálu fyzicky zasáhnout.
- **Akceptační kritéria:**
  - Výskyt v zóně *polygon + offset* automaticky přiřadí hlášení místnímu velínu areálu.
  - Výskyt mimo veškeré areály je vždy spolehlivě doručen na centrální Defaultní dispečink (žádné ztracené hlášení).
  - Nulová interakce uživatele: detekce, aplikace offsetu i směrování probíhá 100% automaticky.
  - Rychlost vyhodnocení areálu do 500 ms od startu aplikace.

---

## 🧪 Validační matice a checklist funkcionalit

| Funkcionalita | Backend API | Backend Users | Next.js Velín | Flutter Klient | Automatizovaný test |
|---|:---:|:---:|:---:|:---:|:---:|
| **Idempotentní SOS hlášení** | ✅ Hotovo | N/A | ✅ Zobrazeno | ✅ Hotovo | `ReportLifecycleTests` |
| **Doplnění GPS polohy** | ✅ Hotovo | N/A | ✅ Proklik mapy | ✅ Hotovo | `ReportLifecycleTests` |
| **Foto a audio přílohy** | ✅ Hotovo | N/A | ✅ Galerie + Přehrávač | ✅ SDK hotovo | `ReportLifecycleTests` / `MockApiClient` |
| **Autentizace operátora** | N/A | ✅ Hotovo | ✅ `/login` + Guard | N/A (nemá účet) | `AuthTests` / Next.js check |
| **Převzetí hlášení velínem** | ✅ Hotovo | N/A | ✅ Tlačítko akce | ✅ 2s polling | `ReportLifecycleTests` / `navigation_test` |
| **Uzavření hlášení** | ✅ Hotovo | N/A | ✅ Tlačítko akce | ✅ Reaktivní stav | `ReportLifecycleTests` |
| **Auditní historie změn** | ✅ Hotovo | N/A | ✅ Časová osa | N/A | `ReportLifecycleTests` |
| **Tichý režim klienta** | N/A | N/A | N/A | ✅ Žádný zvuk/vibrace | Widget tests |
| **Multi-tenancy filtrace** | ✅ Hotovo | N/A | ✅ Přepínač org. | ✅ Volba v nastavení | Integrační testy |
| **Živé audio/video (WebRTC)** | ⏳ Připraveno | N/A | ⏳ Návrh LiveKit | ⏳ Návrh LiveKit | Klientské cvičení |
| **Offline metodiky v aplikaci (UC-08)** | ⏳ Fáze 4 | N/A | N/A | ⏳ Fáze 4 | Widget & offline tests |
| **Knihovna šablon & klonování (UC-11)** | ⏳ Fáze 4 | N/A | ⏳ Fáze 4 | N/A | Backend & Admin check |
| **Editor a publikace metodik (UC-12)** | ⏳ Fáze 4 | N/A | ⏳ Fáze 4 | ⏳ Fáze 4 | Admin check & API tests |
| **Geofencing a směrování poplachu (UC-13)** | ⏳ Fáze 3 | N/A | ⏳ Zóny areálů | ⏳ GPS detekce | Geocache & polygon tests |
| **DB správa organizací (Tenancy)** | ⏳ Fáze 3 | N/A | ⏳ DemoStore | ✅ Nastavení | Připraveno k implementaci |

---

## 🎯 Pokyny pro akceptační testování (Cvičení v terénu)

1. **Test tichosti:** Spustit SOS na iPhonu i Androidu v tiché místnosti. Ověřit, že telefon nevydá žádný systémový tón, vibraci ani neaktivuje blesk.
2. **Test rychlosti doručení:** Změřit čas od stisku tlačítka POMOC na telefonu po rozblikání nového hlášení na obrazovce velínu (cíl: < 3 s).
3. **Test převzetí operátorem:** Operátor klikne „Převzít hlášení“; stopovat čas, kdy mobilní telefon přepne na stav „PŘEVZATO“ (cíl: < 2.5 s).
4. **Test hlasovky:** Nahrát 10s mluvené slovo v telefonu, odeslat jako přílohu a ověřit přehrání ve velínu včetně posuvníku času.
5. **Test izolace:** Odeslat hlášení pod Organizací 1 a ověřit, že dispečer přihlášený do Organizace 2 toto hlášení nevidí v seznamu ani v čítačích.
6. **Test geofencingu a směrování:** Simulovat polohu uvnitř polygonu areálu XYZ. Ověřit, že aplikace automaticky přepne metodiky na areál XYZ a při stisku SOS hlášení dorazí výhradně do velínu alokovaného pro areál XYZ (i pro externího návštěvníka).
